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
        public class Position
        {
            public Position(
                double x,
                double y,
                Tobii.Interaction.Vector3 left_eye,
                Tobii.Interaction.Vector3 right_eye,
                Tobii.Interaction.Vector3 head_position,
                Tobii.Interaction.Vector3 head_direction)
            {
                X = x;
                Y = y;

                MultidimensionCalibrationType type = Options.Instance.calibration.multidimension_calibration_type;
                if ((type & MultidimensionCalibrationType.HeadPosition) != MultidimensionCalibrationType.None)
                {
                    HeadPosition = head_position;
                }

                if ((type & MultidimensionCalibrationType.HeadDirection) != MultidimensionCalibrationType.None)
                {
                    HeadDirection = head_direction;
                }

                if ((type & MultidimensionCalibrationType.LeftEye) != MultidimensionCalibrationType.None)
                {
                    LeftEye = left_eye;
                }

                if ((type & MultidimensionCalibrationType.RightEye) != MultidimensionCalibrationType.None)
                {
                    RightEye = right_eye;
                }

                AdjustColorBoundaries();
            }

            public void AdjustColorBoundaries()
            {
                var color = GetColorComponents();
                if (max_color.X < color.X)
                    max_color.X = color.X;

                if (max_color.Y < color.Y)
                    max_color.Y = color.Y;

                if (max_color.Z < color.Z)
                    max_color.Z = color.Z;

                if (min_color.X > color.X)
                    min_color.X = color.X;

                if (min_color.Y > color.Y)
                    min_color.Y = color.Y;

                if (min_color.Z > color.Z)
                    min_color.Z = color.Z;
            }

            public System.Windows.Media.Color GetColor()
            {
                var color_components = GetColorComponents();

                return System.Windows.Media.Color.FromArgb(
                    255, (byte)((color_components.X - min_color.X) / (max_color.X - min_color.X) * 254),
                    (byte)((color_components.Y - min_color.Y) / (max_color.Y - min_color.Y) * 254),
                    (byte)((color_components.Z - min_color.Z) / (max_color.Z - min_color.Z) * 254));
            }

            private static Tobii.Interaction.Vector3 min_color = new Tobii.Interaction.Vector3(Double.MaxValue, Double.MaxValue, Double.MaxValue);
            private static Tobii.Interaction.Vector3 max_color = new Tobii.Interaction.Vector3(Double.MinValue, Double.MinValue, Double.MinValue);

            private Tobii.Interaction.Vector3 GetColorComponents()
            {
                return new Tobii.Interaction.Vector3(
                    LeftEye.X + RightEye.X + HeadDirection.X + HeadPosition.X,
                    LeftEye.Y + RightEye.Y + HeadDirection.Y + HeadPosition.Y,
                    LeftEye.Z + RightEye.Z + HeadDirection.Z + HeadPosition.Z);
            }

            public double X = 0;
            public double Y = 0;

            public Tobii.Interaction.Vector3 LeftEye { get; set; }
            public Tobii.Interaction.Vector3 RightEye { get; set; }
            public Tobii.Interaction.Vector3 HeadPosition { get; set; }
            public Tobii.Interaction.Vector3 HeadDirection { get; set; }

            // To calculate points density we split the screen to sectors. This algprithm is not accurate but simple and fast
            [JsonIgnore]
            public int SectorX
            {
                get
                {
                    return (int)(X / 500.0);
                }
            }

            [JsonIgnore]
            public int SectorY
            {
                get
                {
                    return (int)(Y / 500.0);
                }
            }

            private static double SquaredDistance(Tobii.Interaction.Vector3 a, Tobii.Interaction.Vector3 b)
            {
                return (Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2)) * Math.Pow(Options.Instance.calibration.multi_dimensions_detalization, 2);
            }

            public double GetDistance(Position other)
            {
                double squared_distance = Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2);

                squared_distance += SquaredDistance(HeadPosition, other.HeadPosition);
                squared_distance += SquaredDistance(HeadDirection, other.HeadDirection);
                squared_distance += SquaredDistance(LeftEye, other.LeftEye);
                squared_distance += SquaredDistance(RightEye, other.RightEye);

                return Math.Pow(squared_distance, 0.5);
            }
        }

        public class ShiftItem
        {
            public ShiftItem(Position position, Point shift)
            {
                Position = position;
                Shift = shift;
            }

            public Point Shift { get; private set; }

            public Position Position { get; private set; }
        }

        private static string Filepath { get { return Path.Combine(Helpers.GetLocalFolder(), "calibration" + Options.Instance.calibration.multidimension_calibration_type + ".json"); } }

        public List<ShiftItem> Shifts { private set; get; } = new List<ShiftItem>();

        public Position LastPosition { private set; get; }

        public static event EventHandler Changed;

        public static event EventHandler CursorPositionUpdated;
        public static ShiftsStorage Instance { get; set; } = LoadFromFile();

        public static ShiftsStorage LoadFromFile()
        {
            var storage = new ShiftsStorage();
            if (File.Exists(Filepath))
            {
                try
                {
                    storage.Shifts = JsonConvert.DeserializeObject<List<ShiftItem>>(File.ReadAllText(Filepath));
                    foreach (var shift in storage.Shifts)
                        shift.Position.AdjustColorBoundaries();
                }
                catch (Exception) { }
            }
            return storage;
        }

        public ShiftsStorage()
        {
            Settings.OptionsChanged += OnSettingsChanged;
        }

        public Point GetShift(Position cursor_position)
        {
            LastPosition = cursor_position;
            CursorPositionUpdated?.Invoke(this, new EventArgs());
            var closest_indices = GetClosestShiftIndexes(cursor_position, Options.Instance.calibration.considered_zones_count);
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
        private static void OnSettingsChanged(object sender, EventArgs e)
        {
            AsyncSaver.FlushSynchroniously();

            Instance = LoadFromFile();

            // Adjust to new calibration zone size.
            for (int i = 0; i < Instance.Shifts.Count - 1;)
            {
                bool did_remove = false;
                for (int j = i + 1; j < Instance.Shifts.Count; j++)
                {
                    if (Instance.Shifts[j].Position.GetDistance(Instance.Shifts[i].Position) < Options.Instance.calibration.zone_size)
                    {
                        did_remove = true;
                        Instance.Shifts.RemoveAt(i);
                        break;
                    }
                }

                if (!did_remove)
                    i++;
            }

            // Adjust to new calibration zones count.
            while (Instance.Shifts.Count > Options.Instance.calibration.max_zones_count)
                Instance.Shifts.RemoveAt(0);

            AsyncSaver.Save(Filepath, Instance.GetDeepCopy);
            Instance.NotifyOnChange();
        }

        private object GetDeepCopy()
        {
            var deep_copy = new List<ShiftItem>();
            foreach (var i in Shifts)
            {
                deep_copy.Add(new ShiftItem(i.Position, i.Shift));
            }

            return deep_copy;
        }

        public void AddShift(Position cursor_position, Point shift)
        {
            var indices = GetClosestShiftIndexes(cursor_position, 2);
            if (indices != null && indices[0].Item2 < Options.Instance.calibration.zone_size)
            {
                Shifts[indices[0].Item1] = new ShiftItem(cursor_position, shift);
                if (indices.Count > 1 && indices[1].Item2 < Options.Instance.calibration.zone_size)
                    Shifts.RemoveAt(indices[1].Item1);
            }
            else if (Shifts.Count() < Options.Instance.calibration.max_zones_count)
            {
                Shifts.Add(new ShiftItem(cursor_position, shift));
            }
            else
            {
                Shifts[GetClosestPointOfHihestDensity(cursor_position)] = new ShiftItem(cursor_position, shift);
            }

            AsyncSaver.Save(Filepath, GetDeepCopy);
            NotifyOnChange();
        }

        private void NotifyOnChange()
        {
            Changed?.Invoke(this, new EventArgs());
        }

        private int GetSectorNumber(ShiftItem shift, int max_sector_x)
        {
            return shift.Position.SectorX + shift.Position.SectorY * (max_sector_x + 1);
        }

        private int GetClosestPointOfHihestDensity(Position cursor_position)
        {
            Debug.Assert(Shifts.Count > 0);

            int max_sector_x = 0;
            for (int i = 0; i < Shifts.Count; i++)
                if (Shifts[i].Position.SectorX > max_sector_x)
                    max_sector_x = Shifts[i].Position.SectorX;

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
                    double distance = Shifts[i].Position.GetDistance(cursor_position);
                    if (min_distance > distance)
                    {
                        min_distance = distance;
                        index_of_closest_point = i;
                    }
                }
            }

            return index_of_closest_point;
        }

        private List<Tuple<int /*index*/, double /*distance*/>> GetClosestShiftIndexes(Position cursor_position, int number)
        {
            if (Shifts.Count() == 0)
                return null;

            var retval = new List<Tuple<int, double>>();
            for (int i = 0; i < Shifts.Count(); i++)
            {
                double distance = Shifts[i].Position.GetDistance(cursor_position);
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
