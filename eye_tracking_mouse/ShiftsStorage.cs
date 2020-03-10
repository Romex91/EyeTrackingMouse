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

        public ShiftsStorage()
        {
            Settings.OptionsChanged += OnSettingsChanged;
            LoadFromFile();
        }

        public void Dispose()
        {
            Settings.OptionsChanged -= OnSettingsChanged;
            FilesSavingQueue.FlushSynchroniously();
            if (calibration_window != null)
            {
                calibration_window.Close();
                calibration_window = null;
            }
        }

        public void ToggleDebugWindow()
        {
            lock(Helpers.locker)
            {
                if (calibration_window == null)
                {
                    calibration_window = new CalibrationWindow();
                    calibration_window.Show();
                    calibration_window.UpdateCorrections(Corrections);
                }
                else
                {
                    calibration_window.Close();
                    calibration_window = null;
                }
            }
        }

        public void Reset()
        {
            lock (Helpers.locker)
            {
                Corrections.Clear();
                OnShiftsChanged();
            }
        }

        public void AddShift(ShiftPosition cursor_position, Point shift)
        {
            var closest_shifts =  Helpers.CalculateClosestCorrectionsInfo(this, cursor_position, 2);
            if (closest_shifts != null && closest_shifts[0].distance < Options.Instance.calibration_mode.zone_size)
            {
                Corrections[closest_shifts[0].index] = new UserCorrection(cursor_position, shift);
                if (closest_shifts.Count > 1 && closest_shifts[1].distance < Options.Instance.calibration_mode.zone_size)
                    Corrections.RemoveAt(closest_shifts[1].index);
            }
            else if (Corrections.Count() < Options.Instance.calibration_mode.max_zones_count)
            {
                Corrections.Add(new UserCorrection(cursor_position, shift));
            }
            else
            {
                Corrections[GetClosestPointOfHihestDensity(cursor_position)] = new UserCorrection(cursor_position, shift);
            }

            OnShiftsChanged();
        }

        private static string GetVector3PathPart(Vector3Bool vector)
        {
            return (vector.X ? "1" : "0") + (vector.Y ? "1" : "0") + (vector.Z ? "1" : "0");
        }

        private static string Filepath
        {
            get
            {
                var dimensions_config = Options.Instance.calibration_mode.additional_dimensions_configuration;

                return Path.Combine(Helpers.UserDataFolder, "calibration" +
                  GetVector3PathPart(dimensions_config.LeftEye) +
                  GetVector3PathPart(dimensions_config.RightEye) +
                  GetVector3PathPart(dimensions_config.AngleBetweenEyes) +
                  GetVector3PathPart(dimensions_config.HeadPosition) +
                  GetVector3PathPart(dimensions_config.HeadDirection) +
                  ".json");
            }
        }

        private void LoadFromFile()
        {
            try
            {
                Corrections.Clear();
                if (!File.Exists(Filepath))
                    return;

                bool error_message_box_shown = false;

                Corrections = JsonConvert.DeserializeObject<List<UserCorrection>>(File.ReadAllText(Filepath)).Where(x=> {
                    if (x.Position.Count != Options.Instance.calibration_mode.additional_dimensions_configuration.CoordinatesCount)
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
                System.Windows.MessageBox.Show("Failed reading shifts storage: " + e.Message, Helpers.application_name, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            // Adjust to new calibration zone size.
            for (int i = 0; i < Corrections.Count - 1;)
            {
                bool did_remove = false;
                for (int j = i + 1; j < Corrections.Count; j++)
                {
                    if (Helpers.GetVectorLength(Corrections[j].Position - Corrections[i].Position) < Options.Instance.calibration_mode.zone_size)
                    {
                        did_remove = true;
                        Corrections.RemoveAt(i);
                        break;
                    }
                }

                if (!did_remove)
                    i++;
            }

            // Adjust to new calibration zones count.
            while (Corrections.Count > Options.Instance.calibration_mode.max_zones_count)
                Corrections.RemoveAt(0);

            OnShiftsChanged();
        }

        private string GetSerializedContent()
        {
            lock (Helpers.locker)
            {
                return JsonConvert.SerializeObject(Corrections);
            }
        }

        private void OnShiftsChanged()
        {
            lock (Helpers.locker)
            {
                FilesSavingQueue.Save(Filepath, GetSerializedContent);
                calibration_window?.UpdateCorrections(Corrections);
            }
        }

        private int GetSectorNumber(UserCorrection shift, int max_sector_x)
        {
            return shift.Position.SectorX + shift.Position.SectorY * (max_sector_x + 1);
        }

        private int GetClosestPointOfHihestDensity(ShiftPosition cursor_position)
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
                    double distance = Helpers.GetVectorLength(Corrections[i].Position - cursor_position);
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
        public static void NormalizeWeights(List<CorrectionInfoRelatedToCursor> corrections)
        {
            double total_weight = 0;
            foreach (var correction in corrections)
                total_weight += correction.weight;

            for (int i = 0; i < corrections.Count; i++)
                corrections[i].weight = corrections[i].weight / total_weight;
        }

        public static Point GetWeightedAverage(ShiftsStorage shift_storage, List<CorrectionInfoRelatedToCursor> corrections)
        {
            Point resulting_shift = new Point(0, 0);
            foreach (var correction in corrections)
            {
                resulting_shift.X += (int)(shift_storage.Corrections[correction.index].Shift.X * correction.weight);
                resulting_shift.Y += (int)(shift_storage.Corrections[correction.index].Shift.Y * correction.weight);
            }
            return resulting_shift;
        }

        public class CorrectionInfoRelatedToCursor
        {
            public int index;
            public ShiftPosition vector_from_cursor;
            public double distance;
            public double weight;
        }

        public static List<CorrectionInfoRelatedToCursor> CalculateClosestCorrectionsInfo(ShiftsStorage storage, ShiftPosition cursor_position, int number)
        {
            var Corrections = storage.Corrections;
            if (Corrections.Count == 0)
                return null;

            var retval = new List<CorrectionInfoRelatedToCursor>();
            for (int i = 0; i < Corrections.Count(); i++)
            {
                CorrectionInfoRelatedToCursor info = new CorrectionInfoRelatedToCursor { index = i, vector_from_cursor = Corrections[i].Position - cursor_position };
                info.distance = Helpers.GetVectorLength(info.vector_from_cursor);
                if (info.distance < 0.001)
                    info.distance = 0.001;

                int j = 0;
                for (; j < retval.Count; j++)
                {
                    if (info.distance < retval[j].distance)
                    {
                        retval.Insert(j, info);
                        break;
                    }
                }
                if (j == retval.Count)
                    retval.Add(info);

                if (retval.Count > number)
                    retval.RemoveAt(retval.Count - 1);
            }
            return retval;
        }

        public static double GetVectorLength(ShiftPosition vector)
        {
            double squared_distance = 0;
            for (int i = 0; i < vector.Count; i++)
            {
                squared_distance += Math.Pow(vector[i], 2);
            }

            return Math.Pow(squared_distance, 0.5);
        }

        public static double GetAngleBetweenVectors(CorrectionInfoRelatedToCursor a, CorrectionInfoRelatedToCursor b)
        {
            Debug.Assert(a.vector_from_cursor.Count == b.vector_from_cursor.Count);
            double dot_product = 0;

            for (int i = 0; i < a.vector_from_cursor.Count; i++)
                dot_product += a.vector_from_cursor[i] * b.vector_from_cursor[i];

            return Math.Acos(dot_product / a.distance / b.distance);
        }
    }
}
