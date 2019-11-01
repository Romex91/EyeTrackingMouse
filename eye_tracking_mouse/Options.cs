using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    class KeyBindings
    {
        public Interceptor.Keys modifier = Interceptor.Keys.WindowsKey;
        // Some modifiers have different Up and Down states depending on whether it is right or left.
        // Humanity has messy keyboard scancodes.
        public bool is_modifier_e0 = true;
        public Interceptor.Keys left_click = Interceptor.Keys.J;
        public Interceptor.Keys right_click = Interceptor.Keys.K;
        public Interceptor.Keys scroll_down = Interceptor.Keys.N;
        public Interceptor.Keys scroll_up = Interceptor.Keys.H;
        public Interceptor.Keys scroll_left = Interceptor.Keys.CommaLeftArrow;
        public Interceptor.Keys scroll_right = Interceptor.Keys.PeriodRightArrow;
        public Interceptor.Keys reset_calibration = Interceptor.Keys.M;
        public Interceptor.Keys calibrate_left = Interceptor.Keys.A;
        public Interceptor.Keys calibrate_right = Interceptor.Keys.D;
        public Interceptor.Keys calibrate_up = Interceptor.Keys.W;
        public Interceptor.Keys calibrate_down = Interceptor.Keys.S;
    };

    class Options
    {
        public KeyBindings key_bindings = new KeyBindings();
        public int calibration_step = 5;
        public int calibration_zone_size = 150; // TODO: add options
        public int calibration_points_count = 45; // TODO: add options
        public int calibration_shift_ttl_ms = 100; // Not configurable since it is hard to explain what it means.

        public int horizontal_scroll_step = 4;
        public int vertical_scroll_step = 4;

        public int win_press_delay_ms = 10; // Not configurable since changing it wouldn't improve user experience.
        public int calibrate_freeze_time_ms = 800;
        public int click_freeze_time_ms = 400;
        public int double_click_duration_ms = 300;
        public int short_click_duration_ms = 300;

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
