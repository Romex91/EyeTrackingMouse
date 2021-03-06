﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.IO;
using System.Windows.Controls;
using Squirrel;
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

            settings_window.Activate();
        }

        private static void Shutdown(object sender, EventArgs e)
        {
            input_manager.Stop();
            eye_tracking_mouse.StopControlling();
            Helpers.tray_icon.Visible = false;

            FilesSavingQueue.FlushSynchroniously();
            System.Windows.Application.Current.Shutdown();
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
                    CalibrationManager.Instance.Reset();
                }
            }
        }

        [STAThread]
        public static void Main()
        {
            application = new App();
            application.InitializeComponent();
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
            ToolTipService.InitialShowDelayProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(0));

            if (!Directory.Exists(Helpers.UserDataFolder))
            {
                Directory.CreateDirectory(Helpers.UserDataFolder);
            }

            // Tray icon initialization
            {
                System.Windows.Forms.ContextMenuStrip context_menu_strip = new System.Windows.Forms.ContextMenuStrip { Visible = true };

                context_menu_strip.SuspendLayout();

                System.Windows.Forms.ToolStripMenuItem settings = new System.Windows.Forms.ToolStripMenuItem { Text = "Settings", Visible = true };
                settings.Click += OpenSettings;

                System.Windows.Forms.ToolStripMenuItem reset_calibration = new System.Windows.Forms.ToolStripMenuItem { Text = "Reset calibration", Visible = true };
                reset_calibration.Click += ResetCalibration;

                System.Windows.Forms.ToolStripMenuItem exit = new System.Windows.Forms.ToolStripMenuItem { Text = "Exit", Visible = true };
                exit.Click += Shutdown;

                context_menu_strip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { settings, reset_calibration, exit });

                context_menu_strip.ResumeLayout(false);
                Helpers.tray_icon.ContextMenuStrip = context_menu_strip;
                Helpers.tray_icon.MouseClick += Tray_icon_Click;
            }

            eye_tracking_mouse = new EyeTrackingMouse();
            input_manager = new InputManager(eye_tracking_mouse);

            SquirrelAwareApp.HandleEvents(
                onAppUninstall: v =>
                {
                    Helpers.RemoveShortcuts();
                    lock (Helpers.locker)
                    {
                        if (Options.Instance.key_bindings.is_driver_installed)
                        {
                            string interception_installer = System.IO.Path.Combine(
                                Directory.GetParent(System.Reflection.Assembly.GetEntryAssembly().Location).FullName,
                                "install-interception.exe");

                            var process = System.Diagnostics.Process.Start(
                                interception_installer, " /uninstall");
                        }
                    }

                    Helpers.DeleteAppFiles();
                },
                onAppUpdate: (Version version)=>
                {
                    lock (Helpers.locker)
                    {

                        Helpers.ShowBaloonNotification("EyeTrackingMouse has updated. It is now faster and more accurate. \n Double press " + Options.Instance.key_bindings[Key.Modifier] + " to toggle Always On mode. \n" +
                            "This is the last automatic update over the network. I removed auto-update for better security. EyeTrackingMouse is completely offline now.");
                    }
                },
                onFirstRun: () =>
                {
                    Helpers.CreateShortcuts();
                });

            lock (Helpers.locker)
            {

                if (!Options.Instance.user_consent_given) {
                    new ConsentWindow( ()=>
                    {
                        Autostart.Enable();
                        OpenSettings(null, null);
                    }, ()=> {
                        Shutdown(null, null);
                    }).Show();
                }
            }

            application.Run();

            eye_tracking_mouse.Dispose();
            Helpers.tray_icon.Visible = false;
        }

        private static void Tray_icon_Click(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
                OpenSettings(sender, e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

        }
    }
}
