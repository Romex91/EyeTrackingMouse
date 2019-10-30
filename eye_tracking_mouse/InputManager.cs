using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eye_tracking_mouse
{
    struct InteractionHistoryEntry
    {
        public Interceptor.Keys Key;
        public Interceptor.KeyState State;
        public DateTime Time;
    };

    class InputManager
    {
        private readonly Interceptor.Input input = new Interceptor.Input();
        private readonly EyeTrackingMouse eye_tracking_mouse;
        private readonly Options options;

        // For hardcoded stop-word.
        private bool is_win_pressed = false;
        private readonly InteractionHistoryEntry[] interaction_history = new InteractionHistoryEntry[3];

        public enum KeyState
        {
            Up,
            Down,
        };

        public void OnKeyPressed(object sender, Interceptor.KeyPressedEventArgs e)
        {
            lock (Helpers.locker)
            {
                // Console.WriteLine(e.Key);
                // Console.WriteLine(e.State);

                e.Handled = true;

                KeyState key_state;
                if (e.State == Helpers.GetDownKeyState(e.Key))
                {
                    key_state = KeyState.Down;
                }
                else if (e.State == Helpers.GetUpKeyState(e.Key))
                {
                    key_state = KeyState.Up;
                } else
                {
                    e.Handled = false;
                    return;
                }

                // Hardcoded stop-word is Win+Del.
                if (e.Key == Interceptor.Keys.WindowsKey)
                {
                    if (key_state == KeyState.Down)
                        is_win_pressed = true;
                    else if (key_state == KeyState.Up)
                        is_win_pressed = false;
                }

                if (e.Key == Interceptor.Keys.Delete &&
                    key_state == KeyState.Down && is_win_pressed)
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
                    key_state == KeyState.Down)
                {
                    if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Idle)
                        e.Handled = false;
                    return;
                }

                interaction_history[2] = interaction_history[1];
                interaction_history[1] = interaction_history[0];
                interaction_history[0].Key = e.Key;
                interaction_history[0].State = e.State;
                interaction_history[0].Time = DateTime.Now;

                bool is_double_press =
                    key_state == KeyState.Down &&
                    interaction_history[1].Key == e.Key &&
                    interaction_history[2].Key == e.Key &&
                    (DateTime.Now - interaction_history[2].Time).TotalMilliseconds < options.double_click_duration_ms;

                bool is_short_press =
                    key_state == KeyState.Up &&
                    interaction_history[1].Key == e.Key &&
                    (DateTime.Now - interaction_history[1].Time).TotalMilliseconds < options.short_click_duration_ms;

                // The application grabs control over cursor when modifier is pressed.
                if (e.Key == options.key_bindings.modifier)
                {
                    if (key_state == InputManager.KeyState.Down)
                    {
                        eye_tracking_mouse.StartControlling();
                    }
                    else if (key_state == InputManager.KeyState.Up)
                    {
                        if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Idle)
                        {
                            e.Handled = false;
                        }
                        else if (is_short_press)
                        {
                            input.SendKey(e.Key, Helpers.GetDownKeyState(e.Key));
                            Thread.Sleep(options.win_press_delay_ms);
                            input.SendKey(e.Key, Helpers.GetUpKeyState(e.Key));
                        }

                        eye_tracking_mouse.StopControlling();
                    }
                    return;
                }


                if (eye_tracking_mouse.mouse_state == EyeTrackingMouse.MouseState.Idle)
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
                    if (eye_tracking_mouse.mouse_state != EyeTrackingMouse.MouseState.Idle  && !Helpers.modifier_keys.Contains(e.Key))
                    {
                        eye_tracking_mouse.StopControlling();
                        input.SendKey(options.key_bindings.modifier, Helpers.GetDownKeyState(options.key_bindings.modifier));
                    }
                    e.Handled = false;
                    return;
                }


                eye_tracking_mouse.OnKeyPressed(e.Key, key_state, is_double_press);
            }
        }

        public InputManager(EyeTrackingMouse eye_tracking_mouse, Options options)
        {
            this.eye_tracking_mouse = eye_tracking_mouse;
            this.options = options;

            input.KeyboardFilterMode = Interceptor.KeyboardFilterMode.All;
            if (!input.Load())
            {
                Helpers.ShowBaloonNotification("Failed loading interception driver. Try reinstalling EyeTrackingMouse.");
                System.Windows.Forms.Application.Exit();
            }

            input.OnKeyPressed += OnKeyPressed;
        }
    }
}
