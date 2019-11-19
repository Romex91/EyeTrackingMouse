using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.IO;
using System.Windows.Controls;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static EyeTrackingMouse eye_tracking_mouse;
        private static InputManager input_manager;

        private static App application;
        private static CalibrationWindow calibration_window = null;

        private static Settings settings_window;

        private static void OpenSettings(object sender, EventArgs e)
        {
            if (settings_window == null || !settings_window.IsLoaded)
            {
                settings_window = new Settings(input_manager);
                settings_window.Show();
            }

            if (settings_window.IsEnabled)
                settings_window.TabControl.SelectedIndex = 0;
        }
        private static void OpenAbout(object sender, EventArgs e)
        {
            if (settings_window == null || !settings_window.IsLoaded)
            {
                settings_window = new Settings(input_manager);
                settings_window.Show();
            }

            if (settings_window.IsEnabled)
                settings_window.TabControl.SelectedIndex = 2;
        }
        private static void Shutdown(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "This will prevent you from controlling mouse with your eyes.\nSure you want to quit?",
                    Helpers.application_name,
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                System.Windows.Application.Current.Shutdown();
                input_manager.Stop();
                eye_tracking_mouse.StopControlling();
                Helpers.tray_icon.Visible = false;
                ShiftsStorage.Instance.SaveToFileAsync();
                ShiftsStorage.Instance.save_to_file_task.Wait();
            }
        }

        private static void ResetCalibration(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "This will reset your calibration.\nSure?",
                    Helpers.application_name,
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                lock (Helpers.locker)
                {
                    ShiftsStorage.Instance.Reset();
                }
            }
        }

        public static void ToggleCalibrationWindow()
        {
            application.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (calibration_window == null)
                {
                    calibration_window = new CalibrationWindow();
                    calibration_window.Show();
                }
                else
                {
                    calibration_window.Close();
                    calibration_window = null;
                }
            }));
        }

        [STAThread]
        public static void Main()
        {
            application = new App();
            application.InitializeComponent();
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            if (!Directory.Exists(Helpers.GetLocalFolder()))
            {
                Directory.CreateDirectory(Helpers.GetLocalFolder());
            }

            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.RealTime;

            // Tray icon initialization
            {
                System.Windows.Forms.ContextMenuStrip context_menu_strip = new System.Windows.Forms.ContextMenuStrip { Visible = true };

                context_menu_strip.SuspendLayout();

                System.Windows.Forms.ToolStripMenuItem settings = new System.Windows.Forms.ToolStripMenuItem { Text = "Settings", Visible = true };
                settings.Click += OpenSettings;

                System.Windows.Forms.ToolStripMenuItem reset_calibration = new System.Windows.Forms.ToolStripMenuItem { Text = "Reset calibration", Visible = true };
                reset_calibration.Click += ResetCalibration;

                System.Windows.Forms.ToolStripMenuItem about = new System.Windows.Forms.ToolStripMenuItem { Text = "About " + Helpers.application_name, Visible = true };
                about.Click += OpenAbout;

                System.Windows.Forms.ToolStripMenuItem exit = new System.Windows.Forms.ToolStripMenuItem { Text = "Exit", Visible = true };
                exit.Click += Shutdown;

                context_menu_strip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { settings, reset_calibration, about, exit });

                context_menu_strip.ResumeLayout(false);
                Helpers.tray_icon.ContextMenuStrip = context_menu_strip;
                Helpers.tray_icon.MouseClick += Tray_icon_Click;
            }

            eye_tracking_mouse = new EyeTrackingMouse();
            input_manager = new InputManager(eye_tracking_mouse);


            application.Run();

            eye_tracking_mouse.Dispose();
            Helpers.tray_icon.Visible = false;
        }

        private static void Tray_icon_Click(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                OpenAbout(sender, e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

        }
    }
}
