using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.IO;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private static readonly Options options = Options.LoadFromFile();
        private static EyeTrackingMouse eye_tracking_mouse;
        private static InputManager input_manager;
        // TODO: prevent multiple windows.
        private static void OpenSettings(object sender, EventArgs e)
        {
            MessageBox.Show("Settings", Helpers.application_name);
        }
        private static void OpenAbout(object sender, EventArgs e)
        {
            MessageBox.Show("About", Helpers.application_name);
        }
        private static void Shutdown(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        [STAThread]
        public static void Main()
        {
            var application = new App();
            application.InitializeComponent();

            if (!Directory.Exists(Helpers.GetLocalFolder()))
            {
                Directory.CreateDirectory(Helpers.GetLocalFolder());
            }

            // Tray icon initialization
            {
                System.Windows.Forms.ContextMenuStrip context_menu_strip = new System.Windows.Forms.ContextMenuStrip { Visible = true };

                context_menu_strip.SuspendLayout();

                System.Windows.Forms.ToolStripMenuItem settings = new System.Windows.Forms.ToolStripMenuItem { Text = "Settings", Visible = true };
                settings.Click += OpenSettings;

                System.Windows.Forms.ToolStripMenuItem about = new System.Windows.Forms.ToolStripMenuItem { Text = "About " + Helpers.application_name, Visible = true };
                about.Click += OpenAbout;

                System.Windows.Forms.ToolStripMenuItem exit = new System.Windows.Forms.ToolStripMenuItem { Text = "Exit", Visible = true };
                exit.Click += Shutdown;

                context_menu_strip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { settings, about, exit });

                context_menu_strip.ResumeLayout(false);
                Helpers.tray_icon.ContextMenuStrip = context_menu_strip;
            }

            eye_tracking_mouse = new EyeTrackingMouse(options);
            input_manager = new InputManager(eye_tracking_mouse, options);


            application.Run();

            eye_tracking_mouse.Dispose();
            Helpers.tray_icon.Visible = false;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
        }
    }
}
