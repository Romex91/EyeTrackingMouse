using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.Interaction;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;

namespace move_mouse
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
        public int calibration_step = 10;
        public int horizontal_scroll_step = 6;
        public int vertical_scroll_step = 6;
        public int win_press_delay_ms = 1;
        public int click_freeze_time_ms = 200;
        public int double_click_duration_ms = 300;
        public int short_click_duration_ms = 300;
    }

    enum ApplicationState
    {
        // Application does nothing. 
        Idle,
        // Application moves cursor to gaze point applying previous calibrations. To enable this state user should press modifier key.
        Controlling,
        // User can press W/A/S/D while holding modifier to calibrate cursor position to fit user's gaze more accurately. 
        Calibrating
    };

    struct InteractionHistoryEntry
    {
        public Interceptor.Keys Key;
        public Interceptor.KeyState State;
        public DateTime Time;
    };

    class Program
    {
        // TODO: think accurately about thread safety.
        static readonly object _locker = new object();

        private static readonly Options options = new Options();

        private static ApplicationState application_state = ApplicationState.Idle;

        private static Point gaze_point = new Point(0, 0);

        // |gaze_point| is not accurate. To enable precise cursor control the application supports calibration by W/A/S/D.
        // |calibration_shift| is result of such calibration. Application sets cursor position to |gaze_point| + |calibration_shift| when in |Controlling| state.
        private static Point calibration_shift = new Point(0, 0);
        private static readonly ShiftsStorage shifts_storage = new ShiftsStorage();

        // Updating |calibration_shift| may be expensive. These variables tracks whether update is required.
        private static bool is_calibration_shift_outdated = false;
        private static DateTime last_shift_update_time = DateTime.Now;

        private static readonly InteractionHistoryEntry[] interaction_history = new InteractionHistoryEntry[3];

        private static readonly Interceptor.Input input = new Interceptor.Input();

        // For hardcoded stop-word.
        private static bool is_win_pressed = false;

        // Interceptor.KeyState is a mess. Different Keys produce different KeyState when pressed and released.
        // TODO: Figure out full list of e0 keys;
        private static SortedSet<Interceptor.Keys> e0_keys = new SortedSet<Interceptor.Keys> { Interceptor.Keys.WindowsKey, Interceptor.Keys.Delete };
        private static Interceptor.KeyState GetDownKeyState(Interceptor.Keys key)
        {
            if (e0_keys.Contains(key))
                return Interceptor.KeyState.E0;
            return Interceptor.KeyState.Down;
        }
        private static Interceptor.KeyState GetUpKeyState(Interceptor.Keys key)
        {
            if (e0_keys.Contains(key))
                return Interceptor.KeyState.E0 | Interceptor.KeyState.Up;
            return Interceptor.KeyState.Up;
        }

        private static void OnKeyPressed(object sender, Interceptor.KeyPressedEventArgs e)
        {
            // Console.WriteLine(e.Key);
            // Console.WriteLine(e.State);

            e.Handled = true;

            // Hardcoded stop-word is Win+Del.
            if (e.Key == Interceptor.Keys.WindowsKey)
            {
                if (e.State == GetDownKeyState(e.Key))
                    is_win_pressed = true;
                else if (e.State == GetUpKeyState(e.Key))
                    is_win_pressed = false;
            }
            if (e.Key == Interceptor.Keys.Delete &&
                e.State == GetDownKeyState(e.Key) && is_win_pressed)
            {
                Application.Exit();
                return;
            }

            // If you hold a key pressed for a second it will start to produce a sequence of rrrrrrrrrrepeated |KeyState.Down| events.
            // For most keys we don't want to handle such events and assume that a key stays pressed until |KeyState.Up| appears.
            var repeteation_white_list = new SortedSet<Interceptor.Keys> {
                options.key_bindings.calibrate_down,
                options.key_bindings.calibrate_up,
                options.key_bindings.calibrate_left,
                options.key_bindings.calibrate_right,
                options.key_bindings.scroll_down,
                options.key_bindings.scroll_up,
                options.key_bindings.scroll_left,
                options.key_bindings.scroll_right,
            };
            if (!repeteation_white_list.Contains(e.Key) && 
                interaction_history[0].Key == e.Key &&
                interaction_history[0].State == e.State && 
                e.State == GetDownKeyState(e.Key))
            {
                if (application_state == ApplicationState.Idle)
                    e.Handled = false;
                return;
            }

            lock (_locker)
            {
                interaction_history[2] = interaction_history[1];
                interaction_history[1] = interaction_history[0];
                interaction_history[0].Key = e.Key;
                interaction_history[0].State = e.State;
                interaction_history[0].Time = DateTime.Now;
            }

            bool is_double_press =
                e.State == GetDownKeyState(e.Key) &&
                interaction_history[1].Key == e.Key &&
                interaction_history[2].Key == e.Key &&
                (DateTime.Now - interaction_history[2].Time).TotalMilliseconds < options.double_click_duration_ms;

            bool is_short_press =
                e.State == GetUpKeyState(e.Key) &&
                interaction_history[1].Key == e.Key &&
                (DateTime.Now - interaction_history[1].Time).TotalMilliseconds < options.short_click_duration_ms;

            // The application grabs control over cursor when modifier is pressed.
            if (e.Key == options.key_bindings.modifier)
            {
                if (e.State == GetDownKeyState(e.Key))
                {
                    application_state = ApplicationState.Controlling;
                }
                else if (e.State == GetUpKeyState(e.Key))
                {
                    if (application_state == ApplicationState.Idle)
                    {
                        e.Handled = false;
                    }
                    else if (is_short_press)
                    {
                        input.SendKey(e.Key, GetDownKeyState(e.Key));
                        Thread.Sleep(options.win_press_delay_ms);
                        input.SendKey(e.Key, GetUpKeyState(e.Key));
                    }

                    application_state = ApplicationState.Idle;
                }
                return;
            }

            if (application_state == ApplicationState.Idle)
            {
                e.Handled = false;
                return;
            }

            bool is_key_bound = false;
            foreach (var key_binding in typeof(KeyBindings).GetFields())
            {
                if (key_binding.FieldType == typeof(Interceptor.Keys) && key_binding.GetValue(options.key_bindings).Equals(e.Key))
                {
                    is_key_bound = true;
                }
            }
            if (!is_key_bound)
            {
                // The application intercepts modifier key presses. We do not want to lose modifier when handling unbound keys.
                // We stop controlling cursor when facing the first unbound key and send modifier keystroke to OS before handling pressed key.
                application_state = ApplicationState.Idle;
                input.SendKey(options.key_bindings.modifier, GetDownKeyState(options.key_bindings.modifier));
                e.Handled = false;
                return;
            }

            if (e.State == GetDownKeyState(e.Key))
            {
                // Calibration
                int calibration_step = options.calibration_step * (is_double_press ? 2 : 1);
                if (e.Key == options.key_bindings.calibrate_left)
                {
                    application_state = ApplicationState.Calibrating;
                    calibration_shift.X -= calibration_step;
                }
                if (e.Key == options.key_bindings.calibrate_right)
                {
                    application_state = ApplicationState.Calibrating;
                    calibration_shift.X += calibration_step;
                }
                if (e.Key == options.key_bindings.calibrate_up)
                {
                    application_state = ApplicationState.Calibrating;
                    calibration_shift.Y -= calibration_step;
                }
                if (e.Key == options.key_bindings.calibrate_down)
                {
                    application_state = ApplicationState.Calibrating;
                    calibration_shift.Y += calibration_step;
                }

                // Scroll
                if (e.Key == options.key_bindings.scroll_down)
                {
                    Mouse.WheelDown(options.vertical_scroll_step * (is_double_press ? 2 : 1));
                }
                if (e.Key == options.key_bindings.scroll_up)
                {
                    Mouse.WheelUp(options.vertical_scroll_step * (is_double_press ? 2 : 1));
                }
                if (e.Key == options.key_bindings.scroll_left)
                {
                    Mouse.WheelLeft(options.horizontal_scroll_step * (is_double_press ? 2 : 1));
                }
                if (e.Key == options.key_bindings.scroll_right)
                {
                    Mouse.WheelRight(options.horizontal_scroll_step * (is_double_press ? 2 : 1));
                }
            }

            // Mouse buttons
            if (application_state == ApplicationState.Calibrating &&
                e.State == GetDownKeyState(e.Key) &&
                (e.Key == options.key_bindings.left_click || e.Key == options.key_bindings.right_click))
            {
                lock (_locker)
                {
                    shifts_storage.AddShift(gaze_point, calibration_shift);
                    application_state = ApplicationState.Controlling;
                }
            }
            if (e.Key == options.key_bindings.left_click)
            {
                if (e.State == GetDownKeyState(e.Key))
                    Mouse.LeftDown();
                else if (e.State == GetUpKeyState(e.Key))
                    Mouse.LeftUp();
            }
            if (e.Key == options.key_bindings.right_click)
            {
                if (e.State == GetDownKeyState(e.Key))
                    Mouse.RightDown();
                else if (e.State == GetUpKeyState(e.Key))
                    Mouse.RightUp();
            }

            if (e.Key == options.key_bindings.reset_calibration)
            {
                lock (_locker)
                {
                    shifts_storage.ResetClosest(gaze_point);
                    if (is_double_press)
                        shifts_storage.Reset();
                }
            }
        }

        static void UpdateCursorPosition()
        {
            double dpiX, dpiY;
            Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);
            dpiX = graphics.DpiX / 100.0;
            dpiY = graphics.DpiY / 100.0;

            Cursor.Position = new Point((int)((gaze_point.X + calibration_shift.X) * dpiX), (int)((gaze_point.Y + calibration_shift.Y) * dpiY));
        }

        static void Main(string[] args)
        {
            Host host = new Host();
            GazePointDataStream gazePointDataStream = host.Streams.CreateGazePointDataStream();

            gazePointDataStream.GazePoint((x, y, ts) =>
            {
                lock (_locker)
                {
                    if (application_state == ApplicationState.Controlling || application_state == ApplicationState.Calibrating)
                    {
                        // Freeze cursor for a short period of time after mouse clicks to make double clicks esier.
                        foreach (var interaction in interaction_history)
                        {
                            if (interaction.State != GetDownKeyState(interaction.Key))
                                continue;
                            if (interaction.Key != options.key_bindings.left_click && interaction.Key != options.key_bindings.right_click)
                                continue;
                            if ((DateTime.Now - interaction.Time).TotalMilliseconds < options.click_freeze_time_ms)
                                return;
                        }
                        
                        gaze_point.X = (int)(x / 1);
                        gaze_point.Y = (int)(y / 1);
                        UpdateCursorPosition();
                        is_calibration_shift_outdated = true;
                    }
                }
            });

            Application.Idle += (object sender, EventArgs e) =>
            {
                lock (_locker)
                {
                    if (is_calibration_shift_outdated && application_state == ApplicationState.Controlling)
                    {
                        calibration_shift = shifts_storage.GetShift(gaze_point);
                        UpdateCursorPosition();
                        is_calibration_shift_outdated = false;
                    }
                }
            };

            Console.WriteLine("Close me to stop handling tobii hotкeys");

            input.KeyboardFilterMode = Interceptor.KeyboardFilterMode.All;
            if (input.Load())
            {
                input.OnKeyPressed += OnKeyPressed;
            }

            Application.Run();

            host.Dispose();
        }
    }
}
