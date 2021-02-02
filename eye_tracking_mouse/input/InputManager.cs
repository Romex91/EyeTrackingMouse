using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace eye_tracking_mouse
{
    public enum KeyState
    {
        Up,
        Down,
    };

    // Implementations should intercept key presses.
    public abstract class InputProvider
    {
        public interface IInputReceiver
        {
            bool OnKeyPressed(Key key, KeyState key_state, bool is_modifier);
        }

        protected IInputReceiver receiver;

        public InputProvider(IInputReceiver receiver)
        {
            this.receiver = receiver;
        }

        public struct ReadKeyResult
        {
            public Interceptor.Keys key;
            public bool is_e0_key;
        }
        public abstract void ReadKey(Action<ReadKeyResult> callback);

        public abstract void Load();
        public abstract void Unload();

        public abstract bool IsLoaded { get; }

        public abstract void SendModifierDown();
        public abstract void SendModifierUp();
    }


    public class InputManager : InputProvider.IInputReceiver
    {
        private readonly AccessibilityHelperWindow accessibility_helper_window = new AccessibilityHelperWindow();

        private readonly EyeTrackingMouse eye_tracking_mouse;

        private readonly InteractionHistoryEntry[] interaction_history = new InteractionHistoryEntry[3];

        private InputProvider input_provider;

        private bool is_waiting_for_second_modifier_press = false;
        private bool always_on = false;
        private DateTime always_on_disabled_time = DateTime.Now;

        private struct InteractionHistoryEntry
        {
            public Key Key;
            public KeyState State;
            public DateTime Time;
        };

        public void Stop()
        {
            lock (Helpers.locker)
            {
                eye_tracking_mouse.StopControlling();
                if (input_provider != null && input_provider.IsLoaded)
                    input_provider.Unload();
                input_provider = null;
            }
        }

        bool InputProvider.IInputReceiver.OnKeyPressed(Key key, KeyState key_state, bool is_modifier)
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

                return eye_tracking_mouse.OnKeyPressed(key, key_state, speed_up, is_repetition, is_modifier, input_provider);
            }
        }

        public void ReadKey(Action<InputProvider.ReadKeyResult> callback)
        {
            lock (Helpers.locker)
            {
                input_provider.ReadKey(callback);
            }
        }


        // Enables keys interception with selected |interception_method|. Backs off to WinAPI if failed loading interception driver.
        public bool UpdateInterceptionMethod()
        {
            lock (Helpers.locker)
            {
                Stop();

                if (Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.OblitaDriver)
                {
                    input_provider = new OblitaInterceptionInputProvider(this);
                    input_provider.Load();

                    if (input_provider.IsLoaded)
                        return true;
                }

                input_provider = new WinApiInputProvider(this);
                input_provider.Load();

                return Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.WinApi;
            }
        }

        public bool IsDriverLoaded()
        {
            lock (Helpers.locker)
            {
                return input_provider.IsLoaded && input_provider.GetType() == typeof(OblitaInterceptionInputProvider);
            }
        }

        public InputManager(EyeTrackingMouse eye_tracking_mouse)
        {
            this.eye_tracking_mouse = eye_tracking_mouse;
            accessibility_helper_window.Show();
            if (!UpdateInterceptionMethod())
            {
                lock (Helpers.locker)
                {
                    Options.Instance.key_bindings.interception_method = KeyBindings.InterceptionMethod.WinApi;
                    Options.Instance.SaveToFile(Options.Filepath);
                }
            }
        }
    }
}
