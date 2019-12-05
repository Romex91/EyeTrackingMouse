using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using IWshRuntimeLibrary;

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

        public static bool IsModifier(Interceptor.Keys key)
        {
            return modifier_keys_intrerceptor.Contains(key);
        }

        private static readonly SortedSet<Interceptor.Keys> modifier_keys_intrerceptor = new SortedSet<Interceptor.Keys> {
            Interceptor.Keys.WindowsKey,
            Interceptor.Keys.LeftShift,
            Interceptor.Keys.RightShift,
            Interceptor.Keys.Control,
            Interceptor.Keys.RightAlt,
        };

        public static bool IsModifier(System.Windows.Forms.Keys key)
        {
            return modifier_keys.Contains(key);
        }

        private static readonly SortedSet<System.Windows.Forms.Keys> modifier_keys = new SortedSet<System.Windows.Forms.Keys> {
            System.Windows.Forms.Keys.LWin,
            System.Windows.Forms.Keys.RWin,

            System.Windows.Forms.Keys.Control,
            System.Windows.Forms.Keys.RControlKey,
            System.Windows.Forms.Keys.LControlKey,

            System.Windows.Forms.Keys.Shift,
            System.Windows.Forms.Keys.LShiftKey,
            System.Windows.Forms.Keys.RShiftKey,

            System.Windows.Forms.Keys.LMenu,
            System.Windows.Forms.Keys.RMenu,
            System.Windows.Forms.Keys.Menu,

            System.Windows.Forms.Keys.Alt,
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

        public static string GetUserDataFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), application_name + " User Data");
        }

        public static string GetAppFolder()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), application_name);
        }


        private static string StartMenuShortcatLocation
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs", application_name, application_name + ".lnk");
            }
        }

        private static string DesctopShortcatLocation
        { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), application_name + ".lnk"); } }


        public static void CreateShortcuts()
        {
            string pathToExe = Path.Combine(GetAppFolder(), application_name + ".exe");

            if (!Directory.Exists(Directory.GetParent(StartMenuShortcatLocation).FullName))
                Directory.CreateDirectory(Directory.GetParent(StartMenuShortcatLocation).FullName);

            WshShell shell = new WshShell();
            IWshShortcut start_menu_shortcut = (IWshShortcut)shell.CreateShortcut(StartMenuShortcatLocation);
            start_menu_shortcut.TargetPath = pathToExe;
            start_menu_shortcut.Save();


            IWshShortcut desktop_shotrcut = (IWshShortcut)shell.CreateShortcut(DesctopShortcatLocation);
            desktop_shotrcut.TargetPath = pathToExe;
            desktop_shotrcut.Save();
        }

        public static void RemoveShortcuts()
        {
            System.IO.File.Delete(DesctopShortcatLocation);
            System.IO.Directory.Delete(Directory.GetParent(StartMenuShortcatLocation).FullName, true);
        }
    }
}
