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

namespace BlindConfigurationTester
{
    using UtilityAndModePair = Tuple<float, eye_tracking_mouse.Options.CalibrationMode>;
    public static partial class Extensions
    {
        public static string GetUniqueKey(this eye_tracking_mouse.Options.CalibrationMode mode)
        {
            return JsonConvert.SerializeObject(mode, Formatting.None);
        }
    }

    class ExtremumSearcher
    {
        readonly object mutex = new object();
        HashSet<string> handled_extremums = new HashSet<string>();
        List<UtilityAndModePair> extremums_queue =
            new List<UtilityAndModePair>();

        List<DataPoint> data_points;

        Action<string, string> progress_info_callback;

        public List<UtilityAndModePair> Extremums { get; } = new List<UtilityAndModePair>();

        public ExtremumSearcher(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            List<DataPoint> data_points,
            Action<string, string> progress_info_callback)
        {
            this.data_points = data_points;
            this.progress_info_callback = progress_info_callback;
            extremums_queue.Add(new UtilityAndModePair(0, starting_mode));

            Task[] tasks = new Task[16];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = new Task(() =>
                {
                    TaskLoop();
                });
                tasks[i].Start();
            }
            Task.WaitAll(tasks);
        }

        private void TaskLoop()
        {
            while (true)
            {
                eye_tracking_mouse.Options.CalibrationMode queued_extremum = null;
                lock (mutex)
                {
                    if (extremums_queue.Count > 0)
                    {
                        queued_extremum = extremums_queue.Last().Item2;
                        extremums_queue.RemoveAt(extremums_queue.Count - 1);
                    }
                    else if (handled_extremums.Count > 0)
                    {
                        return;
                    }
                }

                if (queued_extremum == null)
                {
                    Thread.Sleep(500);
                    continue;
                }

                List<UtilityAndModePair> extremums = FindExtremums(queued_extremum, 2);

                lock (mutex)
                {
                    foreach (var extremum in extremums)
                    {
                        string key = extremum.Item2.GetUniqueKey();
                        if (handled_extremums.Contains(key))
                            continue;
                        handled_extremums.Add(key);
                        extremums_queue.Add(extremum);
                        Extremums.Add(extremum);
                    }
                    Extremums.Sort((x, y) => {
                        if (x.Item1 > y.Item1)
                            return 1;
                        else return -1;
                    });
                    extremums_queue.Sort((x, y) => {
                        if (x.Item1 > y.Item1)
                            return 1;
                        else return -1;
                    });
                    if (extremums_queue.Count > 30)
                        extremums_queue.RemoveRange(0, extremums_queue.Count - 30);

                    if (Extremums.Count > 100)
                        Extremums.RemoveRange(0, Extremums.Count - 100);

                    string progress_info = "Queue. Size: "  + extremums_queue.Count + " \nContent: ";
                    for (int i = extremums_queue.Count - 10; i < extremums_queue.Count; i++)
                    {
                        progress_info += extremums_queue[i].Item1 + " ";
                    }
                    progress_info += "\n Extremums handled: " + handled_extremums.Count() + "\n";

                    progress_info += "Top extremums.: ";
                    for (int i = Extremums.Count - 10; i < Extremums.Count; i++)
                    {
                        progress_info += Extremums[i].Item1 + " ";
                    }

                    string current_best_extremum = JsonConvert.SerializeObject(
                        Extremums.Last().Item2, 
                        Formatting.Indented);
                    progress_info_callback(progress_info, current_best_extremum);
                }
            }
        }
        int ConvertIndexesToSingleInt(int[] indexes, int[] range_sizes)
        {
            Debug.Assert(indexes.Length == range_sizes.Length);
            int[] index_scales = new int[indexes.Length];
            {
                int index_scale = 1;
                for (int i = 0; i < indexes.Length; i++)
                {
                    index_scales[i] = index_scale;
                    index_scale *= range_sizes[i];
                }
            }

            int retval = 0;
            for (int i = 0; i < indexes.Length; i++)
            {
                retval += indexes[i] * index_scales[i];
            }
            return retval;
        }

        void ForEachIndexesCombination(
            int[] indexes,
            int[] range_sizes,
            int starting_range,
            Action<int[]> callback)
        {
            Debug.Assert(indexes.Length == range_sizes.Length);
            for (int i = 0; i < range_sizes[starting_range]; i++)
            {
                indexes[starting_range] = i;
                if (starting_range + 1 < indexes.Length)
                    ForEachIndexesCombination(indexes, range_sizes, starting_range + 1, callback);
                else
                    callback(indexes);
            }
        }

