using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlindConfigurationTester
{
    class CalibrationModeIterator
    {
        public eye_tracking_mouse.Options.CalibrationMode CalibrationMode {
            private set;
            get;
        }

        public List<OptionsField> Fields
        {
            private set;
            get;
        } = new List<OptionsField>();

        public CalibrationModeIterator(eye_tracking_mouse.Options.CalibrationMode mode)
        {
            CalibrationMode = mode;

            var fields = new OptionsField[]
            {
                OptionsField.BuildHardcoded(field_name : "zone_size", new List<int>{ 10, 15, 25, 50, 75, 100, 150, 200, 250, 350, 500, 800 }),
                OptionsField.BuildHardcoded(field_name : "considered_zones_count", new List<int>{ 3,  6, 10, 20 }),
                OptionsField.BuildLinear(field_name : "size_of_opaque_sector_in_percents", max : 70, min : 30, step: 10),
                OptionsField.BuildLinear(field_name : "size_of_transparent_sector_in_percents", max : 60, min : 0, step: 10),
                OptionsField.BuildHardcoded(field_name : "correction_fade_out_distance", new List<int>{ 50, 75, 100, 150, 200, 250, 350, 500, 800 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 2", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 3", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 4", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 5", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 6", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 7", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 8", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
                OptionsField.BuildHardcoded(field_name : "coordinate 9", new List<int> {50, 100, 250, 400, 600, 800, 1000, 1300, 1700, 2500, 5000, 7500, 10000, 12000 }),
            };

            foreach(var field in fields)
            {
                if (field.GetFieldValue(CalibrationMode) != -1)
                {
                    Fields.Add(field);
                }
            }
        }

        public interface IntRange
        {
            List<int> GetRange();
        }

        public struct OptionsField
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

            public eye_tracking_mouse.Options.CalibrationMode Increment(
                eye_tracking_mouse.Options.CalibrationMode calibration_mode,
                int steps_number)
            {
                var new_calibration_mode = calibration_mode.Clone();

                int value = GetFieldValue(new_calibration_mode);
                if (value == -1)
                {
                    return null;
                }

                var range = this.range.GetRange();
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

                SetFieldValue(new_calibration_mode, range[i]);
                return new_calibration_mode;
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
                        Fields[field_number].SetFieldValue(mode, Fields[field_number].range.GetRange().Last());
                    }
                    else
                    {
                        Fields[field_number].SetFieldValue(mode, Fields[field_number].range.GetRange().First());
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
                        Fields[field_number].SetFieldValue(mode, Fields[field_number].range.GetRange().Last());
                    }
                    else
                    {
                        Fields[field_number].SetFieldValue(mode, Fields[field_number].range.GetRange().First());
                    }
                }

                callback(mode);
            }
        }
    }
}
