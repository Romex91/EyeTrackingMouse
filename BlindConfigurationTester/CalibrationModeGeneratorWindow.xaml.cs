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
using System.Windows.Navigation;
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

            mode_generation_background_task = Task.Factory.StartNew(async () =>
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                try
                {
                   await GenerateConfiguration(data_points);
                } finally
                {
                    watch.Stop();
                    MessageBox.Show("Finished! Elapsed time:" + watch.Elapsed.ToString());
                }
            }, cancellation.Token);

            Closing += CalibrationModeGeneratorWindow_Closing;
        }

        public class Results 
        {
            public UtilityAndModePair best_calibration_mode = new UtilityAndModePair (0, null);
            public List<UtilityAndModePair> linear_search_results = new List<UtilityAndModePair>();
            public List<UtilityAndModePair> extremum_search_results = new List<UtilityAndModePair>();
        }

        private Results results = new Results();

        public async Task<Results> GetResults()
        {
            try
            {
                await mode_generation_background_task.ConfigureAwait(true);
            }
            catch (OperationCanceledException)
            {
            }
            return results;
        }

        private void TryAddToLinearResults(float utility, eye_tracking_mouse.Options.CalibrationMode mode)
        {
            if (utility <= 0)
                return;

            foreach(var good_mode in results.linear_search_results)
            {
                if (good_mode.mode.Equals(mode))
                {
                    good_mode.mode.tag_for_testing += "+" + mode.tag_for_testing;
                    return;
                }
            }

            results.linear_search_results.Add(new UtilityAndModePair(utility, mode));
        }

        private void CalibrationModeGeneratorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs event_args)
        {
            cancellation.Cancel();
        }

        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        private Task mode_generation_background_task = null;
        private long number_of_tests = 0;
        private long number_of_cached_results_reused = 0;
        private long number_of_local_iterations = 0;
        private long number_of_global_iterations = 0;
        private long total_min_max_permutations = 0;
        private long remaining_min_max_permutations = 0;

        private async Task GenerateConfiguration(List<DataPoint> data_points)
        {
            eye_tracking_mouse.Options.Instance = new eye_tracking_mouse.Options();
            CalibrationModeIterator iterator;

            foreach (var mode in CalibrationModesForTesting.Short)
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                }));

                iterator = new CalibrationModeIterator(mode);
                iterator.ForModeAndItsVariations(mode, (x, tag) =>
                {
                    Dispatcher.Invoke((Action)(() =>
                    {
                        Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                    }));
                    MaxOutEachDimension(x.Clone(), data_points, tag);
                    IncrementalImprove(x.Clone(), data_points, tag);
                });

                //iterator.ForEachMinMaxPermutation(mode, x => {
                //    Dispatcher.Invoke((Action)(() =>
                //    {
                //        Text_CurrentPermutation.Text = JsonConvert.SerializeObject(mode, Formatting.Indented);
                //    }));
                //    remaining_min_max_permutations++;
                //    IncrementalImprove(x, data_points, tag); });
                number_of_global_iterations++;
            }

            if (results.best_calibration_mode == null)
                return;

            number_of_tests = 0;

            iterator = new CalibrationModeIterator(results.best_calibration_mode.mode);

            var extremum_searcher = new ExtremumSearcher(
                results.best_calibration_mode.mode, 
                data_points,
                (ExtremumSearcher.TestResultsInfo info) =>
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        number_of_cached_results_reused += info.cached_results_reused;
                        number_of_tests += info.modes_tested;
                        Text_ExtremumsProgressInfo.Text = 
                            "number of tests during extremums search: " + number_of_tests + 
                            " max tests number:"  + iterator.NumberOfDifferentModes  +
                            " number of cached results reused " + number_of_cached_results_reused;
                    }));
                },
                cancellation.Token);

            while(true)
            {
                var new_extremums = await extremum_searcher.SearchNext();
                if (new_extremums == null)
                    break;

                results.extremum_search_results.AddRange(new_extremums);
                results.extremum_search_results.Sort((x, y) => {
                    if (x.utility < y.utility)
                        return 1;
                    else return -1;
                });
                if (results.extremum_search_results.Count > 1000)
                {
                    results.extremum_search_results.RemoveRange(
                        1000, results.extremum_search_results.Count - 1000);
                }
                if (results.best_calibration_mode.utility < results.extremum_search_results.First().utility)
                    results.best_calibration_mode = results.extremum_search_results.First();

                string progress_info = extremum_searcher.QueueInfo;
                Dispatcher.Invoke((Action)(() =>
                {
                    progress_info += "Top extremums.: ";
                    for (int i = 0; 
                         i < results.extremum_search_results.Count && i < 10;
                         i++)
                    {
                        progress_info += results.extremum_search_results[i].utility + " ";
                    }

                    Text_ExtremumsQueueInfo.Text = progress_info;
                }));
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
                    var range = field.Range;

                    List<eye_tracking_mouse.Options.CalibrationMode> modes_to_test = new List<eye_tracking_mouse.Options.CalibrationMode>();
                    for (int i = 0; i < range.Length; i++)
                    {
                        eye_tracking_mouse.Options.CalibrationMode calibration_mode = local_best_calibration_mode.Clone();
                        field.SetFieldValue(calibration_mode, range[i]);

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
            TryAddToLinearResults(local_best_utility, local_best_calibration_mode);
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

            cancellation.Token.ThrowIfCancellationRequested();

            List<Task<Helpers.TestResult>> tasks = new List<Task<Helpers.TestResult>>();
            foreach (var mode in modes)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                tasks.Add(Task.Factory.StartNew<Helpers.TestResult>(() =>
                {
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

            Debug.Assert(results.best_calibration_mode.utility >= local_best_utility);

            if (best_utility > results.best_calibration_mode.utility)
            {
                results.best_calibration_mode = new UtilityAndModePair(best_utility, best_mode);

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

                    eye_tracking_mouse.Options.CalibrationMode calibration_mode = local_best_calibration_mode.Clone();
                    if (field.Increment(calibration_mode, steps_number))
                    {
                        modes_to_test.Add(calibration_mode);
                    }

                    calibration_mode = local_best_calibration_mode.Clone();
                    if (field.Increment(calibration_mode, -steps_number))
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
            TryAddToLinearResults(local_best_utility, local_best_calibration_mode);
        }
    }
}
