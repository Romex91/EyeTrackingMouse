using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace eye_tracking_mouse
{
    // When users correct inacurate precision with W/A/S/D and then click stuff they create a |UserCorrection|.
    public class UserCorrection
    {
        public UserCorrection(float[] coordinates, Point shift)
        {
            Coordinates = coordinates;
            Shift = shift;
        }

        // How far the cursor moved from the point before the correction.
        public Point Shift { get; private set; }

        // A multidimensional vector where first two coordinates represent 2d point on the display.
        // Other dimensions represent user body position.
        public float[] Coordinates{ get; private set; }
    }

    // |ShiftsStorage| is responsible for storing user corrections (shifts).
    // * It supports even spreading of the corrections on display. If user makes two corrections in the same (or near) position the new correction overwrites the old one.
    // * It handles saving and loading corrections from file.
    public class ShiftsStorage : IDisposable
    {
        public CalibrationWindow calibration_window = null;
        public List<UserCorrection> Corrections = new List<UserCorrection>();

        private Options.CalibrationMode calibration_mode;
        private ShiftStorageCache cache;

        private string DefaultPath { get; set; } 

        public ShiftsStorage(Options.CalibrationMode mode, ShiftStorageCache cache, bool for_testing)
        {
            calibration_mode = mode;
            DefaultPath = GetFilepath(Helpers.UserDataFolder);
            this.cache = cache;
            if (!for_testing)
                LoadFromFile();
        }

        public void Dispose()
        {
            FilesSavingQueue.FlushSynchroniously();
            if (calibration_window != null)
            {
                calibration_window.Close();
                calibration_window = null;
            }
        }
        public bool IsDebugWindowEnabled
        {
            get
            { return calibration_window != null; }

            set
            {
                App.Current.Dispatcher.BeginInvoke((Action)(() =>
              {
                  lock (Helpers.locker)
                  {
                      if (value && calibration_window == null)
                      {
                          calibration_window = new CalibrationWindow();
                          calibration_window.Show();
                          calibration_window.UpdateCorrections(Corrections);
                      }
                      else if (!value && calibration_window != null)
                      {
                          calibration_window.Close();
                          calibration_window = null;
                      }
                  }
              }));
            }
        }

        public void Reset()
        {
            Corrections.Clear();
            cache.Clear();
            OnShiftsChanged();
        }

        public void AddShift(float[] cursor_position, Point shift)
        {
            var closest_shifts = cache.ClosestPoints;
            if (closest_shifts != null && closest_shifts[0].distance < calibration_mode.zone_size)
            {
                cache.SaveToCache(cursor_position, closest_shifts[0].index);
                Corrections[closest_shifts[0].index] = new UserCorrection(cursor_position, shift);
                if (closest_shifts.Count > 1 && closest_shifts[1].distance < calibration_mode.zone_size)
                {
                    cache.FreeIndex(closest_shifts[1].index);
                    Corrections.RemoveAt(closest_shifts[1].index);
                }
            }
            else if (Corrections.Count() < calibration_mode.max_zones_count)
            {
                if (cache.AllocateIndex() != Corrections.Count)
                    throw new Exception("Logic error");
                cache.SaveToCache(cursor_position, Corrections.Count);
                Corrections.Add(new UserCorrection(cursor_position, shift));
            }
            else
            {
                var highest_density_point = GetClosestPointOfHihestDensity();
                cache.SaveToCache(cursor_position, highest_density_point);
                Corrections[highest_density_point] = new UserCorrection(cursor_position, shift);
            }

            OnShiftsChanged();
        }

        public void SaveInDirectory(string directory_path)
        {
            File.WriteAllText(GetFilepath(directory_path), GetSerializedContent());
        }

        private static string GetVector3PathPart(Vector3Percents vector)
        {
            return (vector.X > 0 ? "1" : "0") + (vector.Y > 0 ? "1" : "0") + (vector.Z > 0 ? "1" : "0");
        }

        private string GetFilepath(string directory_path)
        {
            var dimensions_config = calibration_mode.additional_dimensions_configuration;

            return Path.Combine(directory_path, "calibration" +
              GetVector3PathPart(dimensions_config.LeftEye) +
              GetVector3PathPart(dimensions_config.RightEye) +
              GetVector3PathPart(dimensions_config.AngleBetweenEyes) +
              GetVector3PathPart(dimensions_config.HeadPosition) +
              GetVector3PathPart(dimensions_config.HeadDirection) +
              ".json");
        }

        private void LoadFromFile()
        {
            try
            {
                Corrections.Clear();
                cache.Clear();
                if (!File.Exists(DefaultPath))
                    return;

                bool error_message_box_shown = false;

                Corrections = JsonConvert.DeserializeObject<List<UserCorrection>>(
                    File.ReadAllText(DefaultPath)).Where(x =>
                {
                    if (x.Coordinates == null)
                        return false;
                    if (x.Coordinates.Length != calibration_mode.additional_dimensions_configuration.CoordinatesCount)
                    {
                        if (!error_message_box_shown)
                        {
                            error_message_box_shown = true;
                            System.Windows.MessageBox.Show("Number of coordinates in the file doesn't fit options.");
                        }
                        return false;
                    }
                    return true;
                }).ToList();
                while (Corrections.Count > calibration_mode.max_zones_count)
                    Corrections.Remove(Corrections.Last());
                for (int i = 0; i < Corrections.Count; i++)
                {
                    if (cache.AllocateIndex() != i)
                        throw new Exception("Logic error");

                    cache.SaveToCache(Corrections[i].Coordinates, i);
                }

            }
            catch (Exception e)
            {
                Corrections = new List<UserCorrection>();
                cache.Clear();
                System.Windows.MessageBox.Show("Failed reading shifts storage: " + e.Message, Helpers.application_name, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private string GetSerializedContent()
        {
            return JsonConvert.SerializeObject(Corrections);
        }

        private void OnShiftsChanged()
        {
            FilesSavingQueue.Save(DefaultPath, GetSerializedContent);
            calibration_window?.UpdateCorrections(Corrections);
        }

        private static int GetSectorNumber(float coordinate)
        {
            return (int)(coordinate / 500.0);
        }

        private int GetSectorNumber(UserCorrection shift, int max_sector_x)
        {
            return GetSectorNumber(shift.Coordinates[0]) + 
                GetSectorNumber(shift.Coordinates[1]) * (max_sector_x + 1);
        }

        private int GetClosestPointOfHihestDensity()
        {
            Debug.Assert(Corrections.Count > 0);

            int max_sector_x = 0;
            for (int i = 0; i < Corrections.Count; i++)
            {
                int sector_x = GetSectorNumber(Corrections[i].Coordinates[0]);
                if (sector_x > max_sector_x)
                    max_sector_x = sector_x;
            }
            var sectors = new Dictionary<int /*number of sector*/, int /*Count of points in sector*/>();
            for (int i = 0; i < Corrections.Count; i++)
            {
                int sector_number = GetSectorNumber(Corrections[i], max_sector_x);
                if (!sectors.ContainsKey(sector_number))
                    sectors.Add(sector_number, 0);

                sectors[sector_number]++;
            }

            int max_points_count_in_sector = 0;
            for (int i = 0; i < sectors.Count; i++)
            {
                int points_number_in_sector = sectors.ElementAt(i).Value;
                if (points_number_in_sector > max_points_count_in_sector)
                    max_points_count_in_sector = points_number_in_sector;
            }


            int index_of_closest_point = 0;
            float min_distance = float.MaxValue;
            for (int i = 0; i < Corrections.Count; i++)
            {
                if (sectors[GetSectorNumber(Corrections[i], max_sector_x)] == max_points_count_in_sector)
                {
                    float distance = cache.GetDistanceFromCursor(i);
                    
                    if (min_distance > distance)
                    {
                        min_distance = distance;
                        index_of_closest_point = i;
                    }
                }
            }

            return index_of_closest_point;
        }

    }

    public static partial class Helpers
    {
        public static void NormalizeWeights(List<ShiftStorageCache.PointInfo> corrections)
        {
            float total_weight = 0;
            foreach (var correction in corrections)
                total_weight += correction.weight;

            for (int i = 0; i < corrections.Count; i++)
                corrections[i].weight = corrections[i].weight / total_weight;
        }

        public static Point GetWeightedAverage(ShiftsStorage shift_storage, List<ShiftStorageCache.PointInfo> corrections)
        {
            Point resulting_shift = new Point(0, 0);
            foreach (var correction in corrections)
            {
                resulting_shift.X += (int)(shift_storage.Corrections[correction.index].Shift.X * correction.weight);
                resulting_shift.Y += (int)(shift_storage.Corrections[correction.index].Shift.Y * correction.weight);
            }
            return resulting_shift;
        }

        public static float GetAngleBetweenVectors(
            ShiftStorageCache.PointInfo a,
            ShiftStorageCache.PointInfo b)
        {
            Debug.Assert(
                a.vector_from_correction_to_cursor.Length ==
                b.vector_from_correction_to_cursor.Length);

            float dot_product = 0;
            for (int i = 0; i < a.vector_from_correction_to_cursor.Length; i++)
                dot_product += a.vector_from_correction_to_cursor[i] * b.vector_from_correction_to_cursor[i];

            float cos = dot_product / a.distance / b.distance;
            if (cos > 1.0)
                cos = 1.0f;
            else if (cos < -1.0)
                cos = -1.0f;

            return (float)Math.Acos(cos);
        }
    }
}
