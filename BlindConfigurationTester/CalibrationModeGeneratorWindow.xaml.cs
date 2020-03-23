using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BlindConfigurationTester
{
    /// <summary>
    /// Interaction logic for CalibrationModeGeneratorWindow.xaml
    /// </summary>
    public partial class CalibrationModeGeneratorWindow : Window
    {
        public CalibrationModeGeneratorWindow(List<DataPoint> data_points)
        {
            InitializeComponent();

            mode_generation_background_task = Task.Factory.StartNew(() =>
            {

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                BestCalibrationMode = GenerateConfiguration(data_points);
                watch.Stop();

                MessageBox.Show("Finished! Elapsed time:" + watch.Elapsed.ToString());
            }, cancellation.Token);

            Closing += CalibrationModeGeneratorWindow_Closing;
        }

        public eye_tracking_mouse.Options.CalibrationMode BestCalibrationMode
        {
            get; set;
        }

        private void CalibrationModeGeneratorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancellation.Cancel();
        }

        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        private Task mode_generation_background_task = null;
        private long number_of_tests = 0;
        private long number_of_local_iterations = 0;
        private long number_of_global_iterations = 0;

        private eye_tracking_mouse.Options.CalibrationMode GenerateConfiguration(List<DataPoint> data_points)
        {
            eye_tracking_mouse.FilesSavingQueue.DisabledForTesting = true;

            eye_tracking_mouse.Options.Instance = new eye_tracking_mouse.Options();
            var best_calibration_mode = eye_tracking_mouse.Options.Instance.calibration_mode;
            double best_utility = Helpers.TestCalibrationMode(data_points, best_calibration_mode).UtilityFunction;

            foreach (var mode in calibration_modes_to_test)
            {
                var maximized_calibration_mode = MaxOutEachDimension(mode, data_points);
                if (maximized_calibration_mode == null)
                    return null;

                var result = Helpers.TestCalibrationMode(data_points, maximized_calibration_mode);
                double utility = result.UtilityFunction;
                if (utility > best_utility)
                {
                    best_utility = utility;
                    best_calibration_mode = maximized_calibration_mode;

                    Dispatcher.Invoke((Action)(() =>
                    {
                        Text_GlobalBestModeInfo.Text = "Global Best Calibration Mode Utility: " + utility + ". Single test time(ms): " + result.time_ms;
                    }));
                }

                number_of_global_iterations++;
            }

            return best_calibration_mode;
        }

        private interface IntRange
        {
            List<int> GetRange();
        }

        private struct OptionsField
        {
            public string field_name;
            public IntRange range;

            public static OptionsField BuildLinear(string field_name, int min, int max, int step)
            {
                return new OptionsField
                {
                    field_name = field_name,
                    range = new IteartionRange { min_value = min, max_value = max, step = step, exponential = false }
                };
            }
            public static OptionsField BuildExponential(string field_name, int min, int max, int step)
            {
                return new OptionsField
                {
                    field_name = field_name,
                    range = new IteartionRange { min_value = min, max_value = max, step = step, exponential = true }
                };
            }
            public static OptionsField BuildHardcoded(string field_name, List<int> range)
            {
                return new OptionsField
                {
                    field_name = field_name,
                    range = new HardcodedIntRange { range = range }
                };
            }
        }

        private struct IteartionRange : IntRange
        {
            public int min_value;
            public int max_value;
            public int step;
            public bool exponential;

            public List<int> GetRange()
            {
                List<int> range = new List<int>();


                int i = min_value;
                while (true)
                {
                    range.Add(i >= max_value ? max_value : i);

                    if (i >= max_value)
                    {
                        break;
                    }
                    if (exponential)
                    {
                        i *= step;
                    }
                    else
                    {
                        i += step;
                    }
                }

                return range;
            }
        }

        private struct HardcodedIntRange : IntRange
        {
            public List<int> range;
            public List<int> GetRange()
            {
                return range;
            }
        }



        const double eps = 0.0001;


        private static OptionsField[] fields = new OptionsField[]
        {
            OptionsField.BuildLinear(field_name : "zone_size", max : 2000, min : 1, step: 10),
            OptionsField.BuildExponential(field_name : "max_zones_count", max : 2000, min : 1, step: 2),
            OptionsField.BuildHardcoded(field_name : "considered_zones_count", new List<int>{ 1, 2, 3, 4, 6, 10, 20 }),
            OptionsField.BuildLinear(field_name : "size_of_opaque_sector_in_percents", max : 100, min : 0, step: 10),
            OptionsField.BuildLinear(field_name : "size_of_transparent_sector_in_percents", max : 100, min : 0, step: 10),
            OptionsField.BuildLinear(field_name : "shade_thickness_in_pixels", max : 1000, min : 0, step: 10),

            OptionsField.BuildHardcoded(field_name : "coordinate 1", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 2", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 3", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 4", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 5", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 6", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 7", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 8", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 9", new List<int> {1, 10, 50, 100, 250, 300, 600, 1000, 10000 }),
    };


        private static eye_tracking_mouse.Options.CalibrationMode[] calibration_modes_to_test = new eye_tracking_mouse.Options.CalibrationMode[]
            {
            new eye_tracking_mouse.Options.CalibrationMode
            {
                considered_zones_count = 5,
                max_zones_count = 200,
                shade_thickness_in_pixels = 10,
                size_of_opaque_sector_in_percents = 30,
                size_of_transparent_sector_in_percents = 30,
                zone_size = 150,

                algorithm = "V1",
                update_period_ms = 0,
                additional_dimensions_configuration =
                    new eye_tracking_mouse.AdditionalDimensionsConfguration
                    {
                        LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                        RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                        AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 0 },
                        HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                        HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                    }
            },
            new eye_tracking_mouse.Options.CalibrationMode
            {
                considered_zones_count = 5,
                max_zones_count = 200,
                shade_thickness_in_pixels = 10,
                size_of_opaque_sector_in_percents = 30,
                size_of_transparent_sector_in_percents = 30,
                zone_size = 150,

                algorithm = "V1",
                update_period_ms = 0,
                additional_dimensions_configuration =
                    new eye_tracking_mouse.AdditionalDimensionsConfguration
                    {
                        LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                        RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                        AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                        HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                        HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 }
                    }
            },
            new eye_tracking_mouse.Options.CalibrationMode
            {
                considered_zones_count = 5,
                max_zones_count = 200,
                shade_thickness_in_pixels = 10,
                size_of_opaque_sector_in_percents = 30,
                size_of_transparent_sector_in_percents = 30,
                zone_size = 150,

                algorithm = "V1",
                update_period_ms = 0,
                additional_dimensions_configuration =
                new eye_tracking_mouse.AdditionalDimensionsConfguration
                {
                    LeftEye = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 },
                    RightEye = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 0, Y = 0, Z = 0 },
                    HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 700, Y = 700, Z = 700 }
                }
            },
            new eye_tracking_mouse.Options.CalibrationMode
            {
                considered_zones_count = 5,
                max_zones_count = 200,
                shade_thickness_in_pixels = 10,
                size_of_opaque_sector_in_percents = 30,
                size_of_transparent_sector_in_percents = 30,
                zone_size = 150,

                algorithm = "V0",
                update_period_ms = 0,
                additional_dimensions_configuration = eye_tracking_mouse.AdditionalDimensionsConfguration.Disabled
            }
            };


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
                return (int)field.GetValue(calibration_mode);
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
            if (value == -1)
            {
                return null;
            }

            var range = field.range.GetRange();
            for (int i = 0; i < range.Count; i++)
            {
                if (range[i] > value)
                {
                    SetFieldValue(new_calibration_mode, field, range[i]);
                    return new_calibration_mode;
                }
            }
            return null;
        }

        private static eye_tracking_mouse.Options.CalibrationMode DecrementField(
            eye_tracking_mouse.Options.CalibrationMode calibration_mode,
            OptionsField field)
        {
            var new_calibration_mode = calibration_mode.Clone();

            int value = GetFieldValue(new_calibration_mode, field);
            if (value == -1)
            {
                return null;
            }

            var range = field.range.GetRange();
            for (int i = range.Count - 1; i >= 0; i--)
            {
                if (range[i] < value)
                {
                    SetFieldValue(new_calibration_mode, field, range[i]);
                    return new_calibration_mode;
                }
            }

            return null;
        }

        private eye_tracking_mouse.Options.CalibrationMode MaxOutEachDimension(
        eye_tracking_mouse.Options.CalibrationMode mode,
        List<DataPoint> data_points)
        {
            eye_tracking_mouse.Options.CalibrationMode best_calibration_mode = mode;
            double best_utility = 0;

            while (true)
            {
                double old_best_utility = best_utility;
                foreach (var field in fields)
                {
                    if (GetFieldValue(best_calibration_mode, field) == -1)
                        continue;

                    var range = field.range.GetRange();

                    for (int i = 0; i < range.Count; i++)
                    {
                        eye_tracking_mouse.Options.CalibrationMode calibration_mode = best_calibration_mode.Clone();
                        SetFieldValue(calibration_mode, field, range[i]);

                        number_of_tests++;

                        Dispatcher.Invoke((Action)(() =>
                        {
                            Text_ProgressInfo.Text =
                                "Total Runned Tests: " + number_of_tests +
                                ". Local Iterations: " + number_of_local_iterations +
                                ". Global Iterations " + number_of_global_iterations;
                        }));


                        if (cancellation.Token.IsCancellationRequested)
                            return null;

                        var results = Helpers.TestCalibrationMode(data_points, calibration_mode);
                        double utility = results.UtilityFunction;

                        Dispatcher.Invoke((Action)(() =>
                        {
                            Text_LastTestResult.Text = "Last test results " + results.ToString() + ". Delta: " + (utility - old_best_utility);
                        }));

                        if (utility > best_utility)
                        {
                            best_utility = utility;
                            best_calibration_mode = calibration_mode;

                            Dispatcher.Invoke((Action)(() =>
                            {
                                Text_LocalBestModeInfo.Text = "Local Best Calibration Mode  " + results.ToString() + ". Delta: " + (utility - old_best_utility);
                            }));
                        }
                    }
                }

                number_of_local_iterations++;
                Dispatcher.Invoke((Action)(() =>
                {
                    Text_LastIterationUtilityDelta.Text = "Last Iteration Utility Delta: " + (best_utility - old_best_utility);
                }));

                if (best_utility - old_best_utility < eps)
                    break;
            }

            return best_calibration_mode;
        }

        private static eye_tracking_mouse.Options.CalibrationMode IncrementalImproveFromCurrentOptionsState(List<DataPoint> data_points)
        {
            eye_tracking_mouse.Options.CalibrationMode best_calibration_mode = eye_tracking_mouse.Options.Instance.calibration_mode;
            double best_utility = Helpers.TestCalibrationMode(data_points, best_calibration_mode).UtilityFunction;

            while (true)
            {
                double old_best_utility = best_utility;
                foreach (var field in fields)
                {
                    if (GetFieldValue(best_calibration_mode, field) == -1)
                        continue;

                    eye_tracking_mouse.Options.CalibrationMode calibration_mode;
                    while ((calibration_mode = IncrementField(best_calibration_mode, field)) != null)
                    {
                        double utility = Helpers.TestCalibrationMode(data_points, calibration_mode).UtilityFunction;
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

                    while ((calibration_mode = DecrementField(best_calibration_mode, field)) != null)
                    {
                        double utility = Helpers.TestCalibrationMode(data_points, calibration_mode).UtilityFunction;
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
                if (best_utility - old_best_utility < eps)
                    break;
            }

            return best_calibration_mode;
        }
    }
}
