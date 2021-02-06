using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    class OblitaInterceptionInputProvider : InputProvider
    {
        // For hardcoded stop-word.
        private bool is_win_pressed = false;

        private readonly Interceptor.Input driver_input = new Interceptor.Input();

        private Action<ReadKeyResult> read_key_callback;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern short GetKeyState(int key);
        private void UncheckCapsLock()
        {
            if ((GetKeyState((int)System.Windows.Forms.Keys.CapsLock) & 1) == 1)
            {
                driver_input.SendKey(Interceptor.Keys.CapsLock, Interceptor.KeyState.Down);
                driver_input.SendKey(Interceptor.Keys.CapsLock, Interceptor.KeyState.Up);
            }
        }

        public void OnKeyPressed(object sender, Interceptor.KeyPressedEventArgs e)
        {
            // Console.WriteLine(e.Key);
            // Console.WriteLine(e.State);

            lock (Helpers.locker)
            {
                e.Handled = true;

                // Make sure capslock is always disabled when it is used as a modifier.
                if (Options.Instance.key_bindings[Key.Modifier] == Interceptor.Keys.CapsLock)
                {
                    UncheckCapsLock();
                }

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

                if (key_state == KeyState.Down && read_key_callback != null)
                {
                    read_key_callback(new ReadKeyResult { is_e0_key = is_e0_key, key = e.Key });
                    read_key_callback = null;
                    e.Handled = true;
                    return;
                }

                // Convert |Interceptor.Keys| to |eye_tracking_mouse.Key|
                var key_bindings = Options.Instance.key_bindings;
                Key key = Key.Unbound;
                if (key_bindings.bindings.ContainsValue(e.Key))
                {
                    key = key_bindings.bindings.First(pair =>
                    {
                        return pair.Value == e.Key;
                    }).Key;
                }

                if (key == Key.Modifier && key_bindings.is_modifier_e0 != is_e0_key)
                    key = Key.Unbound;

                e.Handled = receiver.OnKeyPressed(key, key_state, Helpers.IsModifier(e.Key), this);
            }
        }

        public OblitaInterceptionInputProvider(IInputReceiver receiver) : base(receiver) { }

        public override bool IsLoaded { get { return driver_input.IsLoaded; } }

        public override void Load()
        {
            driver_input.OnKeyPressed += OnKeyPressed;
            driver_input.KeyboardFilterMode = Interceptor.KeyboardFilterMode.All;
            driver_input.Load();
        }

        public override void SendModifierDown()
        {
            var key = Options.Instance.key_bindings[Key.Modifier];
            // Make sure capslock is always disabled when it is used as a modifier.
            if (key == Interceptor.Keys.CapsLock || key == null)
            {
                return;
            }
            driver_input.SendKey((Interceptor.Keys)key, Options.Instance.key_bindings.is_modifier_e0 ? Interceptor.KeyState.E0 : Interceptor.KeyState.Down);
            Thread.Sleep(10);
        }

        public override void SendModifierUp()
        {
            var key = Options.Instance.key_bindings[Key.Modifier];
            if (key == null)
            {
                return;
            }

            driver_input.SendKey((Interceptor.Keys)key, Options.Instance.key_bindings.is_modifier_e0 ? Interceptor.KeyState.E0 | Interceptor.KeyState.Up : Interceptor.KeyState.Up);
            Thread.Sleep(10);
        }

        public override void Unload()
        {
            if (driver_input.IsLoaded)
            {
                driver_input.Unload();
                driver_input.OnKeyPressed -= OnKeyPressed;
            }
        }

        public override void ReadKey(Action<ReadKeyResult> callback)
        {
            read_key_callback = callback;
        }
    }
}
