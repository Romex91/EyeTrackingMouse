using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private bool is_initialized = false;

        private InputManager input_manager;

        public Settings(InputManager input_manager)
        {
            this.input_manager = input_manager;

            InitializeComponent();

            lock (Helpers.locker)
            {
                UpdateTexts();
                UpdateKeyBindingControls();

                CheckboxAutostart.IsChecked = Autostart.IsEnabled;
                CheckboxAccessibility.IsChecked = Options.Instance.accessibility_mode;

                UpdateSliders();
            }
        }

        // TODO: make sure it updates when changing key bindings.
        public void UpdateTexts()
        {
            lock (Helpers.locker)
            {

                var key_bindings = Options.Instance.key_bindings;
                VerticalScrollStep.ToolTip = "How fast mouse wheel 'spins' when you press "
                    + key_bindings[Key.ScrollUp].ToString() + "/"
                    + key_bindings[Key.ScrollDown].ToString();

                HorizontalScrollStep.ToolTip = "How fast mouse wheel 'spins' horizontally when you press "
                    + key_bindings[Key.ScrollLeft].ToString() + "/"
                    + key_bindings[Key.ScrollRight].ToString();

                string calibration_buttons = key_bindings[Key.CalibrateUp].ToString() + "/"
                    + key_bindings[Key.CalibrateLeft].ToString() + "/"
                    + key_bindings[Key.CalibrateDown].ToString() + "/"
                    + key_bindings[Key.CalibrateRight].ToString();


                CalibrationStep.ToolTip = "How fast the cursor moves when you press " + calibration_buttons;

                ClickFreezeTimeMs.ToolTip =
                    "How long the cursor will be frozen when you click stuff. \n" +
                    "\n" +
                    "The cursor may shake when controlled by eyes. This makes some problems.\n" +
                    "Imagine you want to click something, but instead, you get a tiny Drag&Drop because the cursor moved during the click.\n" +
                    "Freezing the cursor after clicks solves this problem.";

                CalibrationFreezeTimeMs.ToolTip =
                    "How long cursor will be frozen when you calibrate (" + calibration_buttons + "). \n" +
                    "\n" +
                    "Helps clicking little areas when cursor is shaking.";

                DoubleSpeedUpTimeMs.ToolTip =
                    "Frequent presses speed-up calibration and scrolling. \n" +
                    "If you press " + calibration_buttons + " twice during this time then the second press will move cursor twice farther.";

                QuadrupleSpeedUpTimeMs.ToolTip =
                    "Frequent presses speed-up calibration and scrolling. \n" +
                    "If you press " + calibration_buttons + " twice during this time then the second press will move cursor four times farther.";

                ModifierShortPressTimeMs.ToolTip =
                    "If you press " + key_bindings[Key.Modifier] + " for a short period of time this press will go to OS. \n" +
                    "The reason this option exists is to make Windows Start Menu available.\n" +
                    "If you see Start Menu more often than you want then decrease this time.\n" +
                    "If you cannot open Start Menu because " + Helpers.application_name + " intercepts your key presses then increase it.";

                SmootheningPointsCount.ToolTip =
                    "The eye tracker provides gaze point coordinates ~30 times per second.\n" +
                    "If the cursor jumped to each point immediately it would shake a lot.\n" +
                    "Instead of that the app caches N last points and uses their average as \n" +
                    "the current cursor position. This makes the cursor movement smooth \n" +
                    "although too much points add latency.";

                SmotheningZoneRadius.ToolTip =
                    "If you move your gaze quickly farther than this value the cursor will jump instantly to the new gaze point.\n" +
                    "This cancels latency caused by smoothening.";

                string calibration_view_hotkeys = Helpers.GetModifierString() + " + " +
                    Options.Instance.key_bindings[Key.ShowCalibrationView].ToString();

                CalibrationMode_RightEye.ToolTip =
                CalibrationMode_PoorFixation.ToolTip =
                CalibrationMode_Default.ToolTip =
                CalibrationModeLabel.ToolTip =
                CalibrationModeCombo.ToolTip =
                "When you use " + calibration_buttons + " " + Helpers.application_name + " slowly gathers data to increase accuracy.\n" +
                    "The algorithm is a bit tricky. It can use different data and different amounts of data.\n" +
                    "Modes:\n" +
                    "1.Default \n" +
                    "   * Tracks head position. \n" +
                    "   * Long learning period (weeks of using). \n" +
                    "   * Rough initial precision. \n" +
                    "   * When learned provides better precison. \n" +
                    "   * Uses left eye.\n" +
                    "2.Right eye \n" +
                    "   * Same as Default but uses right eye.\n" +
                    "   * Third eye isn't supported.\n" +
                    "3.Poor tracker fixation \n" +
                    "   * Uses as little data as reasonable. \n" +
                    "   * Quick learning period. \n" +
                    "   * Rough precision. \n" +
                    "   * Doesn't distinguish left and right eyes. \n" +
                    "   * Best when the Eye Tracker device has a poor fixation.";

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                TextBlockVersion.Text = "Version: " + fileVersionInfo.FileMajorPart + "." + fileVersionInfo.FileMinorPart + "." + fileVersionInfo.FileBuildPart;
            }
        }

        private void Hyperlink_RequestNavigate(
            object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void UpdateSliders()
        {
            lock (Helpers.locker)
            {
                is_initialized = false;
                SmotheningZoneRadius.Value = Options.Instance.instant_jump_distance;
                SmootheningPointsCount.Value = Options.Instance.smothening_points_count;
                ModifierShortPressTimeMs.Value = Options.Instance.modifier_short_press_duration_ms;

                QuadrupleSpeedUpTimeMs.Maximum = 500;
                DoubleSpeedUpTimeMs.Minimum = 0;
                QuadrupleSpeedUpTimeMs.Value = Options.Instance.quadriple_speed_up_press_time_ms;
                DoubleSpeedUpTimeMs.Value = Options.Instance.double_speedup_press_time_ms;
                UpdateSpeedUpControllersMinMax();

                CalibrationFreezeTimeMs.Value = Options.Instance.calibrate_freeze_time_ms;
                ClickFreezeTimeMs.Value = Options.Instance.click_freeze_time_ms;
                HorizontalScrollStep.Value = Options.Instance.horizontal_scroll_step;
                VerticalScrollStep.Value = Options.Instance.vertical_scroll_step;
                CalibrationStep.Value = Options.Instance.calibration_step;

                if (Options.Instance.calibration_mode.Equals(Options.CalibrationMode.MultiDimensionPreset))
                {
                    CalibrationModeCombo.SelectedIndex = 0;
                    CalibrationMode_Custom.Visibility = Visibility.Collapsed;
                }
                else if (Options.Instance.calibration_mode.Equals(Options.CalibrationMode.MultiDimensionPresetRightEye))
                {
                    CalibrationModeCombo.SelectedIndex = 1;
                    CalibrationMode_Custom.Visibility = Visibility.Collapsed;
                }
                else if (Options.Instance.calibration_mode.Equals(Options.CalibrationMode.SingleDimensionPreset))
                {
                    CalibrationModeCombo.SelectedIndex = 2;
                    CalibrationMode_Custom.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CalibrationMode_Custom.Visibility = Visibility.Visible;
                    CalibrationModeCombo.SelectedIndex = 3;
                }
                is_initialized = true;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;
            lock (Helpers.locker)
            {
                // TODO: test sliders with dinamic minimum and maximum.

                if (sender == SmotheningZoneRadius)
                {
                    Options.Instance.instant_jump_distance = (int)SmotheningZoneRadius.Value;
                }
                else if (sender == CalibrationStep)
                {
                    Options.Instance.calibration_step = (int)CalibrationStep.Value;
                }
                else if (sender == VerticalScrollStep)
                {
                    Options.Instance.vertical_scroll_step = (int)VerticalScrollStep.Value;
                }
                else if (sender == HorizontalScrollStep)
                {
                    Options.Instance.horizontal_scroll_step = (int)HorizontalScrollStep.Value;
                }
                else if (sender == ClickFreezeTimeMs)
                {
                    Options.Instance.click_freeze_time_ms = (int)ClickFreezeTimeMs.Value;
                }
                else if (sender == CalibrationFreezeTimeMs)
                {
                    Options.Instance.calibrate_freeze_time_ms = (int)CalibrationFreezeTimeMs.Value;
                }
                else if (sender == QuadrupleSpeedUpTimeMs)
                {
                    Options.Instance.quadriple_speed_up_press_time_ms = (int)QuadrupleSpeedUpTimeMs.Value;
                    UpdateSpeedUpControllersMinMax();
                }
                else if (sender == DoubleSpeedUpTimeMs)
                {
                    Options.Instance.double_speedup_press_time_ms = (int)DoubleSpeedUpTimeMs.Value;
                    UpdateSpeedUpControllersMinMax();
                }
                else if (sender == ModifierShortPressTimeMs)
                {
                    Options.Instance.modifier_short_press_duration_ms = (int)ModifierShortPressTimeMs.Value;
                }
                else if (sender == SmootheningPointsCount)
                {
                    Options.Instance.smothening_points_count = (int)SmootheningPointsCount.Value;
                }
                Options.Changed?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile(Options.Filepath);
            }
        }

        private void UpdateSpeedUpControllersMinMax()
        {
            DoubleSpeedUpTimeMs.Minimum = (int)QuadrupleSpeedUpTimeMs.Value;
            QuadrupleSpeedUpTimeMs.Maximum = (int)DoubleSpeedUpTimeMs.Value;
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                    "You will lose your settings.\nContinue?",
                    Helpers.application_name,
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                lock (Helpers.locker)
                {
                    var key_bindings = Options.Instance.key_bindings;
                    Options.Instance = Options.Default();
                    Options.Instance.key_bindings = key_bindings;
                    Options.CalibrationMode.Changed?.Invoke(this, new EventArgs());
                    Options.Changed?.Invoke(this, new EventArgs());

                    Options.Instance.SaveToFile(Options.Filepath);
                    UpdateSliders();
                }
            }
        }


        private class KeyBindingControl
        {
            public Key key;
            public Button set_new_binding_button;
            public Button set_default_binding_button;
            public Button unset_binding_button;
        }

        private List<KeyBindingControl> key_binding_controls_list;

        private void KeyBindingButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var key_binding_control in key_binding_controls_list)
            {
                if (key_binding_control.set_new_binding_button == sender)
                {
                    Key key_binding = key_binding_control.key;
                    input_manager.ReadKey((read_key_result) =>
                    {
                        lock (Helpers.locker)
                        {
                            if (key_binding == Key.Modifier)
                                Options.Instance.key_bindings.is_modifier_e0 = read_key_result.is_e0_key;
                            Options.Instance.key_bindings[key_binding] = read_key_result.key;

                            Dispatcher.BeginInvoke(new System.Action(() =>
                            {
                                lock (Helpers.locker)
                                {
                                    UpdateKeyBindingControls();
                                    UpdateTexts();
                                    Options.Instance.SaveToFile(Options.Filepath);
                                    KeyBindings.Changed?.Invoke(this, new EventArgs());
                                    IsEnabled = true;
                                }
                            }));
                        }
                    });

                    key_binding_control.set_new_binding_button.Background = new SolidColorBrush(Colors.LightBlue);
                    key_binding_control.set_new_binding_button.Content = "PRESS ANY KEY";

                    IsEnabled = false;
                    return;
                }
                else if (key_binding_control.set_default_binding_button == sender)
                {
                    lock (Helpers.locker)
                    {
                        Options.Instance.key_bindings[key_binding_control.key] = KeyBindings.default_bindings[key_binding_control.key];
                        if (key_binding_control.key == Key.Modifier)
                            Options.Instance.key_bindings.is_modifier_e0 = true;
                        UpdateKeyBindingControls();
                        UpdateTexts();
                        Options.Instance.SaveToFile(Options.Filepath);
                        KeyBindings.Changed?.Invoke(this, new EventArgs());
                    }
                    return;
                }
                else if (key_binding_control.unset_binding_button == sender)
                {
                    lock (Helpers.locker)
                    {
                        Options.Instance.key_bindings[key_binding_control.key] = null;
                        UpdateKeyBindingControls();
                        UpdateTexts();
                        Options.Instance.SaveToFile(Options.Filepath);
                        KeyBindings.Changed?.Invoke(this, new EventArgs());
                    }
                    return;
                }
            }

            throw new Exception("Click event from unassigned button");
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !IsEnabled;
        }

        private void UpdateKeyBindingControls()
        {
            if (key_binding_controls_list == null)
            {
                key_binding_controls_list = new List<KeyBindingControl>
                {
                    new KeyBindingControl { key = Key.CalibrateDown, set_new_binding_button = CalibrateDown, set_default_binding_button = CalibrateDownDefault, unset_binding_button=CalibrateDownUnset  },
                    new KeyBindingControl { key = Key.CalibrateLeft, set_new_binding_button = CalibrateLeft, set_default_binding_button = CalibrateLeftDefault, unset_binding_button=CalibrateLeftUnset  },
                    new KeyBindingControl { key = Key.CalibrateRight, set_new_binding_button = CalibrateRigth, set_default_binding_button = CalibrateRigthDefault, unset_binding_button=CalibrateRigthUnset  },
                    new KeyBindingControl { key = Key.CalibrateUp, set_new_binding_button = CalibrateUp, set_default_binding_button = CalibrateUpDefault, unset_binding_button=CalibrateUpUnset  },
                    new KeyBindingControl { key = Key.LeftMouseButton, set_new_binding_button = LeftMouseButton, set_default_binding_button = LeftMouseButtonDefault, unset_binding_button=LeftMouseButtonUnset  },
                    new KeyBindingControl { key = Key.Modifier, set_new_binding_button = EnableMouseControll, set_default_binding_button = EnableMouseControllDefault, unset_binding_button=EnableMouseControllUnset  },
                    new KeyBindingControl { key = Key.RightMouseButton, set_new_binding_button = RightMouseButton, set_default_binding_button = RightMouseButtonDefault, unset_binding_button=RightMouseButtonUnset  },
                    new KeyBindingControl { key = Key.ScrollDown, set_new_binding_button = ScrollDown, set_default_binding_button = ScrollDownDefault, unset_binding_button=ScrollDownUnset  },
                    new KeyBindingControl { key = Key.ScrollLeft, set_new_binding_button = ScrollLeft, set_default_binding_button = ScrollLeftDefault, unset_binding_button=ScrollLeftUnset  },
                    new KeyBindingControl { key = Key.ScrollRight, set_new_binding_button = ScrollRight, set_default_binding_button = ScrollRightDefault, unset_binding_button=ScrollRightUnset  },
                    new KeyBindingControl { key = Key.ScrollUp, set_new_binding_button = ScrollUp, set_default_binding_button = ScrollUpDefault, unset_binding_button=ScrollUpUnset  },
                    new KeyBindingControl { key = Key.ShowCalibrationView, set_new_binding_button = CalibrationView, set_default_binding_button = CalibrationViewDefault, unset_binding_button=CalibrationViewUnset },

                    new KeyBindingControl { key = Key.Accessibility_SaveCalibration, set_new_binding_button = Accessibility_SaveCalibration, set_default_binding_button = Accessibility_SaveCalibrationDefault, unset_binding_button=Accessibility_SaveCalibrationUnset },
                    new KeyBindingControl { key = Key.StopCalibration, set_new_binding_button = StopCalibration, set_default_binding_button = StopCalibrationDefault, unset_binding_button=StopCalibrationUnset },
                };
            }

            lock (Helpers.locker)
            {
                bool is_driver_loaded = input_manager.IsDriverLoaded();
                if (Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.OblitaDriver)
                    InterceptionMethod.SelectedIndex = 1;
                else
                    InterceptionMethod.SelectedIndex = 0;

                foreach (var key_binding_control in key_binding_controls_list)
                {
                    Interceptor.Keys? key = Options.Instance.key_bindings[key_binding_control.key];

                    key_binding_control.set_new_binding_button.Content = Helpers.GetKeyString(key, key_binding_control.key == Key.Modifier ? Options.Instance.key_bindings.is_modifier_e0 : false);
                    key_binding_control.set_new_binding_button.IsEnabled = InterceptionMethod.SelectedIndex == 1 && is_driver_loaded;
                    key_binding_control.set_new_binding_button.Background = new SolidColorBrush(Colors.White);

                    key_binding_control.set_default_binding_button.IsEnabled = InterceptionMethod.SelectedIndex == 1 && is_driver_loaded;
                }
                if (is_driver_loaded)
                {
                    Options.Instance.key_bindings.is_driver_installed = true;
                    Options.Instance.SaveToFile(Options.Filepath);
                }

                WinApiWarning.Visibility = InterceptionMethod.SelectedIndex == 0 || !is_driver_loaded ? Visibility.Visible : Visibility.Hidden;
                Button_UninstallOblita.Visibility = Options.Instance.key_bindings.is_driver_installed ? Visibility.Visible : Visibility.Hidden;

                KeyBindings.Changed?.Invoke(this, new EventArgs());
            }
        }


        private void ResetEverything_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "You will lose your key bindings.\nContinue?",
                Helpers.application_name,
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                lock (Helpers.locker)
                {
                    Options.Instance.key_bindings = new KeyBindings();
                    Options.Instance.SaveToFile(Options.Filepath);
                    KeyBindings.Changed?.Invoke(this, new EventArgs());

                    UpdateKeyBindingControls();
                    UpdateTexts();
                }
            }
        }

        private void InterceptionMethod_Selected(object sender, RoutedEventArgs e)
        {
            if (!is_initialized)
                return;
            lock (Helpers.locker)
            {
                Options.Instance.key_bindings.interception_method = InterceptionMethod.SelectedIndex == 0 ? KeyBindings.InterceptionMethod.WinApi : KeyBindings.InterceptionMethod.OblitaDriver;
                bool success = input_manager.Reset();

                if (!success)
                {
                    if (Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.WinApi)
                    {
                        throw new Exception("Failed setting WinAPI interception method.");
                    }

                    Options.Instance.key_bindings.is_driver_installed = false;
                    if (MessageBox.Show(
                        "This interception method will require driver installation and OS reboot.\nContinue?",
                        Helpers.application_name,
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var driver_installation_window = new DriverInstallationWindow();
                        driver_installation_window.ShowDialog();
                    }
                }

                Options.Instance.SaveToFile(Options.Filepath);

                UpdateKeyBindingControls();
                UpdateTexts();
            }
        }

        private void Button_UninstallOblita_Click(object sender, RoutedEventArgs e)
        {
            if (!is_initialized)
                return;
            lock (Helpers.locker)
            {
                if (MessageBox.Show("This will require system restart. Sure?", Helpers.application_name, MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                {
                    return;
                }

                string interception_installer = System.IO.Path.Combine(Environment.CurrentDirectory, "install-interception.exe");
                var process = System.Diagnostics.Process.Start(interception_installer, "/uninstall");
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Options.Instance.key_bindings.is_driver_installed = false;
                    Options.Instance.SaveToFile(Options.Filepath);
                    if (MessageBox.Show("Driver is removed. Reboot now?", Helpers.application_name, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("shutdown.exe", "-r -t 0");
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't uninstall the interception driver: installer returned non-zero exit code.",
                        Helpers.application_name, 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            }
        }

        private void CalibrationModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!is_initialized)
                return;
            lock (Helpers.locker)
            {
                if (CalibrationModeCombo.SelectedIndex == 0)
                {
                    Options.Instance.calibration_mode = Options.CalibrationMode.MultiDimensionPreset;
                    CalibrationMode_Custom.Visibility = Visibility.Collapsed;
                }
                if (CalibrationModeCombo.SelectedIndex == 1)
                {
                    Options.Instance.calibration_mode = Options.CalibrationMode.MultiDimensionPresetRightEye;
                    CalibrationMode_Custom.Visibility = Visibility.Collapsed;
                }
                else if (CalibrationModeCombo.SelectedIndex == 2)
                {
                    Options.Instance.calibration_mode = Options.CalibrationMode.SingleDimensionPreset;
                    CalibrationMode_Custom.Visibility = Visibility.Collapsed;
                }

                Options.CalibrationMode.Changed?.Invoke(this, null);
                Options.Changed?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile(Options.Filepath);
            }
        }

        private void CheckboxAutostart_Checked(object sender, RoutedEventArgs e)
        {
            if (is_initialized)
                Autostart.Enable();
        }

        private void CheckboxAutostart_Unchecked(object sender, RoutedEventArgs e)
        {
            if (is_initialized)
                Autostart.Disable();
        }

        private void CheckboxAccessibility_Checked(object sender, RoutedEventArgs e)
        {
            if (!is_initialized)
                return;
            lock (Helpers.locker)
            {
                Options.Instance.accessibility_mode= true;
                input_manager.Reset();
                Options.Changed?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile(Options.Filepath);
            }
        }

        private void CheckboxAccessibility_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!is_initialized)
                return;
            lock (Helpers.locker)
            {
                Options.Instance.accessibility_mode = false;
                input_manager.Reset();
                Options.Changed?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile(Options.Filepath);
            }
        }
    }
}