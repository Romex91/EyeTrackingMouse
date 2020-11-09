using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    public enum Key
    {
        Unbound,
        Modifier,
        LeftMouseButton,
        RightMouseButton,
        ScrollDown,
        ScrollUp,
        ScrollLeft,
        ScrollRight,
        ShowCalibrationView,
        CalibrateLeft,
        CalibrateRight,
        CalibrateUp,
        CalibrateDown,
    }

    // Determines how spatial each new dimension will be.
    // null means dimension is disabled.
    public class Vector3Percents
    {
        public int? X { get; set; }
        public int? Y { get; set; }
        public int? Z { get; set; }

        public Vector3Percents Clone()
        {
            return new Vector3Percents { X = this.X, Y = this.Y, Z = this.Z };
        }

        public bool Equals(Vector3Percents other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public static Vector3Percents Disabled
        {
            get
            {
                return new Vector3Percents { X = null, Y = null, Z = null };
            }
        }
    }

    public struct AdditionalDimensionsConfguration
    {
        public Vector3Percents LeftEye;
        public Vector3Percents RightEye;
        public Vector3Percents AngleBetweenEyes; // Z is always null
        public Vector3Percents HeadPosition;
        public Vector3Percents HeadDirection;

        private List<Vector3Percents> Vectors
        {
            get { return new List<Vector3Percents> { LeftEye, RightEye, AngleBetweenEyes, HeadDirection, HeadPosition }; }
        }

        [JsonIgnore]
        public int[] CoordinatesScalesInPercents
        {
            get
            {
                int[] results = new int[CoordinatesCount];
                results[0] = 100;
                results[1] = 100;
                int coordinates_count = 2;
                foreach (var vector3 in Vectors)
                {
                    if (vector3.X.HasValue)
                        results[coordinates_count++] = vector3.X.Value;
                    if (vector3.Y.HasValue)
                        results[coordinates_count++] = vector3.Y.Value;
                    if (vector3.Z.HasValue)
                        results[coordinates_count++] = vector3.Z.Value;
                }

                return results;
            }

            set
            {
                Debug.Assert(value[0] == 100);
                Debug.Assert(value[1] == 100);
                int index = 2;
                foreach (var vector3 in Vectors)
                {
                    if (vector3.X.HasValue)
                    {
                        vector3.X = value[index++];
                    }
                    if (vector3.Y.HasValue)
                    {
                        vector3.Y = value[index++];
                    }
                    if (vector3.Z.HasValue)
                    {
                        vector3.Z = value[index++];
                    }
                }
            }
        }

        public AdditionalDimensionsConfguration Clone()
        {
            return new AdditionalDimensionsConfguration
            {
                AngleBetweenEyes = this.AngleBetweenEyes.Clone(),
                HeadDirection = this.HeadDirection.Clone(),
                HeadPosition = this.HeadPosition.Clone(),
                LeftEye = this.LeftEye.Clone(),
                RightEye = this.RightEye.Clone()
            };
        }

        [JsonIgnore]
        public int CoordinatesCount
        {
            get
            {
                // X and Y are always there.
                int coordinates_count = 2;
                foreach (var vector3 in Vectors)
                {
                    if (vector3.X.HasValue)
                        coordinates_count++;
                    if (vector3.Y.HasValue)
                        coordinates_count++;
                    if (vector3.Z.HasValue)
                        coordinates_count++;
                }
                return coordinates_count;
            }
        }
        public bool Equals(AdditionalDimensionsConfguration other)
        {
            return
                LeftEye.Equals(other.LeftEye) &&
                RightEye.Equals(other.RightEye) &&
                AngleBetweenEyes.Equals(other.AngleBetweenEyes) &&
                HeadDirection.Equals(other.HeadDirection) &&
                HeadPosition.Equals(other.HeadPosition);
        }

        public static AdditionalDimensionsConfguration Disabled
        {
            get
            {
                return new AdditionalDimensionsConfguration
                {
                    LeftEye = Vector3Percents.Disabled,
                    RightEye = Vector3Percents.Disabled,
                    AngleBetweenEyes = Vector3Percents.Disabled,
                    HeadPosition = Vector3Percents.Disabled,
                    HeadDirection = Vector3Percents.Disabled
                };
            }
        }
    }

    public class KeyBindings
    {
        public static EventHandler Changed;

        // Some modifiers have different Up and Down states depending on whether it is right or left.
        // Humanity has messy keyboard scancodes.
        public bool is_modifier_e0 = true;

        public enum InterceptionMethod
        {
            WinApi,
            OblitaDriver
        }
        public InterceptionMethod interception_method = InterceptionMethod.WinApi;
        public bool is_driver_installed = false;

        public Interceptor.Keys this[Key key]
        {
            get => interception_method == InterceptionMethod.OblitaDriver ? bindings[key] : default_bindings[key];
            set => bindings[key] = value;
        }

        public static Dictionary<Key, Interceptor.Keys> default_bindings = new Dictionary<Key, Interceptor.Keys>
        {
            {Key.Modifier, Interceptor.Keys.WindowsKey},
            {Key.LeftMouseButton, Interceptor.Keys.J },
            {Key.RightMouseButton, Interceptor.Keys.K},
            {Key.ScrollDown,Interceptor.Keys.N},
            {Key.ScrollUp, Interceptor.Keys.H},
            {Key.ScrollLeft, Interceptor.Keys.CommaLeftArrow},
            {Key.ScrollRight, Interceptor.Keys.PeriodRightArrow},
            {Key.ShowCalibrationView, Interceptor.Keys.M},
            {Key.CalibrateLeft, Interceptor.Keys.A},
            {Key.CalibrateRight, Interceptor.Keys.D},
            {Key.CalibrateUp, Interceptor.Keys.W},
            {Key.CalibrateDown, Interceptor.Keys.S},
        };

        [JsonProperty(ItemConverterType = typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public Dictionary<Key, Interceptor.Keys> bindings = new Dictionary<Key, Interceptor.Keys>(default_bindings);
    };


    // TODO: check all accesses to this and other shared classes are in critical section.
    public class Options
    {
        public static EventHandler Changed;
        public KeyBindings key_bindings = new KeyBindings();
        public class CalibrationMode
        {
            public static EventHandler Changed;
            public int zone_size;
            public int max_zones_count;

            public int considered_zones_count;

            public int size_of_opaque_sector_in_percents = 30;
            public int shade_thickness_in_pixels = 50;

            // V2
            public int considered_zones_count_v1 = 6;
            public int correction_fade_out_distance = 100;

            public string tag_for_testing;

            // TODO: remove the option. There can be only one!
            public string algorithm = "V2";

            public AdditionalDimensionsConfguration additional_dimensions_configuration;

            public CalibrationMode Clone()
            {
                return new CalibrationMode
                {
                    zone_size = this.zone_size,
                    max_zones_count = this.max_zones_count,
                    considered_zones_count = this.considered_zones_count,
                    size_of_opaque_sector_in_percents = this.size_of_opaque_sector_in_percents,
                    shade_thickness_in_pixels = this.shade_thickness_in_pixels,
                    correction_fade_out_distance = this.correction_fade_out_distance,
                    considered_zones_count_v1 = this.considered_zones_count_v1,
                    algorithm = this.algorithm,
                    additional_dimensions_configuration = this.additional_dimensions_configuration.Clone()
                };
            }
            public bool Equals(CalibrationMode other)
            {
                return
                    zone_size == other.zone_size &&
                    max_zones_count == other.max_zones_count &&
                    considered_zones_count == other.considered_zones_count &&
                    size_of_opaque_sector_in_percents == other.size_of_opaque_sector_in_percents &&
                    shade_thickness_in_pixels == other.shade_thickness_in_pixels &&
                    considered_zones_count_v1 == other.considered_zones_count_v1 &&
                    correction_fade_out_distance == other.correction_fade_out_distance &&
                    algorithm == other.algorithm &&
                    additional_dimensions_configuration.Equals(other.additional_dimensions_configuration);
            }

            public static CalibrationMode SingleDimensionPreset
            {
                get
                {
                    return new CalibrationMode
                    {
                        zone_size = 150,
                        max_zones_count = 64,
                        considered_zones_count = 6,
                        size_of_opaque_sector_in_percents = 40,
                        shade_thickness_in_pixels = 10,
                        considered_zones_count_v1 = 14,
                        correction_fade_out_distance = 75,
                        additional_dimensions_configuration = AdditionalDimensionsConfguration.Disabled,
                    };
                }
            }

            public static CalibrationMode MultiDimensionPreset
            {
                get
                {
                    return new CalibrationMode
                    {
                        considered_zones_count = 6,
                        considered_zones_count_v1 = 14,
                        max_zones_count = 1024,
                        size_of_opaque_sector_in_percents = 30,
                        shade_thickness_in_pixels = 10,
                        correction_fade_out_distance = 75,
                        algorithm = "V2",
                        zone_size = 75,
                        additional_dimensions_configuration = new AdditionalDimensionsConfguration
                        {
                            LeftEye = new Vector3Percents { X = 5454, Y = 8726, Z = 3409 },
                            RightEye = Vector3Percents.Disabled,
                            AngleBetweenEyes = new Vector3Percents { X = 5454, Y = 128, Z = null },
                            HeadPosition = Vector3Percents.Disabled,
                            HeadDirection = Vector3Percents.Disabled
                        },
                    };
                }
            }

            public static CalibrationMode MultiDimensionPresetRightEye
            {
                get
                {
                    var retval = MultiDimensionPreset.Clone();
                    retval.additional_dimensions_configuration.RightEye = retval.additional_dimensions_configuration.LeftEye;
                    retval.additional_dimensions_configuration.LeftEye = Vector3Percents.Disabled;
                    return retval;
                }
            }
        }

        public CalibrationMode calibration_mode = CalibrationMode.MultiDimensionPreset;

        public bool user_consent_given = false;

        public int calibration_step = 5;

        public int reset_calibration_zone_size = 400; // Not configurable since it is hard to explain what it means.

        public int horizontal_scroll_step = 8;
        public int vertical_scroll_step = 8;

        public int calibrate_freeze_time_ms = 500;
        public int click_freeze_time_ms = 400;

        public int double_speedup_press_time_ms = 320;

        public int quadriple_speed_up_press_time_ms = 160;

        public int modifier_short_press_duration_ms = 100;

        public int instant_jump_distance = 100;
        public int smothening_points_count = 15;

        public static string Filepath { get { return Path.Combine(Helpers.UserDataFolder, "options.json"); } }

        public void SaveToFile(string file_path)
        {
            File.WriteAllText(file_path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Options Instance { get; set; } = LoadFromFile(Filepath);

        public static Options LoadFromFile(string file_path)
        {
            Options options = Default();
            try
            {
                if (File.Exists(file_path))
                {
                    options = JsonConvert.DeserializeObject<Options>(File.ReadAllText(file_path));
                }
            }
            catch (Exception)
            { }

            return options;
        }

        public static Options Default()
        {
            return new Options();
        }
    }
}
