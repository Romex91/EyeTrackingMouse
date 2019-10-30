using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eye_tracking_mouse
{
    class Helpers
    {
        // The only synchronisation object. The thread model is simple.
        // Tobii and Interceptor has their own threads. These threads comunicate with the Application via callbacks |EyeTrackingMouse.OnGazePoint| and |InputManager.OnKeyPressed|. 
        // Each such a callback has to run in a critical section |lock(Helpers.locker)|.
        // WPF also has to use |lock (Helpers.locker)| before accessing Options and KeyBindings.
        public static readonly object locker = new object();

        public static readonly string application_name = "EyeTrackingMouse";

        public static readonly NotifyIcon tray_icon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
            Visible = true,
            BalloonTipTitle = application_name,
        };

        public static readonly SortedSet<Interceptor.Keys> modifier_keys = new SortedSet<Interceptor.Keys> {
            Interceptor.Keys.WindowsKey,
            Interceptor.Keys.LeftShift,
            Interceptor.Keys.RightShift,
            Interceptor.Keys.Control,
            Interceptor.Keys.RightAlt,
        };

        // Interceptor.KeyState is a mess. Different Keys produce different KeyState when pressed and released.
        // TODO: Figure out full list of e0 keys;
        private static readonly SortedSet<Interceptor.Keys> e0_keys = new SortedSet<Interceptor.Keys> { Interceptor.Keys.WindowsKey, Interceptor.Keys.Delete};

        public static Interceptor.KeyState GetDownKeyState(Interceptor.Keys key)
        {
            if (e0_keys.Contains(key))
                return Interceptor.KeyState.E0;
            return Interceptor.KeyState.Down;
        }
        public static Interceptor.KeyState GetUpKeyState(Interceptor.Keys key)
        {
            if (e0_keys.Contains(key))
                return Interceptor.KeyState.E0 | Interceptor.KeyState.Up;
            return Interceptor.KeyState.Up;
        }

        public static void ShowBaloonNotification(String text)
        {
            tray_icon.BalloonTipText = text;
            tray_icon.ShowBalloonTip(30000);
        }

        public static string GetLocalFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), application_name);
        }
    }
}
