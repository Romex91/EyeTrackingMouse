using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                GenerateConfiguration(data_points);
                watch.Stop();

                MessageBox.Show("Finished! Elapsed time:" + watch.Elapsed.ToString());
            }, cancellation.Token);

            Closing += CalibrationModeGeneratorWindow_Closing;
        }

        public eye_tracking_mouse.Options.CalibrationMode BestCalibrationMode
        {
            get; set;
        }

        public List<Tuple<double, eye_tracking_mouse.Options.CalibrationMode>> GoodModes
        { get; } = new List<Tuple<double, eye_tracking_mouse.Options.CalibrationMode>>();

        private void TryAddToGoodModes(double utility, eye_tracking_mouse.Options.CalibrationMode mode)
        {
            if (utility <= 0)
                return;

            foreach(var good_mode in GoodModes)
            {
                if (good_mode.Item2.Equals(mode))
                {
                    good_mode.Item2.tag_for_testing += "+" + mode.tag_for_testing;
                    return;
                }
            }

            GoodModes.Add(new Tuple<double, eye_tracking_mouse.Options.CalibrationMode>(utility, mode));
        }

        private double global_best_utility = 0;

        private void CalibrationModeGeneratorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cancellation.Cancel();
            mode_generation_background_task.Wait();
        }

        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        private Task mode_generation_background_task = null;
        private long number_of_tests = 0;
        private long number_of_local_iterations = 0;
        private long number_of_global_iterations = 0;
        private long total_min_max_permutations = 0;
        private long remaining_min_max_permutations = 0;
        private void GenerateConfiguration(List<DataPoint> data_points)
        {
            eye_tracking_mouse.FilesSavingQueue.DisabledForTesting = true;

            eye_tracking_mouse.Options.Instance = new eye_tracking_mouse.Options();

            foreach (var mode in CalibrationModesForTesting.Short)
            {
                ForModeAndItsVariations(mode, (x,tag) => { 
                    MaxOutEachDimension(x.Clone(), data_points, tag); 
                    IncrementalImprove(x.Clone(), data_points, tag); });
                //ForEachMinMaxPermutation(mode, x => IncrementalImprove(x, data_points));
                number_of_global_iterations++;
            }
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

        private static OptionsField[] fields = new OptionsField[]
        {
            //OptionsField.BuildHardcoded(field_name : "zone_size", new List<int>{ 10, 15, 25, 50, 75, 100, 150, 200, 250, 350, 500, 800 }),
            //OptionsField.BuildHardcoded(field_name : "considered_zones_count", new List<int>{ 3,  6, 10, 20 }),
            //OptionsField.BuildLinear(field_name : "size_of_opaque_sector_in_percents", max : 70, min : 30, step: 10),
            //OptionsField.BuildLinear(field_name : "size_of_transparent_sector_in_percents", max : 60, min : 0, step: 10),
            //OptionsField.BuildHardcoded(field_name : "correction_fade_out_distance", new List<int>{ 50, 75, 100, 150, 200, 250, 350, 500, 800 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 2", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 3", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 4", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 5", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            OptionsField.BuildHardcoded(field_name : "coordinate 6", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            //OptionsField.BuildHardcoded(field_name : "coordinate 7", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            //OptionsField.BuildHardcoded(field_name : "coordinate 8", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            //OptionsField.BuildHardcoded(field_name : "coordinate 9", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
        };

        private void ForModeAndItsVariations(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            Action<eye_tracking_mouse.Options.CalibrationMode, string> callback)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                Text_CurrentPermutation.Text = JsonConvert.SerializeObject(starting_mode, Formatting.Indented);
            }));
            callback(starting_mode, "mid");
            List<OptionsField> enabled_fields = new List<OptionsField>();
            foreach (var field in fields)
            {
                if (GetFieldValue(starting_mode, field) != -1)
                {
                    enabled_fields.Add(field);
                }
            }

            for (int i = 0; i < 2; i++)
            {
                eye_tracking_mouse.Options.CalibrationMode mode = starting_mode.Clone();
                for (int field_number = 0; field_number < enabled_fields.Count; field_number++)
                {
                    if (i == 0)
                    {
                        SetFieldValue(mode, enabled_fields[field_number], enabled_fields[field_number].range.GetRange().Last());
                    }
                    else
                    {
                        SetFieldValue(mode, enabled_fields[field_number], enabled_fields[field_number].range.GetRange().First());
                    }
                }
                Dispatcher.Invoke((Action)(() =>
                {
                    Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                }));
                if (cancellation.Token.IsCancellationRequested)
                    return;
                callback(mode, i == 0 ? "max" : "min" );
            }
        }

        private void ForEachMinMaxPermutation(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            Action<eye_tracking_mouse.Options.CalibrationMode> callback)
        {
            callback(starting_mode);
            List<OptionsField> enabled_fields = new List<OptionsField>();

            long iterator = 1;

            foreach (var field in fields)
            {
                if (GetFieldValue(starting_mode, field) != -1)
                {
                    enabled_fields.Add(field);
                    // two additional permutations
                    iterator *= 2;
                }
            }

            total_min_max_permutations = iterator;
            remaining_min_max_permutations = 0;
            List<long> permutations = new List<long>();

            while (iterator > 0)
            {
                permutations.Add(iterator--);
            }
            permutations.Shuffle(new Random((int)(DateTime.Now.ToBinary() % int.MaxValue)));

            foreach (var permutation in permutations)
            {
                if (cancellation.Token.IsCancellationRequested)
                    return;

                eye_tracking_mouse.Options.CalibrationMode mode = starting_mode.Clone();

                for (int field_number = 0; field_number < enabled_fields.Count; field_number++)
                {
                    if ((permutation & (1 << field_number)) == 0)
                    {
                        SetFieldValue(mode, enabled_fields[field_number], enabled_fields[field_number].range.GetRange().Last());
                    }
                    else
                    {
                        SetFieldValue(mode, enabled_fields[field_number], enabled_fields[field_number].range.GetRange().First());
                    }
                }

                Dispatcher.Invoke((Action)(() =>
                {
                    Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                }));

                callback(mode);

                remaining_min_max_permutations++;
            }
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
                int[] coordinates_scales = calibration_mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
                coordinates_scales[coordinate_index] = value;
                calibration_mode.additional_dimensions_configuration.CoordinatesScalesInPercents = coordinates_scales;
            }
            else
            {
                var field = calibration_mode.GetType().GetField(options_field.field_name);
                field.SetValue(calibration_mode, value);
            }
        }

        private static eye_tracking_mouse.Options.CalibrationMode ChangeField(
            eye_tracking_mouse.Options.CalibrationMode calibration_mode,
            OptionsField field, 
            int steps_number)
        {
            var new_calibration_mode = calibration_mode.Clone();

            int value = GetFieldValue(new_calibration_mode, field);
            if (value == -1)
            {
                return null;
            }

            var range = field.range.GetRange();
            int i = 0;
            for (; i < range.Count; i++)
            {
                if (range[i] > value)
                {
                    break;
                }
            }

            i += steps_number;
            if (i < 0 || i >= range.Count)
                return null;

            SetFieldValue(new_calibration_mode, field, range[i]);
            return new_calibration_mode;
        }

        private void MaxOutEachDimension(
            eye_tracking_mouse.Options.CalibrationMode mode,
            List<DataPoint> data_points, string tag)
        {
            eye_tracking_mouse.Options.CalibrationMode local_best_calibration_mode = mode;
            double local_best_utility = 0;

            while (true)
            {
                double old_best_utility = local_best_utility;
                foreach (var field in fields)
                {
                    if (GetFieldValue(local_best_calibration_mode, field) == -1)
                        continue;

                    var range = field.range.GetRange();


                    List<eye_tracking_mouse.Options.CalibrationMode> modes_to_test = new List<eye_tracking_mouse.Options.CalibrationMode>();
                    for (int i = 0; i < range.Count; i++)
                    {
                        eye_tracking_mouse.Options.CalibrationMode calibration_mode = local_best_calibration_mode.Clone();
                        SetFieldValue(calibration_mode, field, range[i]);

                        if (cancellation.Token.IsCancellationRequested)
                            return;

                        modes_to_test.Add(calibration_mode);
                    }

                    RunTests(data_points, modes_to_test, ref local_best_calibration_mode, ref local_best_utility);
                }

                number_of_local_iterations++;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Text_LastIterationUtilityDelta.Text = "Last Iteration Utility Delta: " + (local_best_utility - old_best_utility);
                }));

                if (local_best_utility == old_best_utility)
                    break;
            }
            local_best_calibration_mode.tag_for_testing = tag + "_max_out";
            TryAddToGoodModes(local_best_utility, local_best_calibration_mode);
        }

        private bool IsModeCorrect(eye_tracking_mouse.Options.CalibrationMode mode)
        {
            return
                mode.size_of_opaque_sector_in_percents + mode.size_of_transparent_sector_in_percents < 91;
        }

        private void RunTests(
            List<DataPoint> data_points,
            List<eye_tracking_mouse.Options.CalibrationMode> modes,
            ref eye_tracking_mouse.Options.CalibrationMode local_best_mode,
            ref double local_best_utility)
        {
            number_of_tests += modes.Count;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Text_ProgressInfo.Text =
                    "Total Runned Tests: " + number_of_tests +
                    ". Local Iterations: " + number_of_local_iterations +
                    ". Global Iterations " + number_of_global_iterations +
                    ". MinMax Permutations " + remaining_min_max_permutations + "/" + total_min_max_permutations;
            }));

            List<Task<Helpers.TestResult>> tasks = new List<Task<Helpers.TestResult>>();
            foreach (var mode in modes)
            {
                tasks.Add(Task.Factory.StartNew<Helpers.TestResult>(() =>
                {
                    if (!IsModeCorrect(mode))
                        return new Helpers.TestResult();
                    return Helpers.TestCalibrationMode(data_points, mode);
                }));
            }

            eye_tracking_mouse.Options.CalibrationMode best_mode = null;
            double best_utility = 0;
            Helpers.TestResult best_result = null;

            for (int i = 0; i < tasks.Count; i++)
            {
                double task_utility = tasks[i].Result.UtilityFunction;
                if (best_mode == null || task_utility > best_utility)
                {
                    best_utility = task_utility;
                    best_mode = modes[i];
                    best_result = tasks[i].Result;
                }
            }

            Dispatcher.BeginInvoke((Action)(() =>
            {
                Text_LastTestResult.Text = "Last test results " + best_result.ToString() + " \n" + JsonConvert.SerializeObject(best_mode, Formatting.Indented);
            }));

            Debug.Assert(global_best_utility >= local_best_utility);

            if (best_utility > global_best_utility)
            {
                global_best_utility = best_utility;
                BestCalibrationMode = best_mode;

                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Text_GlobalBestModeInfo.Text = "Global Best Calibration Mode  " + best_result.ToString();
                }));
            }

            if (best_utility > local_best_utility)
            {
                local_best_utility = best_utility;
                local_best_mode = best_mode;

                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Text_LocalBestModeInfo.Text = "Local Best Calibration Mode  " + best_result.ToString();
                }));
            }
        }

        private void IncrementalImprove(
            eye_tracking_mouse.Options.CalibrationMode mode,
            List<DataPoint> data_points,
            string tag)
        {
            eye_tracking_mouse.Options.CalibrationMode local_best_calibration_mode = mode;
            double local_best_utility = 0;
            RunTests(
                data_points,
                new List<eye_tracking_mouse.Options.CalibrationMode> { local_best_calibration_mode },
                ref local_best_calibration_mode,
                ref local_best_utility);


            int steps_number = 0;
            while (true)
            {
                double old_best_utility = local_best_utility;

                List<eye_tracking_mouse.Options.CalibrationMode> modes_to_test = new List<eye_tracking_mouse.Options.CalibrationMode>();
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (GetFieldValue(local_best_calibration_mode, field) == -1)
                    {
                        continue;
                    }

                    if (cancellation.Token.IsCancellationRequested)
                        return;

                    eye_tracking_mouse.Options.CalibrationMode calibration_mode;
                    if ((calibration_mode = ChangeField(local_best_calibration_mode, field, steps_number)) != null)
                    {
                        modes_to_test.Add(calibration_mode);
                    }

                    if ((calibration_mode = ChangeField(local_best_calibration_mode, field, -steps_number)) != null)
                    {
                        modes_to_test.Add(calibration_mode);
                    }
                }

                if (modes_to_test.Count == 0)
                    break;

                RunTests(data_points, modes_to_test, ref local_best_calibration_mode, ref local_best_utility);

                if (local_best_utility == old_best_utility)
                    steps_number++;
                else
                    steps_number = 0;

                number_of_local_iterations++;
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    Text_LastIterationUtilityDelta.Text = "Last Iteration Utility Delta: " + (local_best_utility - old_best_utility);
                }));
            }

            local_best_calibration_mode.tag_for_testing = tag + "_incremental";
            TryAddToGoodModes(local_best_utility, local_best_calibration_mode);
        }
    }
}
