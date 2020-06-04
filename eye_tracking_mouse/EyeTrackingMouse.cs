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
                smoothened_error_correction.Coordinates[0] + smoothened_error_correction.shift.X, 
                smoothened_error_correction.Coordinates[1] + smoothened_error_correction.shift.Y);
        }

        private void OnNewCoordinates(TobiiCoordinates coordinates)
        {
            lock (Helpers.locker)
            {
                if (mouse_state == MouseState.Idle && (DateTime.Now - idle_start_time).TotalSeconds > 60)
                    return;

                if (DateTime.Now > freeze_until)
                {
                    var current_coordinates = coordinates.ToCoordinates(Options.Instance.calibration_mode.additional_dimensions_configuration);

                    if (mouse_state == MouseState.Calibrating && 
                        Helpers.GetDistance(coordinates.gaze_point, calibration_start_gaze_point) > Options.Instance.reset_calibration_zone_size)
                    {
                        mouse_state = MouseState.Controlling;
                    }

                    if (mouse_state == MouseState.Calibrating)
                    {
                        // The only thing to update in calibration mode is gaze point.
                        smoothened_error_correction.Coordinates[0] = coordinates.gaze_point.X;
                        smoothened_error_correction.Coordinates[1] = coordinates.gaze_point.Y;
                        smoothened_error_correction = CoordinateSmoother.Smoothen(smoothened_error_correction);
                    } 
                    else
                    {
                        // The eye tracker provides shaky data that has to be smoothened before transforming to mouse cursor position.
                        // Another problem is that |CalibrationManager| may be a source of shaking too. That is why we shouldn't
                        // smoothen data too early.
                        //
                        // Keep in mind that smoothening decrease mouse cursor reaction time. So smoothening the data twice is a bad choice.
                        // Due to these reason we pass raw shaky data to |CalibrationManager.GetShift| and smoothen the data after that.
                        var shift = CalibrationManager.Instance.GetShift(current_coordinates);

                        // Now is the time to smoothen the data because all sources of shakiness are left behind.
                        smoothened_error_correction = CoordinateSmoother.Smoothen(
                            new EyeTrackerErrorCorrection(current_coordinates, shift));
                    }
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

                if (MouseButtons.RightPressed)
                    MouseButtons.RightUp();
                if (MouseButtons.LeftPressed)
                    MouseButtons.LeftUp();
            }
        }

        public void StartControlling()
        {
            lock (Helpers.locker) {
                idle_start_time = DateTime.Now;
                mouse_state = MouseState.Controlling;
                tobii_coordinates_provider.Restart();
            }
        }

        private void StartCalibration()
        {
            if (mouse_state != MouseState.Calibrating)
            {
                calibration_start_gaze_point =
                    new Point((int)smoothened_error_correction.Coordinates[0],
                              (int)smoothened_error_correction.Coordinates[1]);
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
                // Although we pass shaky (not smoothened) data to |CalibrationManager.GetShift| it is not an option here. 
                // Accuracy tests (June 2020) confirm that |CalibrationManager| works better with smoothened rather than shaky data.
                // Map of error corrections should be as accurate as possible.
                //
                // It's easy to make things worse changing this place. Even worse, it is hard to say that a commit is good or bad without 
                // blind tests. Blind tests are elaborate. Make sure you understand what is going on here before making radical changes;)
                CalibrationManager.Instance.AddShift(smoothened_error_correction.Coordinates, smoothened_error_correction.shift);
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
