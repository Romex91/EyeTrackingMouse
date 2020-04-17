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

        public List<Tuple<float, eye_tracking_mouse.Options.CalibrationMode>> GoodModes
        { get; } = new List<Tuple<float, eye_tracking_mouse.Options.CalibrationMode>>();

        private void TryAddToGoodModes(float utility, eye_tracking_mouse.Options.CalibrationMode mode)
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

            GoodModes.Add(new Tuple<float, eye_tracking_mouse.Options.CalibrationMode>(utility, mode));
        }

        private float global_best_utility = 0;

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
                CalibrationModeIterator iterator = new CalibrationModeIterator(mode);
                Dispatcher.Invoke((Action)(() =>
                {
                    Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                }));
                iterator.ForModeAndItsVariations(mode, (x,tag) => {
                    Dispatcher.Invoke((Action)(() =>
                    {
                        Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                    }));
                    MaxOutEachDimension(x.Clone(), data_points, tag); 
                    IncrementalImprove(x.Clone(), data_points, tag); });

                //iterator.ForEachMinMaxPermutation(mode, x => {
                //    Dispatcher.Invoke((Action)(() =>
                //    {
                //        Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                //    }));
                //    remaining_min_max_permutations++;
                //    IncrementalImprove(x, data_points, tag); });
                number_of_global_iterations++;
            }
        }

        private void MaxOutEachDimension(
            eye_tracking_mouse.Options.CalibrationMode mode,
            List<DataPoint> data_points, string tag)
        {
            eye_tracking_mouse.Options.CalibrationMode local_best_calibration_mode = mode;
            float local_best_utility = 0;

            CalibrationModeIterator iterator = new CalibrationModeIterator(mode);

            while (true)
            {
                float old_best_utility = local_best_utility;
                foreach (var field in iterator.Fields)
                {
                    var range = field.range.GetRange();

                    List<eye_tracking_mouse.Options.CalibrationMode> modes_to_test = new List<eye_tracking_mouse.Options.CalibrationMode>();
                    for (int i = 0; i < range.Count; i++)
                    {
                        eye_tracking_mouse.Options.CalibrationMode calibration_mode = local_best_calibration_mode.Clone();
                        field.SetFieldValue(calibration_mode, range[i]);

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
            ref float local_best_utility)
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
            float best_utility = 0;
            Helpers.TestResult best_result = null;

            for (int i = 0; i < tasks.Count; i++)
            {
                float task_utility = tasks[i].Result.UtilityFunction;
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
            float local_best_utility = 0;
            RunTests(
                data_points,
                new List<eye_tracking_mouse.Options.CalibrationMode> { local_best_calibration_mode },
                ref local_best_calibration_mode,
                ref local_best_utility);

            CalibrationModeIterator iterator = new CalibrationModeIterator(mode);

            int steps_number = 0;
            while (true)
            {
                float old_best_utility = local_best_utility;

                List<eye_tracking_mouse.Options.CalibrationMode> modes_to_test = new List<eye_tracking_mouse.Options.CalibrationMode>();
                for (int i = 0; i < iterator.Fields.Count; i++)
                {
                    var field = iterator.Fields[i];

                    if (cancellation.Token.IsCancellationRequested)
                        return;

                    eye_tracking_mouse.Options.CalibrationMode calibration_mode;
                    if ((calibration_mode = field.Increment(local_best_calibration_mode, steps_number)) != null)
                    {
                        modes_to_test.Add(calibration_mode);
                    }

                    if ((calibration_mode = field.Increment(local_best_calibration_mode, -steps_number)) != null)
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
