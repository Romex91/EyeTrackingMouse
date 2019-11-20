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

    [Flags]
    public enum MultidimensionCalibrationType
    {
        None = 0,
        LeftEye = 1,
        RightEye = 2,
        HeadPosition = 4,
        HeadDirection = 8
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
            get => interception_method == InterceptionMethod.OblitaDriver ? bindings [key] : default_bindings[key];
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

    class CalibrationSettings
    {
        public int step = 3;
        public int zone_size = 150;
        public int max_zones_count = 25;
        public int considered_zones_count = 5; // Not configurable since it is hard to explain what it means.
        public int shift_ttl_ms = 50; // Not configurable since it is hard to explain what it means.
        public int reset_zone_size = 400; // Not configurable since it is hard to explain what it means.

        public MultidimensionCalibrationType multidimension_calibration_type = MultidimensionCalibrationType.LeftEye;
        public double multi_dimensions_detalization = 10;

        public static CalibrationSettings SingleDemensionPreset()
        {
            return new CalibrationSettings
            {
                considered_zones_count = 5,
                max_zones_count = 25,
                multidimension_calibration_type = MultidimensionCalibrationType.None,
                multi_dimensions_detalization = 1,
                reset_zone_size = 400,
                shift_ttl_ms = 50,
                step = 3,
                zone_size = 150
            };
        }
        public static CalibrationSettings MultiDemensionPreset()
        {
            return new CalibrationSettings
            {
                considered_zones_count = 7,
                max_zones_count = 700,
                multidimension_calibration_type = MultidimensionCalibrationType.HeadDirection | MultidimensionCalibrationType.HeadPosition,
                multi_dimensions_detalization = 7,
                reset_zone_size = 400,
                shift_ttl_ms = 50,
                step = 3,
                zone_size = 150
            };
        }
    }

    // TODO: check all accesses to this and other shared classes are in critical section.
    class Options
    {
        public KeyBindings key_bindings = new KeyBindings();

        public CalibrationSettings calibration = CalibrationSettings.MultiDemensionPreset();

        public int horizontal_scroll_step = 4;
        public int vertical_scroll_step = 4;

        public int calibrate_freeze_time_ms = 500;
        public int click_freeze_time_ms = 400;

        public int double_speedup_press_time_ms = 320;

        public int quadriple_speed_up_press_time_ms = 160;

        public int modifier_short_press_duration_ms = 100;

        public int smothening_zone_radius = 250;
        public int smothening_points_count = 15;

        private static string Filepath { get { return Path.Combine(Helpers.GetLocalFolder(), "options.json"); } }

        public void SaveToFile()
        {
            File.WriteAllText(Filepath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Options Instance { get; set; } = LoadFromFile();

        public static Options LoadFromFile()
        {
            Options options = Default();
            if (File.Exists(Filepath))
            {
                options = JsonConvert.DeserializeObject<Options>(File.ReadAllText(Filepath));
            }
            return options;
        }
        public static Options Default()
        {
            return new Options();
        }
    }
}
