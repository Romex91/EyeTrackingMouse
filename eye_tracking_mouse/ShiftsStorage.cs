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
using System.Windows.Media;

namespace eye_tracking_mouse
{
    // When users correct inacurate precision with W/A/S/D and then click stuff they create a |EyeTrackerErrorCorrection|.
    public class EyeTrackerErrorCorrection
    {
        public EyeTrackerErrorCorrection(float[] coordinates, Point shift)
        {
            сoordinates = coordinates;
            this.shift = shift;
        }

        // Vector coming from the uncalibrated gaze point to the 'true' gaze point.
        //
        // cursor position = eye tracker output + |shift|.
        public Point shift;

        // A multidimensional vector where first two coordinates represent 2d point on the display.
        // Other dimensions represent user body position.
        public float[] сoordinates { get; private set; }
    }

    // |ShiftsStorage| is responsible for storing error corrections (shifts).
    // * It supports even spreading of the corrections on display. If user makes two corrections in the same (or near) position the new correction overwrites the old one.
    // * It handles saving and loading corrections from file.
    public class ShiftsStorage : IDisposable
    {
        public CalibrationWindow calibration_window = null;

        // WARNING: Changes will invalidate |cache|! Make sure you keep it up to date.
        public List<EyeTrackerErrorCorrection> Corrections = new List<EyeTrackerErrorCorrection>();

        private Options.CalibrationMode calibration_mode;
        private ShiftStorageCache cache;

        private string DefaultPath { get; set; }

        public ShiftsStorage(Options.CalibrationMode mode, bool for_testing)
        {
            calibration_mode = mode;
            DefaultPath = GetFilepath(Helpers.UserDataFolder);
            this.cache = new ShiftStorageCache(mode);
            if (!for_testing)
                LoadFromFile();
        }

        public class PointInfo
        {
            public EyeTrackerErrorCorrection correction;
            public float distance;
            public float[] vector_from_correction_to_cursor;
            public float weight;
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

        public void SaveInDirectory(string directory_path)
        {
            File.WriteAllText(GetFilepath(directory_path), GetSerializedContent());
        }

        public void AddShift(float[] cursor_position, Point shift)
        {
            if (!Helpers.AreCoordinatesSane(cursor_position))
                return;
            cache.ChangeCursorPosition(cursor_position);
            var closest_shifts = cache.ClosestPoints;
            if (closest_shifts != null && closest_shifts[0].distance < calibration_mode.zone_size)
            {
                cache.FreeIndex(closest_shifts[0].index);
                Corrections.RemoveAt(closest_shifts[0].index);
                if (closest_shifts.Count > 1 && closest_shifts[1].distance < calibration_mode.zone_size)
                {
                    Debug.Assert(closest_shifts[1].index != closest_shifts[0].index);
                    if (closest_shifts[1].index > closest_shifts[0].index)
                        closest_shifts[1].index--;
                    cache.FreeIndex(closest_shifts[1].index);
                    Corrections.RemoveAt(closest_shifts[1].index);
                }
            }
            else if (Corrections.Count >= calibration_mode.max_zones_count)
            {
                Debug.Assert(Corrections.Count == calibration_mode.max_zones_count);
                // Remove least recently used item from the front of |Corrections|.
                cache.FreeIndex(0);
                Corrections.RemoveAt(0);
            }

            if (cache.AllocateIndex() != Corrections.Count)
                throw new Exception("Logic error");
            cache.SaveToCache(cursor_position, Corrections.Count);
            Corrections.Add(new EyeTrackerErrorCorrection(cursor_position, shift));

            OnShiftsChanged();
        }

        public List<PointInfo> GetClosestCorrections(float[] cursor_position)
        {
            if (!Helpers.AreCoordinatesSane(cursor_position))
                return null;
            List<PointInfo> retval = new List<PointInfo>();

            cache.ChangeCursorPosition(cursor_position);
            calibration_window?.OnCursorPositionUpdate(cursor_position);

            if (cache.ClosestPoints == null)
                return null;

            foreach (var point_info in cache.ClosestPoints)
                retval.Add(new PointInfo
                {
                    correction = Corrections[point_info.index],
                    distance = point_info.distance,
                    vector_from_correction_to_cursor = point_info.vector_from_correction_to_cursor,
                    weight = 1,
                });

            MoveClosestPointBack();

            return retval;
        }

        // We move the closest point to the end of |Corrections| to enable LRU deletion.
        private void MoveClosestPointBack()
        {
            int i = cache.ClosestPoints[0].index;

            if (cache.ClosestPoints.Count > 0 && i > 0)
            {
                var tmp = Corrections[i];
                cache.FreeIndex(i);
                Corrections.RemoveAt(i);

                if (cache.AllocateIndex()!= Corrections.Count)
                    throw new Exception("Logic error");
                cache.SaveToCache(tmp.сoordinates, Corrections.Count);
                Corrections.Add(tmp);
            }
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

                var corrections = JsonConvert.DeserializeObject<List<EyeTrackerErrorCorrection>>(
                    File.ReadAllText(DefaultPath)).Where(x =>
                {
                    if (x.сoordinates == null)
                        return false;
                    if (x.сoordinates.Length != calibration_mode.additional_dimensions_configuration.CoordinatesCount)
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
                foreach(var correction in corrections)
                {
                    AddShift(correction.сoordinates, correction.shift);
                }
            }
            catch (Exception e)
            {
                Corrections = new List<EyeTrackerErrorCorrection>();
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
    }

    public static partial class Helpers
    {
        public static bool AreCoordinatesSane(float[] coordinates)
        {
            for (int i = 0; i < coordinates.Length; i++)
            {
                if (float.IsNaN(coordinates[i]) || float.IsInfinity(coordinates[i]))
                    return false;
            }
            return true;
        }

        public static void NormalizeWeights(List<ShiftsStorage.PointInfo> corrections)
        {
            float total_weight = 0;
            foreach (var correction in corrections)
                total_weight += correction.weight;

            for (int i = 0; i < corrections.Count; i++)
                corrections[i].weight = corrections[i].weight / total_weight;
        }

        public static Point GetWeightedAverage(IEnumerable<ShiftsStorage.PointInfo> corrections)
        {
            Point resulting_shift = new Point(0, 0);
            foreach (var point in corrections)
            {
                resulting_shift.X += (int)(point.correction.shift.X * point.weight);
                resulting_shift.Y += (int)(point.correction.shift.Y * point.weight);
            }
            return resulting_shift;
        }

        public static float GetAngleBetweenVectors(
            ShiftsStorage.PointInfo a,
            ShiftsStorage.PointInfo b)
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
