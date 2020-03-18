using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlindConfigurationTester
{
    class CalibrationModeGenerator
    {
        private struct OptionsField
        {
            public string field_name;
            public int starting_value;
            public int min_value;
            public int max_value;
        }

        const double eps = 0.1;

        private static OptionsField[] fields = new OptionsField[]
        {
            new OptionsField{field_name = "zone_size", max_value = 2000, min_value = 1},
            new OptionsField{field_name = "max_zones_count", max_value = 2000, min_value = 1},
            new OptionsField{field_name = "considered_zones_count", max_value = 100, min_value = 1},
            new OptionsField{field_name = "size_of_opaque_sector_in_percents", max_value = 100, min_value = 0},
            new OptionsField{field_name = "size_of_transparent_sector_in_percents", max_value = 100, min_value = 0},
            new OptionsField{field_name = "shade_thickness_in_pixels", max_value = 1000, min_value = 0},
            new OptionsField{field_name = "coordinate 1", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 2", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 3", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 4", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 5", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 6", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 7", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 8", max_value = 1000, min_value = 1},
            new OptionsField{field_name = "coordinate 9", max_value = 1000, min_value = 1}
        };

        private static Helpers.TestResult RunTest(List<DataPoint> data_points, eye_tracking_mouse.Options.CalibrationMode calibration_mode)
        {
            eye_tracking_mouse.Options.Instance.calibration_mode = calibration_mode;
            eye_tracking_mouse.Options.CalibrationMode.Changed?.Invoke(null, null);
            var calibration_manager = eye_tracking_mouse.CalibrationManager.Instance;
            calibration_manager.Reset();
            return Helpers.TestCalibrationManager(calibration_manager, data_points);
        }

        public static eye_tracking_mouse.Options.CalibrationMode GenerateConfiguration(List<DataPoint> data_points)
        {
            eye_tracking_mouse.FilesSavingQueue.DisabledForTesting = true;
            eye_tracking_mouse.Options.Instance = new eye_tracking_mouse.Options();
            return MaxOutEachDimension(data_points);
        }

        private static int GetFieldValue(
            eye_tracking_mouse.Options.CalibrationMode calibration_mode,
            OptionsField options_field)
        {
            if (options_field.field_name.StartsWith("coordinate"))
            {
                int coordinate_index = int.Parse(options_field.field_name.Split(' ')[1]);
                if (coordinate_index >= calibration_mode.additional_dimensions_configuration.CoordinatesCount)
                {
                    return -1;
                }
                return calibration_mode.additional_dimensions_configuration.CoordinatesScalesInPercents[coordinate_index];
            }
            else
            {
                var field = calibration_mode.GetType().GetField(options_field.field_name);
                return (int) field.GetValue(calibration_mode);
            }
        }

        private static void SetFieldValue(
            eye_tracking_mouse.Options.CalibrationMode calibration_mode,
            OptionsField options_field, int value)
        {
            if (options_field.field_name.StartsWith("coordinate"))
            {
                int coordinate_index = int.Parse(options_field.field_name.Split(' ')[1]);
                if (coordinate_index >= calibration_mode.additional_dimensions_configuration.CoordinatesCount)
                {
                    return;
                }
                calibration_mode.additional_dimensions_configuration.CoordinatesScalesInPercents[coordinate_index] = value;
            }
            else
            {
                var field = calibration_mode.GetType().GetField(options_field.field_name);
                field.SetValue(calibration_mode, value);
            }
        }

        private static eye_tracking_mouse.Options.CalibrationMode IncrementField(
            eye_tracking_mouse.Options.CalibrationMode calibration_mode,
            OptionsField field)
        {
            var new_calibration_mode = calibration_mode.Clone();

            int value = GetFieldValue(new_calibration_mode, field);
            if (value == -1 || value >= field.max_value)
            {
                return null;
            }

            SetFieldValue(new_calibration_mode, field, value + 1);
            return new_calibration_mode;
        }

        private static eye_tracking_mouse.Options.CalibrationMode DecrementField(
            eye_tracking_mouse.Options.CalibrationMode calibration_mode,
            OptionsField field)
        {
            var new_calibration_mode = calibration_mode.Clone();

            int value = GetFieldValue(new_calibration_mode, field);
            if (value == -1 || value <= field.min_value)
            {
                return null;
            }

            SetFieldValue(new_calibration_mode, field, value + 1);
            return new_calibration_mode;
        }

        private static eye_tracking_mouse.Options.CalibrationMode MaxOutEachDimension(List<DataPoint> data_points)
        {
            eye_tracking_mouse.Options.CalibrationMode best_calibration_mode = eye_tracking_mouse.Options.Instance.calibration_mode;
            double best_utility = RunTest(data_points, best_calibration_mode).UtilityFunction;

            while (true)
            {
                double old_best_utility = best_utility;
                foreach (var field in fields)
                {
                    if (GetFieldValue(best_calibration_mode, field) == -1)
                        continue;

                    for (int field_value = field.min_value; field_value <= field.max_value; field_value++)
                    {
                        eye_tracking_mouse.Options.CalibrationMode calibration_mode = best_calibration_mode.Clone();
                        SetFieldValue(calibration_mode, field, field_value);
                        double utility = RunTest(data_points, calibration_mode).UtilityFunction;
                        if (utility > best_utility)
                        {
                            best_utility = utility;
                            best_calibration_mode = calibration_mode;
                        }
                    }
                }
                if (best_utility - old_best_utility < eps)
                    break;
            }

            return best_calibration_mode;
        }

        private static eye_tracking_mouse.Options.CalibrationMode IncrementalImproveFromCurrentOptionsState(List<DataPoint> data_points)
        {
            eye_tracking_mouse.Options.CalibrationMode best_calibration_mode = eye_tracking_mouse.Options.Instance.calibration_mode;
            double best_utility = RunTest(data_points, best_calibration_mode).UtilityFunction;

            while (true)
            {
                double old_best_utility = best_utility;
                foreach (var field in fields)
                {
                    for (int i = field.min_value; i <= field.max_value; i++)
                    {
                        if (GetFieldValue(best_calibration_mode, field) == -1)
                            continue;

                        eye_tracking_mouse.Options.CalibrationMode calibration_mode;
                        while ((calibration_mode = IncrementField(best_calibration_mode, field)) != null) {
                            double utility = RunTest(data_points, calibration_mode).UtilityFunction;
                            if (utility > best_utility)
                            {
                                best_utility = utility;
                                best_calibration_mode = calibration_mode;
                            } else
                            {
                                break;
                            }   
                        }

                        while ((calibration_mode = DecrementField(best_calibration_mode, field)) != null)
                        {
                            double utility = RunTest(data_points, calibration_mode).UtilityFunction;
                            if (utility > best_utility)
                            {
                                best_utility = utility;
                                best_calibration_mode = calibration_mode;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                if (best_utility - old_best_utility < eps)
                    break;
            }

            return best_calibration_mode;
        }
    }
}
