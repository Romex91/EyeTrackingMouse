using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eye_tracking_mouse
{
    class EyeTrackingMouse : IDisposable
    {
        private readonly Tobii.Interaction.Host host = new Tobii.Interaction.Host();
        private readonly Tobii.Interaction.GazePointDataStream gazePointDataStream;
        private readonly Options options;

        private Point gaze_point = new Point(0, 0);
        private readonly GazeSmoother gaze_smoother;

        // |gaze_point| is not accurate. To enable precise cursor control the application supports calibration by W/A/S/D.
        // |calibration_shift| is result of such calibration. Application sets cursor position to |gaze_point| + |calibration_shift| when in |Controlling| state.
        private Point calibration_shift = new Point(0, 0);
        private readonly ShiftsStorage shifts_storage = new ShiftsStorage();

        // Updating |calibration_shift| may be expensive. These variables tracks whether update is required.
        private DateTime last_shift_update_time = DateTime.Now;

        private DateTime freeze_time = DateTime.Now;

        // For dpi.
        private Graphics graphics = Graphics.FromHwnd(IntPtr.Zero);

        public enum MouseState
        {
            // Application does nothing. 
            Idle,
            // Application moves cursor to gaze point applying previous calibrations. To enable this state user should press modifier key.
            Controlling,
            // User can press W/A/S/D while holding modifier to calibrate cursor position to fit user's gaze more accurately. 
            Calibrating
        };

        public MouseState mouse_state { private set; get; } = MouseState.Idle;

        private void UpdateCursorPosition()
        {
            double dpiX, dpiY;
            dpiX = graphics.DpiX / 100.0;
            dpiY = graphics.DpiY / 100.0;

            Cursor.Position = new Point((int)((gaze_point.X + calibration_shift.X) * dpiX), (int)((gaze_point.Y + calibration_shift.Y) * dpiY));
        }

        private void OnGazePoint(double x, double y, double ts)
        {
            lock (Helpers.locker)
            {
                if (mouse_state == MouseState.Controlling || mouse_state == MouseState.Calibrating)
                {
                    if ((DateTime.Now - freeze_time).TotalMilliseconds > options.click_freeze_time_ms)
                    {
                        gaze_smoother.AddGazePoint(new Point((int)x, (int)y));
                        gaze_point = gaze_smoother.GetSmoothenedGazePoint();

                        if (mouse_state == MouseState.Controlling &&
                            (DateTime.Now - last_shift_update_time).TotalMilliseconds > options.calibration_shift_ttl_ms)
                        {
                            last_shift_update_time = DateTime.Now;
                            calibration_shift = shifts_storage.GetShift(gaze_point);
                        }
                    }

                    UpdateCursorPosition();
                }
            }
        }

        public void StartControlling()
        {
            mouse_state = MouseState.Controlling;
        }

        public void StopControlling()
        {
            mouse_state = MouseState.Idle;
        }

        public void OnKeyPressed(Interceptor.Keys key, InputManager.KeyState state, bool is_double_press)
        {
            Debug.Assert(mouse_state != MouseState.Idle);

            if (state == InputManager.KeyState.Down)
            {
                // Calibration
                int calibration_step = (int)(options.calibration_step * (is_double_press ? 2.5 : 1.0));
                if (key == options.key_bindings.calibrate_left)
                {
                    mouse_state = MouseState.Calibrating;
                    calibration_shift.X -= calibration_step;
                    freeze_time = DateTime.Now;
                }
                if (key == options.key_bindings.calibrate_right)
                {
                    mouse_state = MouseState.Calibrating;
                    calibration_shift.X += calibration_step;
                    freeze_time = DateTime.Now;
                }
                if (key == options.key_bindings.calibrate_up)
                {
                    mouse_state = MouseState.Calibrating;
                    calibration_shift.Y -= calibration_step;
                    freeze_time = DateTime.Now;
                }
                if (key == options.key_bindings.calibrate_down)
                {
                    mouse_state = MouseState.Calibrating;
                    calibration_shift.Y += calibration_step;
                    freeze_time = DateTime.Now;
                }

                // Scroll
                if (key == options.key_bindings.scroll_down)
                {
                    MouseButtons.WheelDown(options.vertical_scroll_step * (is_double_press ? 2 : 1));
                }
                if (key == options.key_bindings.scroll_up)
                {
                    MouseButtons.WheelUp(options.vertical_scroll_step * (is_double_press ? 2 : 1));
                }
                if (key == options.key_bindings.scroll_left)
                {
                    MouseButtons.WheelLeft(options.horizontal_scroll_step * (is_double_press ? 2 : 1));
                }
                if (key == options.key_bindings.scroll_right)
                {
                    MouseButtons.WheelRight(options.horizontal_scroll_step * (is_double_press ? 2 : 1));
                }
            }

            // Mouse buttons
            if (mouse_state == MouseState.Calibrating &&
                state == InputManager.KeyState.Down &&
                (key == options.key_bindings.left_click || key == options.key_bindings.right_click))
            {
                shifts_storage.AddShift(gaze_point, calibration_shift);
                mouse_state = MouseState.Controlling;
            }

            if (key == options.key_bindings.left_click)
            {
                if (state == InputManager.KeyState.Down)
                {
                    // Freeze cursor for a short period of time after mouse clicks to make double clicks esier.
                    MouseButtons.LeftDown();
                    freeze_time = DateTime.Now;
                } else if (state == InputManager.KeyState.Up)
                {
                    MouseButtons.LeftUp();
                }
            }

            if (key == options.key_bindings.right_click)
            {
                if (state == InputManager.KeyState.Down)
                {
                    // Freeze cursor for a short period of time after mouse clicks to make double clicks esier.
                    MouseButtons.RightDown();
                    freeze_time = DateTime.Now;
                }
                else if (state == InputManager.KeyState.Up)
                {
                    MouseButtons.RightUp();
                }
            }

            if (key == options.key_bindings.reset_calibration)
            {
                shifts_storage.ResetClosest(gaze_point);
                if (is_double_press)
                {
                    Helpers.ShowBaloonNotification("Calibration has been reset.");
                    shifts_storage.Reset();
                }
            }
        }


        public EyeTrackingMouse(Options options)
        {
            this.options = options;
            gaze_smoother = new GazeSmoother(options);
            gazePointDataStream = host.Streams.CreateGazePointDataStream();
            gazePointDataStream.GazePoint(OnGazePoint);
        }

        public void Dispose()
        {
            host.Dispose();
            graphics.Dispose();
        }
    }
}
