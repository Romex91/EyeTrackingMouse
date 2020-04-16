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
    public static partial class Helpers
    {
        // The only synchronisation object. The thread model is simple.
        // Tobii and Interceptor has their own threads. These threads comunicate with the Application via callbacks |EyeTrackingMouse.OnGazePoint| and |InputManager.OnKeyPressed|. 
        // Each such a callback has to run in a critical section |lock(Helpers.locker)|.
        // WPF also has to use |lock (Helpers.locker)| before accessing Options and KeyBindings.
        public static readonly object locker = new object();

        public static readonly string application_name = "EyeTrackingMouse";

        public static float GetDistance(Point a, Point b)
        {
            return (float) Math.Pow(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2), 0.5);
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

        public static string GetKeyString(Interceptor.Keys key, bool is_e0)
        {
            string retval = "";
            if (key == Interceptor.Keys.RightAlt || key == Interceptor.Keys.Control)
            {
                retval = is_e0 ? "Right" : "Left";
            }

            if (key == Interceptor.Keys.RightAlt)
            {
                retval += "Alt";
            }
            else
            {
                retval += key.ToString();
            }

            return retval;
        }

        public static string GetModifierString()
        {
            return Helpers.GetKeyString(Options.Instance.key_bindings[Key.Modifier], Options.Instance.key_bindings.is_modifier_e0);
        }

        public static void ShowBaloonNotification(String text)
        {
            tray_icon.BalloonTipText = text;
            tray_icon.ShowBalloonTip(30000);
        }

        public static string UserDataFolder
        {
            get
            {
                return Path.Combine(AppFolder, " User Data");
            }

        }

        public static string AppFolder
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), application_name);
            }
        }

        public static string ExePath
        {
            get
            {
                return Path.Combine(AppFolder, application_name + ".exe");
            }
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
            if (!Directory.Exists(Directory.GetParent(StartMenuShortcatLocation).FullName))
                Directory.CreateDirectory(Directory.GetParent(StartMenuShortcatLocation).FullName);

            WshShell shell = new WshShell();
            IWshShortcut start_menu_shortcut = (IWshShortcut)shell.CreateShortcut(StartMenuShortcatLocation);
            start_menu_shortcut.TargetPath = ExePath;
            start_menu_shortcut.Save();


            IWshShortcut desktop_shotrcut = (IWshShortcut)shell.CreateShortcut(DesctopShortcatLocation);
            desktop_shotrcut.TargetPath = ExePath;
            desktop_shotrcut.Save();
        }

        // https://github.com/Squirrel/Squirrel.Windows/issues/197
        public static void DeleteAppFiles()
        {
            var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine(":loop");
            sb.AppendLine("tasklist | find \"" + pid + "\" >nul");
            sb.AppendLine("if not errorlevel 1 (");
            sb.AppendLine("    timeout /t 2 >nul");
            sb.AppendLine("goto :loop");
            sb.AppendLine(")");
            sb.AppendLine("rmdir /s /q " + Helpers.AppFolder);
            sb.AppendLine("call :deleteSelf&exit /b");
            sb.AppendLine(":deleteSelf");
            sb.AppendLine("start /b \"\" cmd /c del \"%~f0\"&exit /b");

            var tempPath = Path.GetTempPath();
            var tempSavePath = Path.Combine(tempPath, "squirrel_cleaner.bat");

            System.IO.File.WriteAllText(tempSavePath, sb.ToString(), System.Text.Encoding.ASCII);

            var p = new System.Diagnostics.Process();
            p.StartInfo.WorkingDirectory = tempPath;
            p.StartInfo.FileName = tempSavePath;
            p.StartInfo.CreateNoWindow = true;

            p.Start();
            p.Dispose();
        }

        public static void RemoveShortcuts()
        {
            System.IO.File.Delete(DesctopShortcatLocation);
            System.IO.Directory.Delete(Directory.GetParent(StartMenuShortcatLocation).FullName, true);
        }
    }
}
