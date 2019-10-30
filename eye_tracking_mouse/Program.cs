

using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
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

        private static readonly string filepath = Path.Combine(Helpers.GetLocalFolder(), "options.json");

        public void SaveToFile()
        {
            File.WriteAllText(filepath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Options LoadFromFile()
        {
            Options options = Default();
            if (File.Exists(filepath))
            {
                JsonConvert.DeserializeObject<Options>(File.ReadAllText(filepath));
            }
            return options;
        }
        public static Options Default()
        {
            return new Options();
        }
    }

    class Program
    {
        private static readonly Options options = Options.LoadFromFile();
        private static EyeTrackingMouse eye_tracking_mouse;
        private static InputManager input_manager;
        // TODO: prevent multiple windows.
        private static void OpenSettings(object sender, EventArgs e)
        {
            MessageBox.Show("Settings", Helpers.application_name);
        }
        private static void OpenAbout(object sender, EventArgs e)
        {
            MessageBox.Show("About", Helpers.application_name);
        }
        private static void Exit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        static void Main(string[] args)
        {
            if (!Directory.Exists(Helpers.GetLocalFolder()))
            {
                Directory.CreateDirectory(Helpers.GetLocalFolder());
            }

            // Tray icon initialization
            {
                ContextMenuStrip context_menu_strip = new ContextMenuStrip { Visible = true };

                context_menu_strip.SuspendLayout();

                ToolStripMenuItem settings = new ToolStripMenuItem { Text = "Settings", Visible = true };
                settings.Click += OpenSettings;

                ToolStripMenuItem about = new ToolStripMenuItem { Text = "About " + Helpers.application_name, Visible = true };
                about.Click += OpenAbout;

                ToolStripMenuItem exit = new ToolStripMenuItem { Text = "Exit", Visible = true };
                exit.Click += Exit;

                context_menu_strip.Items.AddRange(new ToolStripItem[] { settings, about, exit });

                context_menu_strip.ResumeLayout(false);
                Helpers.tray_icon.ContextMenuStrip = context_menu_strip;
                Application.ApplicationExit += (object sender, EventArgs e) => { Helpers.tray_icon.Visible = false; };
            }

            eye_tracking_mouse = new EyeTrackingMouse(options);
            input_manager = new InputManager(eye_tracking_mouse, options);

            Application.Run();
            eye_tracking_mouse.Dispose();
        }
    }
}
