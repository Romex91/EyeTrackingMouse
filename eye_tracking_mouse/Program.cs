

using System.Windows.Forms;

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
    }

    class Program
    {
        private static readonly Options options = new Options();

        static void Main(string[] args)
        {
            var eye_tracking_mouse = new EyeTrackingMouse(options);
            var input_manager = new InputManager(eye_tracking_mouse, options);
            Application.Run();
            eye_tracking_mouse.Dispose();

        }
    }
}
