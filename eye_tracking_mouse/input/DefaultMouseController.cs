using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    // DefaultMouseController and AccessibilityMouseController are not DRY.
    // Fixing DefaultMouseController? Consider fixing AccessibilityMouseController.
    public class DefaultMouseController : InputProvider.IInputReceiver
    {
        private readonly InteractionHistoryEntry[] interaction_history = new InteractionHistoryEntry[3];
        private bool is_waiting_for_second_modifier_press = false;
        private bool always_on = false;
        private DateTime always_on_disabled_time = DateTime.Now;

        private struct InteractionHistoryEntry
        {
            public Key Key;
            public KeyState State;
            public DateTime Time;
        };

        private EyeTrackingMouse eye_tracking_mouse;

        public DefaultMouseController(EyeTrackingMouse eye_tracking_mouse)
        {
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

                // Single and double modifier presses have different functions
                // Single press goes to OS (this allows using WINDOWS MENU)
                // Double press enables |always_on| mode.
                if (is_waiting_for_second_modifier_press)
                {
                    is_waiting_for_second_modifier_press = false;
                    if (key == Key.Modifier && key_state == KeyState.Down &&
                        (DateTime.Now - interaction_history[1].Time).TotalMilliseconds < 300)
                    {
                        always_on = true;
                        eye_tracking_mouse.StartControlling();
                        return true;
                    }
                }

                // IF user pressed and released modifier key without pressing other buttons in between...
                if (key == Key.Modifier &&
                    key_state == KeyState.Up &&
                    interaction_history[1].Key == key &&
                    !always_on)
                {
                    double press_duration_ms = (DateTime.Now - interaction_history[1].Time).TotalMilliseconds;

                    // THEN it might be a beginning of a double press...
                    if (press_duration_ms < 300)
                    {
                        is_waiting_for_second_modifier_press = true;
                    }

                    // OR it might be a single modifier press that should go to OS.
                    if (press_duration_ms < Options.Instance.modifier_short_press_duration_ms &&
                        (DateTime.Now - always_on_disabled_time).TotalMilliseconds > 300)
                    {
                        is_waiting_for_second_modifier_press = true;
                        App.Current.Dispatcher.InvokeAsync((async () =>
                        {
                            await Task.Delay(300);
                            lock (Helpers.locker)
                            {
                                if (!is_waiting_for_second_modifier_press)
                                    return;
                                is_waiting_for_second_modifier_press = false;
                                input_provider.SendModifierDown();
                                input_provider.SendModifierUp();
                            }
                        }));
                    }
                }

                if (always_on)
                {
                    if (key == Key.Modifier && key_state == KeyState.Up)
                        return true;

                    if (key == Key.Unbound ||
                        (key == Key.Modifier && key_state == KeyState.Down))
                    {
                        always_on = false;
                        always_on_disabled_time = DateTime.Now;
                        eye_tracking_mouse.StopControlling();
                    }
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
            // The application grabs control over cursor when modifier is pressed.
            if (key == Key.Modifier)
            {
                if (key_state == KeyState.Down)
                {
                    eye_tracking_mouse.StartControlling();
                    return true;
                }

                if (key_state == KeyState.Up)
                {
                    if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Idle)
                    {
                        return false;
                    }

                    eye_tracking_mouse.StopControlling();
                    return true;
                }
            }

            if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Idle)
            {
                return false;
            }

            if (key == Key.Unbound || key == Key.StopCalibration)
            {
                // The application intercepts modifier key presses. We do not want to lose modifier when handling unbound keys.
                // We stop controlling cursor when facing the first unbound key and send modifier keystroke to OS before handling pressed key.
                // This way key combinations like 'Win+E' remain available.
                if (!is_modifier)
                {
                    input_provider.SendModifierDown();
                    eye_tracking_mouse.StopControlling();
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
                    eye_tracking_mouse.StartCalibration(true);
                    eye_tracking_mouse.AdjustCursorPosition(-calibration_step, 0);
                }
                if (key == Key.CalibrateRight)
                {
                    eye_tracking_mouse.StartCalibration(true);
                    eye_tracking_mouse.AdjustCursorPosition(calibration_step, 0);
                }
                if (key == Key.CalibrateUp)
                {
                    eye_tracking_mouse.StartCalibration(true);
                    eye_tracking_mouse.AdjustCursorPosition(0, -calibration_step);
                }
                if (key == Key.CalibrateDown)
                {
                    eye_tracking_mouse.StartCalibration(true);
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
                (key == Key.LeftMouseButton || key == Key.RightMouseButton))
            {
                eye_tracking_mouse.ApplyCalibration();
                eye_tracking_mouse.StartControlling();
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
        }
    }
}
