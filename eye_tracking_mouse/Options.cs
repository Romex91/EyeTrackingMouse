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
        public InterceptionMethod interception_method = InterceptionMethod.OblitaDriver;

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

    class Options
    {
        public KeyBindings key_bindings = new KeyBindings();

        public int calibration_step = 5;
        public int calibration_zone_size = 150; // TODO: add options
        public int calibration_max_zones_count = 45; // TODO: add options
        public int calibration_shift_ttl_ms = 100; // Not configurable since it is hard to explain what it means.
        public int calibration_reset_zone_size = 400; // Not configurable since it is hard to explain what it means.

        public int horizontal_scroll_step = 4;
        public int vertical_scroll_step = 4;

        public int win_press_delay_ms = 10; // Not configurable since changing it wouldn't improve user experience.
        public int calibrate_freeze_time_ms = 800;
        public int click_freeze_time_ms = 400;
        public int double_click_duration_ms = 300;
        public int short_click_duration_ms = 150;

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
