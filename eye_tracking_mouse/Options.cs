using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

    public struct Vector3Bool
    {
        public bool X { get; set; }
        public bool Y { get; set; }
        public bool Z { get; set; }

        public bool Equals(Vector3Bool other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public static Vector3Bool Disabled
        {
            get
            {
                return new Vector3Bool { X = false, Y = false, Z = false };
            }
        }
    }

    public struct AdditionalDimensionsConfguration
    {
        public Vector3Bool LeftEye;
        public Vector3Bool RightEye;
        public Vector3Bool HeadPosition;
        public Vector3Bool HeadDirection;

        [JsonIgnore]
        public int CoordinatesCount
        {
            get
            {
                // X and Y are always there.
                int coordinates_count = 2;
                foreach (var vector3 in new List<Vector3Bool> { LeftEye, RightEye, HeadDirection, HeadPosition})
                {
                    if (vector3.X)
                        coordinates_count++;
                    if (vector3.Y)
                        coordinates_count++;
                    if (vector3.Z)
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
                HeadDirection.Equals(other.HeadDirection) &&
                HeadPosition.Equals(other.HeadPosition);
        }

        public static AdditionalDimensionsConfguration Disabled
        {
            get {
                return new AdditionalDimensionsConfguration
                {
                    LeftEye = Vector3Bool.Disabled,
                    RightEye = Vector3Bool.Disabled,
                    HeadPosition = Vector3Bool.Disabled,
                    HeadDirection = Vector3Bool.Disabled
                };
            }
        }
    }

    class KeyBindings
    {
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
    class Options
    {
        public KeyBindings key_bindings = new KeyBindings();

        public class CalibrationMode
        {
            public int zone_size;
            public int max_zones_count;
            public int considered_zones_count;
            public int update_period_ms;

            // TODO: remove the option. There can be only one!
            public string algorithm = "V0";

            public AdditionalDimensionsConfguration additional_dimensions_configuration;
            public int multi_dimensions_detalization;

            public bool Equals(CalibrationMode other)
            {
                return
                    zone_size == other.zone_size &&
                    max_zones_count == other.max_zones_count &&
                    considered_zones_count == other.considered_zones_count &&
                    update_period_ms == other.update_period_ms &&
                    additional_dimensions_configuration.Equals(other.additional_dimensions_configuration) &&
                    multi_dimensions_detalization == other.multi_dimensions_detalization;
            }

            public static CalibrationMode SingleDimensionPreset
            {
                get
                {
                    return new CalibrationMode
                    {
                        considered_zones_count = 5,
                        max_zones_count = 25,
                        additional_dimensions_configuration = AdditionalDimensionsConfguration.Disabled,
                        multi_dimensions_detalization = 100,
                        update_period_ms = 20,
                        zone_size = 150
                    };
                }
            }

            public static CalibrationMode MultiDimensionPreset
            {
                get
                {
                    return new CalibrationMode
                    {
                        considered_zones_count = 7,
                        max_zones_count = 2000,
                        additional_dimensions_configuration = new AdditionalDimensionsConfguration
                        {
                            LeftEye = new Vector3Bool { X = true, Y = true, Z = true },
                            RightEye = new Vector3Bool { X = true, Y = true, Z = true },
                            HeadPosition = Vector3Bool.Disabled,
                            HeadDirection = Vector3Bool.Disabled
                        },

                        multi_dimensions_detalization = 70,
                        update_period_ms = 20,
                        zone_size = 150
                    };
                }
            }

        }
        public CalibrationMode calibration_mode = CalibrationMode.SingleDimensionPreset;

        public int calibration_step = 5;

        public int reset_calibration_zone_size = 400; // Not configurable since it is hard to explain what it means.

        public int horizontal_scroll_step = 8;
        public int vertical_scroll_step = 8;

        public int calibrate_freeze_time_ms = 500;
        public int click_freeze_time_ms = 400;

        public int double_speedup_press_time_ms = 320;

        public int quadriple_speed_up_press_time_ms = 160;

        public int modifier_short_press_duration_ms = 100;

        public int smothening_zone_radius = 250;
        public int smothening_points_count = 15;

        private static string Filepath { get { return Path.Combine(Helpers.UserDataFolder, "options.json"); } }

        public void SaveToFile()
        {
            File.WriteAllText(Filepath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Options Instance { get; set; } = LoadFromFile();

        public static Options LoadFromFile()
        {
            Options options = Default();
            try
            {
                if (File.Exists(Filepath))
                {
                    options = JsonConvert.DeserializeObject<Options>(File.ReadAllText(Filepath));
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
