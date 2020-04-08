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
        public UserCorrection(ShiftPosition position, Point shift)
        {
            Position = position;
            Shift = shift;
        }

        // How far the cursor moved from the point before the correction.
        public Point Shift { get; private set; }

        // Where the correction took place on the screen and what posture user had during the correction.
        public ShiftPosition Position { get; private set; }
    }

    // |ShiftsStorage| is responsible for storing user corrections (shifts).
    // * It supports even spreading of the corrections on display. If user makes two corrections in the same (or near) position the new correction overwrites the old one.
    // * It handles saving and loading corrections from file.
    public class ShiftsStorage : IDisposable
    {
        public CalibrationWindow calibration_window = null;
        public List<UserCorrection> Corrections = new List<UserCorrection>();

        private Options.CalibrationMode calibration_mode;
        private ShiftPositionCache cache;

        private string DefaultPath { get; set; } 

        public ShiftsStorage(Options.CalibrationMode mode, ShiftPositionCache cache)
        {
            calibration_mode = mode;
            DefaultPath = GetFilepath(Helpers.UserDataFolder);
            this.cache = cache;
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

        public void AddShift(ShiftPosition cursor_position, Point shift)
        {
            var closest_shifts = CalculateClosestCorrectionsInfo(2);
            if (closest_shifts != null && closest_shifts[0].distance < calibration_mode.zone_size)
            {
                Corrections[closest_shifts[0].index].Position.DeleteFromLongTermMemory();
                Corrections[closest_shifts[0].index] = new UserCorrection(cursor_position.SaveToLongTermMemory(), shift);
                if (closest_shifts.Count > 1 && closest_shifts[1].distance < calibration_mode.zone_size)
                {
                    Corrections[closest_shifts[1].index].Position.DeleteFromLongTermMemory();
                    Corrections.RemoveAt(closest_shifts[1].index);
                }
            }
            else if (Corrections.Count() < calibration_mode.max_zones_count)
            {
                Corrections.Add(new UserCorrection(cursor_position.SaveToLongTermMemory(), shift));
            }
            else
            {
                var highest_density_point = GetClosestPointOfHihestDensity();
                Corrections[highest_density_point].Position.DeleteFromLongTermMemory();
                Corrections[highest_density_point] = new UserCorrection(cursor_position.SaveToLongTermMemory(), shift);
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
                    File.ReadAllText(DefaultPath),
                    cache
                    ).Where(x =>
                {
                    if (x.Position.Count != calibration_mode.additional_dimensions_configuration.CoordinatesCount)
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
            FilesSavingQueue.Save(GetFilepath(DefaultPath), GetSerializedContent);
            calibration_window?.UpdateCorrections(Corrections);
        }

        private int GetSectorNumber(UserCorrection shift, int max_sector_x)
        {
            return shift.Position.SectorX + shift.Position.SectorY * (max_sector_x + 1);
        }

        private int GetClosestPointOfHihestDensity()
        {
            Debug.Assert(Corrections.Count > 0);

            int max_sector_x = 0;
            for (int i = 0; i < Corrections.Count; i++)
                if (Corrections[i].Position.SectorX > max_sector_x)
                    max_sector_x = Corrections[i].Position.SectorX;

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
            double min_distance = double.MaxValue;
            for (int i = 0; i < Corrections.Count; i++)
            {
                if (sectors[GetSectorNumber(Corrections[i], max_sector_x)] == max_points_count_in_sector)
                {
                    double distance = Corrections[i].Position.DistanceFromCursor;
                    
                    if (min_distance > distance)
                    {
                        min_distance = distance;
                        index_of_closest_point = i;
                    }
                }
            }

            return index_of_closest_point;
        }

        public class CorrectionInfoRelatedToCursor
        {
            public ShiftsStorage shifts_storage;

            public double[] VectorFromCorrectionToCursor
            {
                get {
                    return shifts_storage.Corrections[index].Position.SubtractionResult;
                }
            }
            public int index;
            public double distance;
            public double weight;
        }

        public List<CorrectionInfoRelatedToCursor> CalculateClosestCorrectionsInfo(int number)
        {
            if (Corrections.Count == 0)
                return null;

            var retval = new List<CorrectionInfoRelatedToCursor>();
            var tmp_info = new CorrectionInfoRelatedToCursor
            {
                shifts_storage = this
            };

            double max_distance = 0;
            int corrections_count = Corrections.Count;
            for (int i = 0; i < corrections_count; i++)
            {
                double distance = Corrections[i].Position.DistanceFromCursor;

                if (distance < 0.001)
                    distance = 0.001;
                if (distance > max_distance)
                {
                    if (retval.Count < number)
                        max_distance = distance;
                    else
                        continue;
                }

                if (retval.Count < number)
                {
                    int j = 0;
                    for (; j < retval.Count; j++)
                    {
                        if (distance < retval[j].distance)
                        {
                            break;
                        }
                    }

                    retval.Insert(j, new CorrectionInfoRelatedToCursor
                    {
                        index = i,
                        distance = distance,
                        weight = 1,
                        shifts_storage = this
                    });
                    continue;
                }

                tmp_info.distance = distance;
                tmp_info.index = i;

                for (int j = 0; j < retval.Count; j++)
                {
                    if (tmp_info.distance < retval[j].distance)
                    {
                        var t = retval[retval.Count - 1];
                        retval.RemoveAt(retval.Count - 1);
                        retval.Insert(j, tmp_info);
                        tmp_info = t;
                        break;
                    }
                }

            }
            return retval;
        }
    }

    public static partial class Helpers
    {
        public static void NormalizeWeights(List<ShiftsStorage.CorrectionInfoRelatedToCursor> corrections)
        {
            double total_weight = 0;
            foreach (var correction in corrections)
                total_weight += correction.weight;

            for (int i = 0; i < corrections.Count; i++)
                corrections[i].weight = corrections[i].weight / total_weight;
        }

        public static Point GetWeightedAverage(ShiftsStorage shift_storage, List<ShiftsStorage.CorrectionInfoRelatedToCursor> corrections)
        {
            Point resulting_shift = new Point(0, 0);
            foreach (var correction in corrections)
            {
                resulting_shift.X += (int)(shift_storage.Corrections[correction.index].Shift.X * correction.weight);
                resulting_shift.Y += (int)(shift_storage.Corrections[correction.index].Shift.Y * correction.weight);
            }
            return resulting_shift;
        }

        public static double GetAngleBetweenVectors(
        	ShiftsStorage.CorrectionInfoRelatedToCursor a, 
        	ShiftsStorage.CorrectionInfoRelatedToCursor b)
        {
            var vector_to_cursor_a = a.VectorFromCorrectionToCursor;
            var vector_to_cursor_b = b.VectorFromCorrectionToCursor;


            Debug.Assert(vector_to_cursor_a.Length == vector_to_cursor_b.Length);

            double dot_product = 0;
            for (int i = 0; i < vector_to_cursor_a.Length; i++)
                dot_product += vector_to_cursor_a[i] * vector_to_cursor_b[i];

            double cos = dot_product / a.distance / b.distance;
            if (cos > 1.0)
                cos = 1.0;
            else if (cos < -1.0)
                cos = -1.0;

            return Math.Acos(cos);
        }
    }
}