        Dictionary<int, UtilityAndModePair>  LaunchTests(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            List<CalibrationModeIterator.OptionsField> options_to_iterate)
        {
            Dictionary<int, UtilityAndModePair> test_results = new Dictionary<int, UtilityAndModePair>();

            int[] starting_indexes = new int[options_to_iterate.Count];
            int[] range_sizes = new int[options_to_iterate.Count];
            for (int i = 0; i < options_to_iterate.Count; i++)
            {
                starting_indexes[i] = 0;
                range_sizes[i] = options_to_iterate[i].Range.Length;
            }

            ForEachIndexesCombination(starting_indexes, range_sizes, 0, (indexes) =>
            {
                var mode = starting_mode.Clone();
                for (int i = 0; i < options_to_iterate.Count; i++)
                {
                    options_to_iterate[i].SetFieldValue(mode, options_to_iterate[i].Range[indexes[i]]);
                }

                test_results.Add(ConvertIndexesToSingleInt(indexes, range_sizes),
                    new UtilityAndModePair(
                        Helpers.TestCalibrationMode(data_points, mode).UtilityFunction,
                        mode));
            });

            return test_results;
        }

        List<UtilityAndModePair> FindExtremums(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            List<CalibrationModeIterator.OptionsField> options_to_iterate)
        {
            Dictionary<int, UtilityAndModePair> test_results = LaunchTests(starting_mode, options_to_iterate);
            int[] starting_indexes = new int[options_to_iterate.Count];
            int[] range_sizes = new int[options_to_iterate.Count];
            for (int i = 0; i < options_to_iterate.Count; i++)
            {
                starting_indexes[i] = 0;
                range_sizes[i] = options_to_iterate[i].Range.Length;
            }

            HashSet<int> non_extremum_indexes = new HashSet<int>();
            List<UtilityAndModePair> extremums = new List<UtilityAndModePair>();
            ForEachIndexesCombination(starting_indexes, range_sizes, 0, (indexes) =>
            {
                bool is_extremum = true;
                if (non_extremum_indexes.Contains(ConvertIndexesToSingleInt(indexes, range_sizes)))
                {
                    is_extremum = false;
                }

                var utility_and_mode_pair = test_results[ConvertIndexesToSingleInt(indexes, range_sizes)];
                Action<int[]> compare_to_neighbors_action = (cloned_indexes) =>
                {
                    double utility = test_results[ConvertIndexesToSingleInt(cloned_indexes, range_sizes)].Item1;
                    if (utility > utility_and_mode_pair.Item1)
                    {
                        is_extremum = false;
                    }
                    else if (utility == utility_and_mode_pair.Item1)
                    {
                        non_extremum_indexes.Add(ConvertIndexesToSingleInt(cloned_indexes, range_sizes));
                    }
                };

                for (int i = 0; i < indexes.Length; i++)
                {
                    if (indexes[i] > 0)
                    {
                        int[] cloned_indexes = (int[])indexes.Clone();
                        cloned_indexes[i]--;
                        compare_to_neighbors_action(cloned_indexes);
                    } 
                    
                    if (indexes[i] < range_sizes[i] - 1)
                    {
                        int[] cloned_indexes = (int[])indexes.Clone();
                        cloned_indexes[i]++;
                        compare_to_neighbors_action(cloned_indexes);
                    }
                }

                if (is_extremum)
                {
                    extremums.Add(utility_and_mode_pair);
                }
            });

            return extremums;
        }

        static IEnumerable<IEnumerable<T>>
          GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }

        List<UtilityAndModePair> FindExtremums(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            int dimensions_count_per_step)
        {
            var iterator = new CalibrationModeIterator(starting_mode);
            int[] fields_indexes = new int[iterator.Fields.Count];
            for (int i = 0; i < fields_indexes.Length; i++)
            {
                fields_indexes[i] = i;
            }
            var fields_indexes_permutations = GetKCombs(fields_indexes, dimensions_count_per_step);

            List<UtilityAndModePair> retval = new List<UtilityAndModePair>();

            foreach (var indexes_permutation in fields_indexes_permutations)
            {
                Debug.Assert(dimensions_count_per_step == indexes_permutation.Count());
                var selected_fields = new List<CalibrationModeIterator.OptionsField>(dimensions_count_per_step);
                foreach(var index in indexes_permutation)
                {
                    selected_fields.Add(iterator.Fields[index]);
                }
                foreach (var index in indexes_permutation)
                {
                    retval.InsertRange(retval.Count, FindExtremums(starting_mode, selected_fields));
                }
            }

            return retval;
        }
    }
}
