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
    public class EyeTrackingMouse : IDisposable
    {
        private EyeTrackerErrorCorrection smoothened_error_correction;

        // A point user looked at when he started calibration. 
        // If user's gaze leaves area around this point the calibration will be over.
        private Point calibration_start_gaze_point = new Point(0, 0);

        private DateTime freeze_until = DateTime.Now;

        private Statistics statistics = Statistics.LoadFromFile(Statistics.Filepath);

        private DateTime idle_start_time = DateTime.Now;

        private TobiiCoordinatesProvider tobii_coordinates_provider;

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
            MouseButtons.Move(
                smoothened_error_correction.сoordinates[0] + smoothened_error_correction.shift.X,
                smoothened_error_correction.сoordinates[1] + smoothened_error_correction.shift.Y);
        }

        private void OnNewCoordinates(TobiiCoordinates coordinates)
        {
            lock (Helpers.locker)
            {
                if (mouse_state == MouseState.Idle
                    && (DateTime.Now - idle_start_time).TotalSeconds > 60)
                {
                    CoordinateSmoother.Reset();
                    return;
                }

                if (DateTime.Now > freeze_until)
                {
                    if (mouse_state == MouseState.Calibrating &&
                        Helpers.GetDistance(coordinates.gaze_point, calibration_start_gaze_point) > Options.Instance.reset_calibration_zone_size)
                    {
                        mouse_state = MouseState.Controlling;
                    }

                    if (mouse_state == MouseState.Calibrating)
                    {
                        // The only thing to update while calibrating is gaze point.
                        float[] coordinates_copy = new float[smoothened_error_correction.сoordinates.Length];
                        smoothened_error_correction.сoordinates.CopyTo(coordinates_copy, 0);
                        var smoothened_error_correction_clone = new EyeTrackerErrorCorrection(coordinates_copy, smoothened_error_correction.shift);
                        smoothened_error_correction_clone.сoordinates[0] = coordinates.gaze_point.X;
                        smoothened_error_correction_clone.сoordinates[1] = coordinates.gaze_point.Y;
                        smoothened_error_correction = CoordinateSmoother.Smoothen(smoothened_error_correction_clone);
                    }
                    else
                    {
                        // The eye tracker provides shaky data that has to be smoothened before transforming to the mouse cursor position.
                        // |CalibrationManager| amplifies this shaking.
                        // To cancel the amplification we smoothen data BEFORE it goes to |CalibrationManager|.
                        //
                        // Another problem is |CalibrationManager| also may be a source of shaking (even on smoothened input). 
                        // So in addition to smoothening its input we have to smoothen its output. 
                        // Smoothening data twice leads to bigger latency but otherwise, the cursor shakes. 
                        // Big latency is compensated by |instant_jump_distance|.
                        var shift = smoothened_error_correction == null ? new Point(0,0) : CalibrationManager.Instance.GetShift(smoothened_error_correction.сoordinates);
                        var shaky_coordinates = coordinates.ToCoordinates(Options.Instance.calibration_mode.additional_dimensions_configuration);
                        smoothened_error_correction = CoordinateSmoother.Smoothen(new EyeTrackerErrorCorrection(shaky_coordinates, shift));
                    }
                }
                else
                {
                    // Adds inertia on exit from freeze state.
                    CoordinateSmoother.Smoothen(smoothened_error_correction);
                }

                if (mouse_state == MouseState.Controlling || mouse_state == MouseState.Calibrating)
                {
                    UpdateCursorPosition();
                }
            }
        }

        public void StopControlling()
        {
            lock (Helpers.locker)
            {
                freeze_until = DateTime.Now;
                mouse_state = MouseState.Idle;
                idle_start_time = DateTime.Now;

                if (MouseButtons.RightPressed)
                    MouseButtons.RightUp();
                if (MouseButtons.LeftPressed)
                    MouseButtons.LeftUp();
            }
        }

        public void StartControlling()
        {
            lock (Helpers.locker)
            {
                mouse_state = MouseState.Controlling;
                tobii_coordinates_provider.Restart();
            }
        }

        private void StartCalibration()
        {
            if (mouse_state != MouseState.Calibrating)
            {
                calibration_start_gaze_point =
                    new Point((int)smoothened_error_correction.сoordinates[0],
                              (int)smoothened_error_correction.сoordinates[1]);
                mouse_state = MouseState.Calibrating;
            }
            freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.calibrate_freeze_time_ms);
            statistics.OnCalibrate();
            CoordinateSmoother.Reset();
        }

        public bool OnKeyPressed(
            Key key,
            KeyState key_state,
            float speed_up,
            bool is_repetition,
            bool is_modifier,
            InputProvider input_provider)
        {
            // The application grabs control over cursor when modifier is pressed.
            if (key == Key.Modifier)
            {
                if (key_state == KeyState.Down)
                {
                    StartControlling();
                    return true;
                }

                if (key_state == KeyState.Up)
                {
                    if (mouse_state == EyeTrackingMouse.MouseState.Idle)
                    {
                        return false;
                    }

                    StopControlling();
                    return true;
                }
            }

            if (mouse_state == EyeTrackingMouse.MouseState.Idle)
            {
                return false;
            }

            if (key == Key.Unbound)
            {
                // The application intercepts modifier key presses. We do not want to lose modifier when handling unbound keys.
                // We stop controlling cursor when facing the first unbound key and send modifier keystroke to OS before handling pressed key.
                // This way key combinations like 'Win+E' remain available.
                if (!is_modifier)
                {
                    input_provider.SendModifierDown();
                    StopControlling();
                }
                return false;
            }

            var repetition_white_list = new SortedSet<Key> {
                    Key.ScrollDown,
                    Key.ScrollUp,
                    Key.ScrollLeft,
                    Key.ScrollRight,
                    Key.CalibrateLeft,
                    Key.CalibrateRight,
                    Key.CalibrateUp,
                    Key.CalibrateDown,
                };

            if (is_repetition && !repetition_white_list.Contains(key))
                return true;

            if (key_state == KeyState.Down)
            {
                // Calibration
                int calibration_step = (int)(Options.Instance.calibration_step * speed_up);
                if (key == Key.CalibrateLeft)
                {
                    StartCalibration();
                    smoothened_error_correction.shift.X -= calibration_step;
                }
                if (key == Key.CalibrateRight)
                {
                    StartCalibration();
                    smoothened_error_correction.shift.X += calibration_step;
                }
                if (key == Key.CalibrateUp)
                {
                    StartCalibration();
                    smoothened_error_correction.shift.Y -= calibration_step;
                }
                if (key == Key.CalibrateDown)
                {
                    StartCalibration();
                    smoothened_error_correction.shift.Y += calibration_step;
                }

                // Scroll
                if (key == Key.ScrollDown)
                {
                    MouseButtons.WheelDown((int)(Options.Instance.vertical_scroll_step * speed_up));
                }
                if (key == Key.ScrollUp)
                {
                    MouseButtons.WheelUp((int)(Options.Instance.vertical_scroll_step * speed_up));
                }
                if (key == Key.ScrollLeft)
                {
                    MouseButtons.WheelLeft((int)(Options.Instance.horizontal_scroll_step * speed_up));
                }
                if (key == Key.ScrollRight)
                {
                    MouseButtons.WheelRight((int)(Options.Instance.horizontal_scroll_step * speed_up));
                }
            }

            // Mouse buttons
            if (mouse_state == MouseState.Calibrating &&
                (key == Key.LeftMouseButton || key == Key.RightMouseButton))
            {
                CalibrationManager.Instance.AddShift(smoothened_error_correction.сoordinates, smoothened_error_correction.shift);
                mouse_state = MouseState.Controlling;
            }

            if (key == Key.LeftMouseButton)
            {
                if (key_state == KeyState.Down)
                {
                    // Freeze cursor for a short period of time after mouse clicks to make double clicks esier.
                    MouseButtons.LeftDown();
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.click_freeze_time_ms);
                    statistics.OnClick();
                }
                else if (key_state == KeyState.Up)
                {
                    MouseButtons.LeftUp();
                }
            }

            if (key == Key.RightMouseButton)
            {
                if (key_state == KeyState.Down)
                {
                    // Freeze cursor for a short period of time after mouse clicks to make double clicks esier.
                    MouseButtons.RightDown();
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.click_freeze_time_ms);
                    statistics.OnClick();
                }
                else if (key_state == KeyState.Up)
                {
                    MouseButtons.RightUp();
                }
            }

            if (key == Key.ShowCalibrationView && key_state == KeyState.Down)
            {
                CalibrationManager.Instance.IsDebugWindowEnabled = !CalibrationManager.Instance.IsDebugWindowEnabled;
            }

            return true;
        }

        public EyeTrackingMouse()
        {
            tobii_coordinates_provider = new TobiiCoordinatesProvider(OnNewCoordinates);
        }

        public void Dispose()
        {
            tobii_coordinates_provider.Dispose();
        }
    }
}
