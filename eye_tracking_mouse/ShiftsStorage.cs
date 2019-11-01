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
    class ShiftsStorage
    {
        private static string Filepath { get { return Path.Combine(Helpers.GetLocalFolder(), "calibration.json"); } }
        private List<Tuple<Point, Point>> shifts_ = new List<Tuple<Point, Point>>();

        private DateTime last_save_time = DateTime.Now;

        public Task save_to_file_task;

        public static ShiftsStorage Instance { get; set; } = LoadFromFile();

        public static ShiftsStorage LoadFromFile()
        {
            var storage = new ShiftsStorage();
            if (File.Exists(Filepath))
            {
                try
                {
                    storage.shifts_ = JsonConvert.DeserializeObject<List<Tuple<Point, Point>>>(File.ReadAllText(Filepath));
                } catch (Exception) {}
            }
            return storage;
        }

        public Point GetShift(Point cursor_position)
        {
            var closest_indices = GetClosestShiftIndexes(cursor_position);
            if (closest_indices == null)
            {
                Debug.Assert(shifts_.Count() == 0);
                return new Point(0, 0);
            }

            double sum_of_reverse_distances = 0;
            foreach(var index in closest_indices)
            {
                sum_of_reverse_distances += (1/index.Item2);
            }

            Point resulting_shift = new Point(0, 0);
            foreach (var index in closest_indices)
            {
                resulting_shift.X += (int)(shifts_[index.Item1].Item2.X / index.Item2 / sum_of_reverse_distances);
                resulting_shift.Y += (int)(shifts_[index.Item1].Item2.Y / index.Item2 / sum_of_reverse_distances);
            }

            return resulting_shift;
        }

        public void Reset()
        {
            shifts_.Clear();
        }
        public void ResetClosest(Point position)
        {
            var closest_indices = GetClosestShiftIndexes(position);
            if (closest_indices != null)
            {
                shifts_.RemoveAt(closest_indices[0].Item1);
            }
        }

        public void OnSettingsChanged()
        {
            // Adjust to new calibration zone size.
            for(int i = 0; i < shifts_.Count; )
            {
                bool did_remove = false;
                foreach(var shift in shifts_)
                {
                    if (Helpers.GetDistance(shift.Item1, shifts_[i].Item1) < Options.Instance.calibration_zone_size)
                    {
                        did_remove = true;
                        shifts_.RemoveAt(i);
                        break;
                    }
                }

                if (!did_remove)
                    i++;
            }

            // Adjust to new calibration zones count.
            while (shifts_.Count > Options.Instance.calibration_max_zones_count)
                shifts_.RemoveAt(0);
        }

        public void AddShift(Point cursor_position, Point shift)
        {
            var indices = GetClosestShiftIndexes(cursor_position);
            if (shifts_.Count() < Options.Instance.calibration_max_zones_count)
            {
                if (indices == null || indices[0].Item2 >  Options.Instance.calibration_zone_size)
                    shifts_.Add(new Tuple<Point, Point>(cursor_position, shift));
                else
                    shifts_[indices[0].Item1] = new Tuple<Point, Point>(cursor_position, shift);
            }
            else
            {
                shifts_[indices[0].Item1] = new Tuple<Point, Point>(cursor_position, shift);
            }

            if ((DateTime.Now - last_save_time).TotalSeconds > 10 && (save_to_file_task == null || save_to_file_task.IsCompleted))
            {
                last_save_time = DateTime.Now;
                var deep_copy = new List<Tuple<Point, Point>>();
                foreach (var i in shifts_)
                {
                    deep_copy.Add(new Tuple<Point, Point>(i.Item1, i.Item2));
                }

                save_to_file_task = Task.Factory.StartNew(() => {
                    File.WriteAllText(Filepath, JsonConvert.SerializeObject(deep_copy, Formatting.Indented));
                });
            }
        }
        private Tuple<int /*index*/, double /*distance*/>[] GetClosestShiftIndexes(Point cursor_position)
        {
            if (shifts_.Count() == 0)
                return null;

            Tuple<int, double>[] retval = new Tuple<int, double>[3] {
                new Tuple<int, double>(-1, double.MaxValue),
                new Tuple<int, double>(-1, double.MaxValue),
                new Tuple<int, double>(-1, double.MaxValue),
            };

            for (int i = 0; i < shifts_.Count(); i++)
            {
                double distance = Helpers.GetDistance(shifts_[i].Item1, cursor_position);
                if (distance < 1)
                    distance = 1;

                if (distance < retval[0].Item2)
                {
                    retval[2] = retval[1];
                    retval[1] = retval[0];
                    retval[0] = new Tuple<int, double>(i, distance);
                    if (retval[1].Item1 == -1)
                        retval[1] = retval[0];
                    if (retval[2].Item1 == -1)
                        retval[2] = retval[0];
                }
                else if (distance < retval[1].Item2)
                {
                    retval[2] = retval[1];
                    retval[1] = new Tuple<int, double>(i, distance);
                }
                else if (distance < retval[1].Item2)
                {
                    retval[2] = new Tuple<int, double>(i, distance);
                }
            }
            return retval;
        }
    }
}
