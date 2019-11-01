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

        public static double GetDistance(Point a, Point b)
        {
            return Math.Pow(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2), 0.5);
        }

        public static Icon AppIcon { get { return Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath); } }

        public static readonly NotifyIcon tray_icon = new NotifyIcon
        {
            Icon = AppIcon,
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

        private static readonly SortedSet<Interceptor.Keys> e0_keys = new SortedSet<Interceptor.Keys> { 
            Interceptor.Keys.WindowsKey, 
            Interceptor.Keys.Delete,
            Interceptor.Keys.Left,
            Interceptor.Keys.Right,
            Interceptor.Keys.Up,
            Interceptor.Keys.Down,
            Interceptor.Keys.ForwardSlashQuestionMark,
            Interceptor.Keys.PrintScreen,
        };

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
