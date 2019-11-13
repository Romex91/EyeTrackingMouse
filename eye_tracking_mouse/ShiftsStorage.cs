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
        public class ShiftItem
        {
            public ShiftItem(Point position, Point shift)
            {
                Position = position;
                Shift = shift;
                SectorX = (int)(position.X / 500.0);
                SectorY = (int)(position.Y / 500.0);
            }

            public Point Position { get; private set; }
            public Point Shift { get; private set; }

            // To calculate points density we split the screen to sectors. This algprithm is not accurate but simple and fast.
            public int SectorX { get; private set; }

            public int SectorY { get; private set; }
        }

        private static string Filepath { get { return Path.Combine(Helpers.GetLocalFolder(), "calibration.json"); } }
        public List<ShiftItem> Shifts { private set; get; } = new List<ShiftItem>();

        private DateTime last_save_time = DateTime.Now;

        public Task save_to_file_task;

        public event EventHandler Changed;
        public static ShiftsStorage Instance { get; set; } = LoadFromFile();

        public static ShiftsStorage LoadFromFile()
        {
            var storage = new ShiftsStorage();
            if (File.Exists(Filepath))
            {
                try
                {
                    storage.Shifts = JsonConvert.DeserializeObject<List<ShiftItem>>(File.ReadAllText(Filepath));
                }
                catch (Exception) { }
            }
            return storage;
        }

        public Point GetShift(Point cursor_position)
        {
            var closest_indices = GetClosestShiftIndexes(cursor_position, Options.Instance.calibration_considered_zones_count);
            if (closest_indices == null)
            {
                Debug.Assert(Shifts.Count() == 0);
                return new Point(0, 0);
            }

            double sum_of_reverse_distances = 0;
            foreach (var index in closest_indices)
            {
                sum_of_reverse_distances += (1 / index.Item2);
            }

            Point resulting_shift = new Point(0, 0);
            foreach (var index in closest_indices)
            {
                resulting_shift.X += (int)(Shifts[index.Item1].Shift.X / index.Item2 / sum_of_reverse_distances);
                resulting_shift.Y += (int)(Shifts[index.Item1].Shift.Y / index.Item2 / sum_of_reverse_distances);
            }

            return resulting_shift;
        }

        public void Reset()
        {
            Shifts.Clear();
            NotifyOnChange();
        }
        public void OnSettingsChanged()
        {
            // Adjust to new calibration zone size.
            for (int i = 0; i < Shifts.Count - 1;)
            {
                bool did_remove = false;
                for (int j = i + 1; j < Shifts.Count; j++)
                {
                    if (Helpers.GetDistance(Shifts[j].Position, Shifts[i].Position) < Options.Instance.calibration_zone_size)
                    {
                        did_remove = true;
                        Shifts.RemoveAt(i);
                        break;
                    }
                }

                if (!did_remove)
                    i++;
            }

            // Adjust to new calibration zones count.
            while (Shifts.Count > Options.Instance.calibration_max_zones_count)
                Shifts.RemoveAt(0);

            SaveToFileAsync();
            NotifyOnChange();
        }

        public void SaveToFileAsync()
        {
            if (save_to_file_task != null && !save_to_file_task.IsCompleted)
                save_to_file_task.Wait();

            last_save_time = DateTime.Now;
            var deep_copy = new List<ShiftItem>();
            foreach (var i in Shifts)
            {
                deep_copy.Add(new ShiftItem(i.Position, i.Shift));
            }

            save_to_file_task = Task.Factory.StartNew(() =>
            {
                File.WriteAllText(Filepath, JsonConvert.SerializeObject(deep_copy, Formatting.Indented));
            });
        }

        public void AddShift(Point cursor_position, Point shift)
        {
            var indices = GetClosestShiftIndexes(cursor_position, 2);
            if (indices != null && indices[0].Item2 < Options.Instance.calibration_zone_size)
            {
                Shifts[indices[0].Item1] = new ShiftItem(cursor_position, shift);
                if (indices.Count > 1 && indices[1].Item2 < Options.Instance.calibration_zone_size)
                    Shifts.RemoveAt(indices[1].Item1);
            }
            else if (Shifts.Count() < Options.Instance.calibration_max_zones_count)
            {
                Shifts.Add(new ShiftItem(cursor_position, shift));
            }
            else
            {
                Shifts[GetClosestPointOfHihestDensity(cursor_position)] = new ShiftItem(cursor_position, shift);
            }

            if ((DateTime.Now - last_save_time).TotalSeconds > 10 && (save_to_file_task == null || save_to_file_task.IsCompleted))
            {
                SaveToFileAsync();
            }

            NotifyOnChange();
        }

        private void NotifyOnChange()
        {
            Changed?.Invoke(this, new EventArgs());
        }

        private int GetSectorNumber(ShiftItem shift, int max_sector_x)
        {
            return shift.SectorX + shift.SectorY * (max_sector_x + 1);
        }

        private int GetClosestPointOfHihestDensity(Point cursor_position)
        {
            Debug.Assert(Shifts.Count > 0);

            int max_sector_x = 0;
            for (int i = 0; i < Shifts.Count; i++)
                if (Shifts[i].SectorX > max_sector_x)
                    max_sector_x = Shifts[i].SectorX;

            var sectors = new Dictionary<int /*number of sector*/, int /*Count of points in sector*/>();
            for (int i = 0; i < Shifts.Count; i++)
            {
                int sector_number = GetSectorNumber(Shifts[i], max_sector_x);
                if (!sectors.ContainsKey(sector_number))
                    sectors.Add(sector_number, 0);

                sectors[sector_number]++;
            }

            int max_points_count_in_sector = 0;
            for (int i = 0; i < sectors.Count; i++)
            {
                int points_number_in_sector = sectors.ElementAt(i).Value;
                if (points_number_in_sector > max_points_count_in_sector)
                    max_points_count_in_sector = points_number_in_sector;
            }

            int index_of_closest_point = 0;
            double min_distance = double.MaxValue;
            for (int i = 0; i < Shifts.Count; i++)
            {
                if (sectors[GetSectorNumber(Shifts[i], max_sector_x)] == max_points_count_in_sector)
                {
                    double distance = Helpers.GetDistance(Shifts[i].Position, cursor_position);
                    if (min_distance > distance)
                    {
                        min_distance = distance;
                        index_of_closest_point = i;
                    }
                }
            }

            return index_of_closest_point;
        }

        private List<Tuple<int /*index*/, double /*distance*/>> GetClosestShiftIndexes(Point cursor_position, int number)
        {
            if (Shifts.Count() == 0)
                return null;

            var retval = new List<Tuple<int, double>>();
            for (int i = 0; i < Shifts.Count(); i++)
            {
                double distance = Helpers.GetDistance(Shifts[i].Position, cursor_position);
                if (distance < 0.1)
                    distance = 0.1;

                if (retval.Count == 0)
                {
                    retval.Add(new Tuple<int, double>(i, distance));
                    continue;
                }

                int j = 0;
                for (; j < retval.Count; j++)
                {
                    if (distance < retval[j].Item2)
                    {
                        retval.Insert(j, new Tuple<int, double>(i, distance));
                        break;
                    }
                }
                if (j == retval.Count)
                    retval.Add(new Tuple<int, double>(i, distance));

                if (retval.Count > number)
                    retval.RemoveAt(retval.Count - 1);
            }
            return retval;
        }
    }
}
