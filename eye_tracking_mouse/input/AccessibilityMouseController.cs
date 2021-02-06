using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    // DefaultMouseController and AccessibilityMouseController are not DRY.
    // Fixing AccessibilityMouseController? Consider fixing DefaultMouseController.
    public class AccessibilityMouseController : InputProvider.IInputReceiver
    {
        private readonly InteractionHistoryEntry[] interaction_history = new InteractionHistoryEntry[3];

        AccessibilityHelperWindow helper_window = null;

        private struct InteractionHistoryEntry
        {
            public Key Key;
            public KeyState State;
            public DateTime Time;
        };

        private EyeTrackingMouse eye_tracking_mouse;

        private Point starting_cursor_position;

        public AccessibilityMouseController(EyeTrackingMouse eye_tracking_mouse)
        {

            helper_window = new AccessibilityHelperWindow();
            this.eye_tracking_mouse = eye_tracking_mouse;
        }

        bool InputProvider.IInputReceiver.OnKeyPressed(Key key, KeyState key_state, bool is_modifier, InputProvider input_provider)
        {
            lock (Helpers.locker)
            {
                // If you hold a key pressed for a second it will start to produce a sequence of rrrrrrrrrrepeated |KeyState.Down| events.
                // For some keys we don't want to handle such events and assume that a key stays pressed until |KeyState.Up| appears.
                bool is_repetition = interaction_history[0].Key == key &&
                    interaction_history[0].State == key_state &&
                    key_state == KeyState.Down;

                if (!is_repetition)
                {
                    interaction_history[2] = interaction_history[1];
                    interaction_history[1] = interaction_history[0];
                    interaction_history[0].Key = key;
                    interaction_history[0].State = key_state;
                    interaction_history[0].Time = DateTime.Now;
                }

                float speed_up = 1.0f;

                if (is_repetition)
                {
                    speed_up = 2.0f;
                }
                else if (key_state == KeyState.Down &&
                  interaction_history[1].Key == key &&
                  interaction_history[2].Key == key)
                {
                    if ((DateTime.Now - interaction_history[2].Time).TotalMilliseconds < Options.Instance.quadriple_speed_up_press_time_ms)
                        speed_up = 4.0f;
                    else if ((DateTime.Now - interaction_history[2].Time).TotalMilliseconds < Options.Instance.double_speedup_press_time_ms)
                        speed_up = 2.0f;
                }

                return this.OnKeyPressed(key, key_state, speed_up, is_repetition, is_modifier, input_provider);
            }
        }

        public bool OnKeyPressed(
            Key key,
            KeyState key_state,
            float speed_up,
            bool is_repetition,
            bool is_modifier,
            InputProvider input_provider)
        {
            if (key == Key.Modifier)
            {
                if (key_state == KeyState.Down)
                {
                    helper_window.Hide();
                    eye_tracking_mouse.StartControlling();
                    return true;
                }

                if (key_state == KeyState.Up)
                {

                    if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Controlling)
                    {
                        starting_cursor_position = System.Windows.Forms.Cursor.Position;
                        eye_tracking_mouse.StartCalibration(false);
                        helper_window.Show();
                        return true;
                    }

                    return false;
                }
            }

            if (eye_tracking_mouse.mouse_state != EyeTrackingMouse.MouseState.Calibrating)
            {
                if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Controlling)
                {
                    helper_window.Hide();
                    eye_tracking_mouse.StopControlling();
                    input_provider.SendModifierDown();
                }
                return false;
            }

            if (key == Key.StopCalibration)
            {
                if (key_state == KeyState.Up)
                {
                    helper_window.Hide();
                    eye_tracking_mouse.StopControlling();
                }
                return true;
            }

            if (key == Key.Unbound)
            {
                // Unbound key in Accessibility mode might mean that User has problems with pressing the right buttons.
                return true;
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
                    eye_tracking_mouse.AdjustCursorPosition(-calibration_step, 0);
                }
                if (key == Key.CalibrateRight)
                {
                    eye_tracking_mouse.AdjustCursorPosition(calibration_step, 0);
                }
                if (key == Key.CalibrateUp)
                {
                    eye_tracking_mouse.AdjustCursorPosition(0, -calibration_step);
                }
                if (key == Key.CalibrateDown)
                {
                    eye_tracking_mouse.AdjustCursorPosition(0, +calibration_step);
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
            if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Calibrating &&
                (key == Key.Accessibility_SaveCalibration))
            {
                if (key_state == KeyState.Up)
                {
                    helper_window.HideCalibration();
                    eye_tracking_mouse.SaveCalibration();
                } else
                {
                    helper_window.ShowCalibration(starting_cursor_position);
                }
            }

            if (key == Key.LeftMouseButton)
            {
                if (key_state == KeyState.Down)
                {
                    eye_tracking_mouse.LeftDown();
                }
                else if (key_state == KeyState.Up)
                {
                    eye_tracking_mouse.LeftUp();
                }
            }

            if (key == Key.RightMouseButton)
            {
                if (key_state == KeyState.Down)
                {
                    eye_tracking_mouse.RightDown();
                }
                else if (key_state == KeyState.Up)
                {
                    eye_tracking_mouse.RightUp();
                }
            }

            if (key == Key.ShowCalibrationView && key_state == KeyState.Down)
            {
                CalibrationManager.Instance.IsDebugWindowEnabled = !CalibrationManager.Instance.IsDebugWindowEnabled;
            }

            return true;
        }

        public void Dispose()
        {
            helper_window.Close();
        }
    }
}
