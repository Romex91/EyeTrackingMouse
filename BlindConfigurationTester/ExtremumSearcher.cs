using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BlindConfigurationTester
{
    public class UtilityAndModePair
    {
        public UtilityAndModePair(
            float utility, 
            eye_tracking_mouse.Options.CalibrationMode mode)
        {
            this.utility = utility;
            this.mode = mode;
        }

        public float utility;
        public eye_tracking_mouse.Options.CalibrationMode mode;
    }

    class ExtremumSearcher
    {
        HashSet<long> handled_extremums = new HashSet<long>();
        List<UtilityAndModePair> extremums_queue =
            new List<UtilityAndModePair>();
        CancellationToken cancellation_token;
        List<DataPoint> data_points;
        Action<TestResultsInfo> test_results_info_callback;

        // Subtasks can only read this field. Each task has separate |new_test_results| to write test results.
        // After each search iteration when each task finishes its job |new_test_results| are merged to |previous_test_results|.
        Dictionary<long, float> previous_test_results = new Dictionary<long, float>();

        CalibrationModeIterator iterator;
        public class TestResultsInfo
        {
            public int modes_tested;
            public int cached_results_reused;
        }

        public ExtremumSearcher(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            List<DataPoint> data_points,
            Action<TestResultsInfo> test_results_info_callback,
            CancellationToken cancellation_token)
        {
            this.data_points = data_points;
            this.cancellation_token = cancellation_token;
            this.test_results_info_callback = test_results_info_callback;

            iterator = new CalibrationModeIterator(starting_mode);
            handled_extremums.Add(iterator.GetUniqueKey(starting_mode));
            extremums_queue.Add(new UtilityAndModePair(0, starting_mode));
        }

        public string QueueInfo { get; private set; }

        public async Task<List<UtilityAndModePair>> SearchNext()
        {
            if (extremums_queue.Count == 0)
                return null;

            var retval = new List<UtilityAndModePair>();

            const int max_tasks_count = 12;
            var tasks = new Task<List<UtilityAndModePair>>[Math.Min(max_tasks_count, extremums_queue.Count)];
            var new_test_results_array = new Dictionary<long, float>[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                new_test_results_array[i] = new Dictionary<long, float>();
                var new_test_results = new_test_results_array[i];
                var mode = extremums_queue[i].mode;
                tasks[i] = Task.Factory.StartNew<List<UtilityAndModePair>>(() =>
                {
                    return FindNeighbourExtremums(mode, new_test_results);
                }, cancellation_token);
            }

            extremums_queue.RemoveRange(0, tasks.Length);

            List<UtilityAndModePair> new_extremums = new List<UtilityAndModePair>();
            for (int i = 0; i < tasks.Length; i++)
            {
                new_extremums.InsertRange(new_extremums.Count, await tasks[i]);
            }

            // merge cached results
            foreach(var new_test_results in new_test_results_array)
            {
                new_test_results.ToList().ForEach(x =>
                {
                    if (!previous_test_results.ContainsKey(x.Key))
                    {
                        previous_test_results.Add(x.Key, x.Value);
                    }
                });
            }

            foreach (var extremum in new_extremums)
            {
                long key = iterator.GetUniqueKey(extremum.mode);
                if (handled_extremums.Contains(key))
                    continue;
                handled_extremums.Add(key);
                extremums_queue.Add(extremum);
                retval.Add(extremum);
            }

            extremums_queue.Sort((x, y) => {
                if (x.utility < y.utility)
                    return 1;
                else return -1;
            });

            QueueInfo = "Queue. Size: " + extremums_queue.Count + " \nContent: ";
            for (int i = 0; i < extremums_queue.Count && i < 10; i++)
            {
                QueueInfo += extremums_queue[i].utility + " ";
            }
            QueueInfo += "\n Extremums handled: " + handled_extremums.Count() + "\n";

            return retval;
        }

        float ComputeModeUtility(
            eye_tracking_mouse.Options.CalibrationMode mode,
            Dictionary<long, float> new_test_results,
            TestResultsInfo info)
        {
            long key = iterator.GetUniqueKey(mode);


            if (previous_test_results.ContainsKey(key))
            {
                info.cached_results_reused++;
                return previous_test_results[key];
            }

            if (new_test_results.ContainsKey(key))
            {
                info.cached_results_reused++;
                return new_test_results[key];
            }

            info.modes_tested++;
            float utility = Helpers.TestCalibrationMode(data_points, mode).UtilityFunction;
            new_test_results.Add(key, utility);

            return utility;
        }

        int ConvertIndexesToSingleInt(int i, int j, int k, int i_max, int j_max)
        {
            return i + j * i_max + k * j_max;
        }

        List<UtilityAndModePair> FindNeighbourExtremums(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            List<CalibrationModeIterator.OptionsField> options_to_iterate,
            Dictionary<long, float> new_test_results)
        {
            TestResultsInfo test_results_info = new TestResultsInfo { cached_results_reused = 0, modes_tested = 0 };
            int i_max = options_to_iterate[0].Range.Length;
            int j_max = options_to_iterate[1].Range.Length;
            int k_max = options_to_iterate[2].Range.Length;

            Debug.Assert(options_to_iterate.Count == 3);
            var test_results = new UtilityAndModePair[i_max, j_max, k_max];

            for (int i = 0; i < i_max; i++)
            {
                for(int j = 0; j < j_max; j++)
                {
                    for (int k = 0; k < k_max; k++)
                    {
                        var mode = starting_mode.Clone();
                        options_to_iterate[0].SetFieldValue(mode, options_to_iterate[0].Range[i]);
                        options_to_iterate[1].SetFieldValue(mode, options_to_iterate[1].Range[j]);
                        options_to_iterate[2].SetFieldValue(mode, options_to_iterate[2].Range[k]);

                        cancellation_token.ThrowIfCancellationRequested();
                        test_results[i, j, k] = 
                            new UtilityAndModePair(
                                ComputeModeUtility(mode, new_test_results, test_results_info),
                                mode);
                    }
                }
            }

            test_results_info_callback(test_results_info);

            HashSet<int> non_extremum_indexes = new HashSet<int>();
            List<UtilityAndModePair> extremums = new List<UtilityAndModePair>();
            for (int i = 0; i < i_max; i++)
            {
                for (int j = 0; j < j_max; j++)
                {
                    for (int k = 0; k < k_max; k++)
                    {
                        bool is_extremum = true;
                        if (non_extremum_indexes.Contains(ConvertIndexesToSingleInt(i, j, k, i_max, j_max)))
                        {
                            is_extremum = false;
                        }


                        // n means neighbor
                        for (int n_i = Math.Max(0, i - 1); n_i <= i + 1 && n_i < i_max; n_i++)
                        {
                            for (int n_j = Math.Max(0, j - 1); n_j <= j + 1 && n_j < j_max; n_j++)
                            {
                                for (int n_k = Math.Max(0, k - 1); n_k <= k + 1 && n_k < k_max; n_k++)
                                {
                                    if (i == n_i && j == n_j && k == n_k)
                                        continue;
                                    if (test_results[i,j,k].utility < test_results[n_i, n_j, n_k].utility)
                                    {
                                        is_extremum = false;
                                    } else if (test_results[i, j, k].utility == test_results[n_i, n_j, n_k].utility)
                                    {
                                        non_extremum_indexes.Add(ConvertIndexesToSingleInt(
                                            n_i, 
                                            n_j, 
                                            n_k, 
                                            i_max, 
                                            j_max));
                                    }
                                }
                            }
                        }

                        if (is_extremum)
                        {
                            extremums.Add(test_results[i, j, k]);
                        }
                    }
                }
            }

            return extremums;
        }

        List<UtilityAndModePair> FindNeighbourExtremums(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            Dictionary<long, float> new_test_results )
        {
            List<UtilityAndModePair> retval = new List<UtilityAndModePair>();

            for(int i = 0; i < iterator.Fields.Count - 2; i++)
            {
                var selected_fields = new List<CalibrationModeIterator.OptionsField> {
                   iterator.Fields[i],
                   iterator.Fields[i+1],
                   iterator.Fields[i+2],
                };
                retval.InsertRange(retval.Count, FindNeighbourExtremums(starting_mode, selected_fields, new_test_results));
            }
            return retval;
        }
    }
}
