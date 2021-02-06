using System;


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
        public interface IInputReceiver: IDisposable
        {
            bool OnKeyPressed(Key key, KeyState key_state, bool is_modifier, InputProvider provider);
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


    public class InputManager
    {
        private readonly EyeTrackingMouse eye_tracking_mouse;
        private InputProvider input_provider;
        InputProvider.IInputReceiver mouse_controller;
        public void Stop()
        {
            lock (Helpers.locker)
            {
                eye_tracking_mouse.StopControlling();
                if (input_provider != null && input_provider.IsLoaded)
                    input_provider.Unload();
                input_provider = null;

                if (mouse_controller != null)
                    mouse_controller.Dispose();
                mouse_controller = null;
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
        public bool Reset()
        {
            lock (Helpers.locker)
            {
                Stop();

                if (Options.Instance.accessibility_mode)
                    mouse_controller = new AccessibilityMouseController(eye_tracking_mouse);
                else
                    mouse_controller = new DefaultMouseController(eye_tracking_mouse);

                if (Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.OblitaDriver)
                {
                    input_provider = new OblitaInterceptionInputProvider(mouse_controller);
                    input_provider.Load();

                    if (input_provider.IsLoaded)
                        return true;
                }

                input_provider = new WinApiInputProvider(mouse_controller);
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
            if (!Reset())
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
