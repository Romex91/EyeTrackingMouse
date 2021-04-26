using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BlindConfigurationTester
{
    public static partial class Extensions
    {
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class CalibrationModeIterator
    {
        public eye_tracking_mouse.Options.CalibrationMode CalibrationMode
        {
            set;
            get;
        }

        public List<OptionsField> Fields
        {
            private set;
            get;
        } = new List<OptionsField>();

        public long GetUniqueKey(eye_tracking_mouse.Options.CalibrationMode mode)
        {
            long retval = 0;
            long current_step = 1;
            foreach(var field in Fields)
            {
                int field_value = field.GetFieldValue(mode);
                int i = 0;
                for (; i < field.Range.Length; i++)
                {
                    if (field.Range[i] == field_value)
                        break;
                }
                if (i == field.Range.Length)
                {
                    MessageBox.Show("tried getting unique key for invalid mode");
                    Application.Current.Shutdown();
                }

                retval += current_step * i;
                current_step *= field.Range.Length;
            }

            Debug.Assert(retval < NumberOfDifferentModes);
            return retval;
        }

        public long NumberOfDifferentModes
        {
            get
            {
                long retval = 1;
                foreach(var field in Fields)
                {
                    retval *= field.Range.Length;
                }
                return retval;
            }
        }

        public CalibrationModeIterator(eye_tracking_mouse.Options.CalibrationMode mode)
        {
            CalibrationMode = mode;

            var fields = new OptionsField[]
            {
                OptionsField.BuildHardcoded(field_name : "zone_size", new List<int>{ 10, 15, 25, 50, 75, 100, 150, 200, 250, 350, 500, 800 }),
                OptionsField.BuildExponential(field_name : "max_zones_count", 8, 2048, 2, false),
                OptionsField.BuildHardcoded(field_name : "considered_zones_count", new List<int>{ 3, 4, 5, 6, 8, 10, 14, 20, 30 }),
                OptionsField.BuildHardcoded(field_name : "considered_zones_count_v1", new List<int>{ 3, 4, 5, 6, 8, 10, 14, 20, 30 }),
                OptionsField.BuildHardcoded(field_name : "shade_thickness_in_pixels", new List<int>{ 5, 10, 25, 50}),
                OptionsField.BuildLinear(field_name : "size_of_opaque_sector_in_percents", max : 70, min : 30, step: 10),
                OptionsField.BuildHardcoded(field_name : "correction_fade_out_distance", new List<int>{ 50, 75, 100, 150, 200, 250, 350, 500, 800 }),
                OptionsField.BuildExponential(field_name : "coordinate 2", 50, 30000, 1.6f, true),
                OptionsField.BuildExponential(field_name : "coordinate 3", 50, 30000, 1.6f, true),
                OptionsField.BuildExponential(field_name : "coordinate 4", 50, 30000, 1.6f, true),
                OptionsField.BuildExponential(field_name : "coordinate 5", 50, 30000, 1.6f, true),
                OptionsField.BuildExponential(field_name : "coordinate 6", 50, 30000, 1.6f, true),
                OptionsField.BuildExponential(field_name : "coordinate 7", 50, 30000, 1.6f, true),
                OptionsField.BuildExponential(field_name : "coordinate 8", 50, 30000, 1.6f, true),
                OptionsField.BuildExponential(field_name : "coordinate 9", 50, 30000, 1.6f, true),
                //OptionsField.BuildHardcoded(field_name : "coordinate 2", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                //OptionsField.BuildHardcoded(field_name : "coordinate 3", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                //OptionsField.BuildHardcoded(field_name : "coordinate 4", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                //OptionsField.BuildHardcoded(field_name : "coordinate 5", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                //OptionsField.BuildHardcoded(field_name : "coordinate 6", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                //OptionsField.BuildHardcoded(field_name : "coordinate 7", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                //OptionsField.BuildHardcoded(field_name : "coordinate 8", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                //OptionsField.BuildHardcoded(field_name : "coordinate 9", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            };

            foreach (var field in fields)
            {
                if (field.GetFieldValue(CalibrationMode) != -1)
                {
                    Fields.Add(field);
                }
            }
        }

        public class OptionsField
        {
            public string field_name;
            public int[] Range
            {
                private set;
                get;
            }

            public int Min
            {
                get
                {
                    return Range.First();
                }
            }

            public static OptionsField BuildLinear(string field_name, int min, int max, int step)
            {
                List<int> range = new List<int>();
                int i = min;
                while (true)
                {
                    if (i <= max)
                        range.Add(i);
                    else
                        break;
                    i = (int)(i + step);
                    
                }

                return new OptionsField
                {
                    field_name = field_name,
                    Range = range.ToArray()
                };
            }
            public static OptionsField BuildExponential(string field_name, int min, int max, float step, bool include_zero)
            {
                List<int> range = new List<int>();
                if (include_zero)
                    range.Add(0);
                int i = min;
                while (true)
                {
                    if (i <= max)
                        range.Add(i);
                    else
                        break;

                    i = (int)(i * step);
                }

                return new OptionsField
                {
                    field_name = field_name,
                    Range = range.ToArray()
                };
            }
            public static OptionsField BuildHardcoded(string field_name, List<int> range)
            {
                return new OptionsField
                {
                    field_name = field_name,
                    Range = range.ToArray()
                };
            }

            public int GetFieldValue(
                eye_tracking_mouse.Options.CalibrationMode calibration_mode)
            {
                if (field_name.StartsWith("coordinate"))
                {
                    int coordinate_index = int.Parse(field_name.Split(' ')[1]);
                    if (coordinate_index >= calibration_mode.additional_dimensions_configuration.CoordinatesCount)
                    {
                        return -1;
                    }
                    return calibration_mode.additional_dimensions_configuration.CoordinatesScalesInPercents[coordinate_index];
                }
                else
                {
                    var field = calibration_mode.GetType().GetField(field_name);
                    return (int)field.GetValue(calibration_mode);
                }
            }

            public void SetFieldValue(
                eye_tracking_mouse.Options.CalibrationMode calibration_mode, int value)
            {
                if (field_name.StartsWith("coordinate"))
                {
                    int coordinate_index = int.Parse(field_name.Split(' ')[1]);
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
                    var field = calibration_mode.GetType().GetField(field_name);
                    field.SetValue(calibration_mode, value);
                }
            }

            public bool Increment(
                eye_tracking_mouse.Options.CalibrationMode calibration_mode,
                int steps_number)
            {
                int value = GetFieldValue(calibration_mode);
                if (value == -1)
                {
                    return false;
                }

                int i = 0;
                for (; i < Range.Length; i++)
                {
                    if (Range[i] >= value)
                    {
                        break;
                    }
                }

                i += steps_number;
                if (i < 0 || i >= Range.Length)
                    return false;

                SetFieldValue(calibration_mode, Range[i]);
                return true;
            }
        }

        public void ForModeAndItsVariations(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            Action<eye_tracking_mouse.Options.CalibrationMode, string> callback)
        {
            callback(starting_mode, "mid");

            for (int i = 0; i < 2; i++)
            {
                eye_tracking_mouse.Options.CalibrationMode mode = starting_mode.Clone();
                for (int field_number = 0; field_number < Fields.Count; field_number++)
                {
                    if (i == 0)
                    {
                        Fields[field_number].SetFieldValue(mode, Fields[field_number].Range.Last());
                    }
                    else
                    {
                        var range = Fields[field_number].Range;
                        Fields[field_number].SetFieldValue(mode, (range[0] == 0 && range.Length > 1) ? range[1] : range[0]);
                    }
                }

                callback(mode, i == 0 ? "max" : "min");
            }
        }

        public void ForEachMinMaxPermutation(
            eye_tracking_mouse.Options.CalibrationMode starting_mode,
            Action<eye_tracking_mouse.Options.CalibrationMode> callback)
        {
            callback(starting_mode);

            long iterator = 1;

            foreach (var field in Fields)
            {
                // two additional permutations
                iterator *= 2;
            }

            List<long> permutations = new List<long>();

            while (iterator > 0)
            {
                permutations.Add(iterator--);
            }
            permutations.Shuffle(new Random((int)(DateTime.Now.ToBinary() % int.MaxValue)));

            foreach (var permutation in permutations)
            {
                eye_tracking_mouse.Options.CalibrationMode mode = starting_mode.Clone();

                for (int field_number = 0; field_number < Fields.Count; field_number++)
                {
                    if ((permutation & (1 << field_number)) == 0)
                    {
                        Fields[field_number].SetFieldValue(mode, Fields[field_number].Range.Last());
                    }
                    else
                    {
                        Fields[field_number].SetFieldValue(mode, Fields[field_number].Range.First());
                    }
                }

                callback(mode);
            }
        }
    }

    public class IteratorTest
    {
        private static void Assert(bool x)
        {
            // VS unit tests don't work and I don't want to fix them.
            if (!x)
                MessageBox.Show("Tests fail");
        }

        static void ForEachFieldValue(
            int field_index,
            List<CalibrationModeIterator.OptionsField> fields,
            eye_tracking_mouse.Options.CalibrationMode mode,
            Action<eye_tracking_mouse.Options.CalibrationMode> action)
        {
            var field = fields[field_index];
            field.SetFieldValue(mode, field.Min);
            while (field.Increment(mode, 1))
            {
                if (field_index < fields.Count - 1)
                    ForEachFieldValue(field_index + 1, fields, mode, action);
                else
                    action(mode);
            }
        }

        public static void UniqueKeysAreUnique(eye_tracking_mouse.Options.CalibrationMode mode)
        {
            var unique_keys = new HashSet<long>();
            var iterator = new CalibrationModeIterator(mode);

            foreach(var field in iterator.Fields)
            {
                field.SetFieldValue(mode, field.Min);
            }

            for (int i = 0; i < 10; i++)
            {
                foreach (var field in iterator.Fields)
                {
                    if (field.Increment(mode, 1))
                    {
                        long key = iterator.GetUniqueKey(mode);
                        Assert(!unique_keys.Contains(key));
                        unique_keys.Add(key);
                    }
                }
            }

            unique_keys.Clear();

            foreach(var field in iterator.Fields)
            {
                field.SetFieldValue(mode, field.Min);
            }
            long tests_to_run = 1000000;
            try
            {
                ForEachFieldValue(0, iterator.Fields, mode, (x) => {
                    if (--tests_to_run < 0)
                    {
                        throw new NotImplementedException("Удалые пляски на костылях.");
                    }
                    else {
                        long key = iterator.GetUniqueKey(x);
                        Assert(!unique_keys.Contains(key));
                        unique_keys.Add(key);
                    }
            });
            } catch (NotImplementedException)
            {
                Assert(tests_to_run < 0);
            }


            foreach (var field in iterator.Fields)
            {
                field.SetFieldValue(mode, field.Min);
            }
            tests_to_run = 1000001;
            try
            {
                ForEachFieldValue(0, iterator.Fields, mode, (x) => {
                    if (--tests_to_run < 0)
                    {
                        throw new NotImplementedException("Удалые пляски на костылях.");
                    } else if (tests_to_run == 0){
                        long key = iterator.GetUniqueKey(x);
                        Assert(!unique_keys.Contains(key));
                    }
                    else 
                    {
                        long key = iterator.GetUniqueKey(x);
                        Assert(unique_keys.Contains(key));
                    }
                });
            }
            catch (NotImplementedException)
            {
                Assert(tests_to_run < 0);
            }
        }
    }

}
