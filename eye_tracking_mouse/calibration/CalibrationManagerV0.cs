﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{

    // Takes average of closest corrections.
    class CalibrationManagerV0 : ICalibrationManager
    {
        private readonly ShiftsStorage shift_storage;
        private readonly Options.CalibrationMode calibration_mode;
        public CalibrationManagerV0(Options.CalibrationMode mode, bool for_testing)
        {
            calibration_mode = mode;
            shift_storage = new ShiftsStorage(calibration_mode, for_testing);
        }

        public Point GetShift(float[] cursor_position)
        {
            if (cursor_position.Length != calibration_mode.additional_dimensions_configuration.CoordinatesCount)
                return new Point(0, 0);

            var closest_corrections = shift_storage.GetClosestCorrections(cursor_position);
            if (closest_corrections == null)
            {
                Debug.Assert(shift_storage.Corrections.Count() == 0 || !Helpers.AreCoordinatesSane(cursor_position));
                return new Point(0, 0);
            }

            float sum_of_reverse_distances = 0;
            foreach (var index in closest_corrections)
            {
                sum_of_reverse_distances += (1 / index.distance);
            }

            foreach (var correction in closest_corrections)
            {
                correction.weight = 1 / correction.distance / sum_of_reverse_distances;
            }

            var result = Helpers.GetWeightedAverage(closest_corrections);

            if (shift_storage.calibration_window != null)
            {
                var lables = new List<Tuple<string /*text*/, System.Windows.Point>>();
                foreach (var correction in closest_corrections)
                    lables.Add(new Tuple<string, System.Windows.Point>((int)(correction.weight * 100) + "%", 
                        new System.Windows.Point(
                            correction.correction.сoordinates[0], 
                            correction.correction.сoordinates[1])));
                shift_storage.calibration_window.UpdateCorrectionsLables(lables);
                shift_storage.calibration_window.UpdateCurrentCorrection(new EyeTrackerErrorCorrection(cursor_position, result));
            }

            return result;
        }

        public void AddShift(float[] coordinates, Point shift)
        {
            shift_storage.AddShift(coordinates, shift);
        }

        public void Dispose()
        {
            shift_storage.Dispose();
        }

        public void Reset()
        {
            shift_storage.Reset();
        }

        public bool IsDebugWindowEnabled
        {
            get => shift_storage.IsDebugWindowEnabled;
            set => shift_storage.IsDebugWindowEnabled = value;
        }

        public void SaveInDirectory(string directory_path)
        {

            shift_storage.SaveInDirectory(directory_path);
        }

    }
}
