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
    public class InputManager
    {
        private readonly Interceptor.Input input = new Interceptor.Input();
        private readonly EyeTrackingMouse eye_tracking_mouse;

        // For hardcoded stop-word.
        private bool is_win_pressed = false;
        private readonly InteractionHistoryEntry[] interaction_history = new InteractionHistoryEntry[3];

        private Action<ReadKeyResult> read_key_callback;

        public enum KeyState
        {
            Up,
            Down,
        };

        private struct InteractionHistoryEntry
        {
            public Key Key;
            public KeyState State;
            public DateTime Time;
        };

        public void Stop()
        {
            input.Unload();
        }

        private void SendModifierDown()
        {
            input.SendKey(Options.Instance.key_bindings[Key.Modifier], Options.Instance.key_bindings.is_modifier_e0 ? Interceptor.KeyState.E0 : Interceptor.KeyState.Down);
            Thread.Sleep(Options.Instance.win_press_delay_ms);
        }

        private bool OnKeyPressed(Key key, KeyState key_state, bool is_modifier)
        {
                // If you hold a key pressed for a second it will start to produce a sequence of rrrrrrrrrrepeated |KeyState.Down| events.
                // For most keys we don't want to handle such events and assume that a key stays pressed until |KeyState.Up| appears.
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

                bool is_double_press =
                    key_state == KeyState.Down &&
                    interaction_history[1].Key == key &&
                    interaction_history[2].Key == key &&
                    (DateTime.Now - interaction_history[2].Time).TotalMilliseconds < Options.Instance.double_click_duration_ms;

                bool is_short_press =
                    key_state == KeyState.Up &&
                    interaction_history[1].Key == key &&
                    (DateTime.Now - interaction_history[1].Time).TotalMilliseconds < Options.Instance.short_click_duration_ms;

                return eye_tracking_mouse.OnKeyPressed(key, key_state, is_double_press, is_short_press, is_repetition, is_modifier, SendModifierDown);
           
        }

        public struct ReadKeyResult
        {
            public Interceptor.Keys key;
            public bool is_e0_key;
        }
        public void ReadKeyAsync(Action<ReadKeyResult> callback)
        {
            lock (Helpers.locker)
            {
                read_key_callback = callback;
            }
        }

        public void OnKeyPressed(object sender, Interceptor.KeyPressedEventArgs e)
        {
            // Console.WriteLine(e.Key);
            // Console.WriteLine(e.State);

            lock (Helpers.locker)
            {
                e.Handled = true;

                // Interceptor.KeyState is a mess. Different Keys produce different KeyState when pressed and released.
                bool is_e0_key = (e.State & Interceptor.KeyState.E0) != 0;
                KeyState key_state;
                if (e.State == Interceptor.KeyState.E0 || e.State == Interceptor.KeyState.Down)
                {
                    key_state = KeyState.Down;
                }
                else if ((e.State & Interceptor.KeyState.Up) != 0)
                {
                    key_state = KeyState.Up;
                }
                else
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
                    Environment.Exit(0);
                    return;
                }


                var key_bindings = Options.Instance.key_bindings;
                Key key = Key.Unbound;
                if (key_bindings.bindings.ContainsValue(e.Key))
                {
                    key = key_bindings.bindings.First(pair =>
                    {
                        return pair.Value == e.Key;
                    }).Key;
                }

                if (key_state == KeyState.Down && read_key_callback != null)
                {
                    read_key_callback(new ReadKeyResult { is_e0_key = is_e0_key, key = e.Key });
                    read_key_callback = null;
                    e.Handled = true;
                    return;
                }
                e.Handled = OnKeyPressed(key, key_state, Helpers.modifier_keys.Contains(e.Key));
            }
        }

        public InputManager(EyeTrackingMouse eye_tracking_mouse)
        {
            this.eye_tracking_mouse = eye_tracking_mouse;

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
