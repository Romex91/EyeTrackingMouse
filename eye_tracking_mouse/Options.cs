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
        public readonly KeyBindings key_bindings = new KeyBindings();
        public int calibration_step = 5;
        public int horizontal_scroll_step = 6;
        public int vertical_scroll_step = 6;

        public int win_press_delay_ms = 1;
        public int click_freeze_time_ms = 200;
        public int double_click_duration_ms = 300;
        public int short_click_duration_ms = 300;
        public int calibration_shift_ttl_ms = 100;

        public int smothening_zone_radius = 100;
        public int smothening_points_count = 15;

        private static string Filepath { get { return Path.Combine(Helpers.GetLocalFolder(), "options.json"); } }

        public void SaveToFile()
        {
            File.WriteAllText(Filepath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Options LoadFromFile()
        {
            Options options = Default();
            if (File.Exists(Filepath))
            {
                JsonConvert.DeserializeObject<Options>(File.ReadAllText(Filepath));
            }
            return options;
        }
        public static Options Default()
        {
            return new Options();
        }
    }
}
