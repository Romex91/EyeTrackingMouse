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

        public static EventHandler KeyBindingsChanged;
        public static EventHandler OptionsChanged;
        public static EventHandler CalibrationModeChanged;

        public Settings(InputManager input_manager)
        {
            this.input_manager = input_manager;

            InitializeComponent();

            lock (Helpers.locker)
            {
                UpdateSliders();
                UpdateTexts();
                UpdateKeyBindingControls();

                CheckboxAutostart.IsChecked = Autostart.IsEnabled;

                is_initialized = true;
            }
        }

        // TODO: make sure it updates when changing key bindings.
        public void UpdateTexts()
        {
            lock (Helpers.locker)
            {

                var key_bindings = Options.Instance.key_bindings;
                VerticalScrollStepTooltip.ToolTip = "How fast mouse wheel 'spins' when you press "
                    + key_bindings[Key.ScrollUp].ToString() + "/"
                    + key_bindings[Key.ScrollDown].ToString();

                HorizontalScrollStepTooltip.ToolTip = "How fast mouse wheel 'spins' when you press "
                    + key_bindings[Key.ScrollLeft].ToString() + "/"
                    + key_bindings[Key.ScrollRight].ToString();

                string calibration_buttons = key_bindings[Key.CalibrateUp].ToString() + "/"
                    + key_bindings[Key.CalibrateLeft].ToString() + "/"
                    + key_bindings[Key.CalibrateDown].ToString() + "/"
                    + key_bindings[Key.CalibrateRight].ToString();


                CalibrationStepTooltip.ToolTip = "How fast the cursor moves when you press " + calibration_buttons;

                ClickFreezeTimeMsTooltip.ToolTip =
                    "How long the cursor will be frozen when you click stuff. \n" +
                    "\n" +
                    "The cursor may shake when controlled by eyes. This makes some problems.\n" +
                    "Imagine you want to click something, but instead, you get a tiny Drag&Drop because the cursor moved during the click.\n" +
                    "Freezing the cursor after click solves this problem.";

                CalibrationFreezeTimeMsTooltip.ToolTip =
                    "How long cursor will be frozen when you calibrate (" + calibration_buttons + "). \n" +
                    "\n" +
                    "Helps clicking little areas when cursor is shaking.";

                DoubleSpeedUpTimeMsTooltip.ToolTip =
                    "Frequent presses speed-up calibration and scrolling. \n" +
                    "If you press " + calibration_buttons + " twice during this time then the second press will move cursor twice farther. \n";

                QuadrupleSpeedUpTimeMsTooltip.ToolTip =
                    "Frequent presses speed-up calibration and scrolling. " +
                    "If you press " + calibration_buttons + " twice during this time then the second press will move cursor four times farther. \n";

                ModifierShortPressTimeMsTooltip.ToolTip =
                    "If you press " + key_bindings[Key.Modifier] + " for a short period of time this press will go to OS. \n" +
                    "The reason this option exist is to make Windows Start Menu available.\n" +
                    "If you see Start Menu more often than you want then decrease this time.\n" +
                    "If you cannot open Start Menu because " + Helpers.application_name + " intercepts your key presses then increase it.";

                SmootheningPointsCountTooltip.ToolTip =
                    "Number of gaze points used by " + Helpers.application_name + " to smooth the cursor position. \n" +
                    "The resulting cursor position is the arithmetic mean of these points.";

                SmotheningZoneRadiusTooltip.ToolTip =
                    "A distance after which the cursor stop being smooth.\n" +
                    "If you move your gaze quickly farther than this value the cursor will jump instantly to the new gaze point.";

                string calibration_view_hotkeys = Helpers.GetModifierString() + " + " +
                    Options.Instance.key_bindings[Key.ShowCalibrationView].ToString();
                    
                CalibrationModeTooltip.ToolTip = "When you use " + calibration_buttons + " " + Helpers.application_name + " slowly gathers data to increase accuracy.\n" +
                    "The algorithm is a bit tricky. It can use different data and different amounts of data.\n" +
                    "To be honest I don't know what parameters are optimal. Trying to figure it out. \n\n" +
                    "Modes:\n" +
                    "1.'Simple & Fast' uses as little data as reasonable. \n" +
                    "   * Quick learning period. \n" +
                    "   * Rough (but probably the best possible) precision. \n" +
                    "   * Absolutely best when the Eye Tracker device has a poor fixation. \n" +
                    "   * Recommended.\n" +
                    "2.'Multidimensional' uses much more data than 'Simple & Fast'. \n" +
                    "   * Tracks head position. \n" +
                    "   * Long learning period (months of using). \n" +
                    "   * Rough initial precision. \n" +
                    "   * When learned it may provide precison better than 'Simple & Fast'. \n" +
                    "   * This mode is experimental.\n" +
                    "   * !!! DON'T use this mode if the Eye Tracker device has a poor fixation !!!";

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

                SmotheningZoneRadius.Value = Options.Instance.smothening_zone_radius;
                SmootheningPointsCount.Value = Options.Instance.smothening_points_count;
                ModifierShortPressTimeMs.Value = Options.Instance.modifier_short_press_duration_ms;

                QuadrupleSpeedUpTimeMs.Value = Options.Instance.quadriple_speed_up_press_time_ms;
                DoubleSpeedUpTimeMs.Value = Options.Instance.double_speedup_press_time_ms;

                CalibrationFreezeTimeMs.Value = Options.Instance.calibrate_freeze_time_ms;
                ClickFreezeTimeMs.Value = Options.Instance.click_freeze_time_ms;
                HorizontalScrollStep.Value = Options.Instance.horizontal_scroll_step;
                VerticalScrollStep.Value = Options.Instance.vertical_scroll_step;
                CalibrationStep.Value = Options.Instance.calibration_step;

                if (Options.Instance.calibration_mode.Equals(Options.CalibrationMode.MultiDimensionPreset))
                {
                    CalibrationModeCombo.SelectedIndex = 0;
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }
                else if (Options.Instance.calibration_mode.Equals(Options.CalibrationMode.SingleDimensionPreset))
                {
                    CalibrationModeCombo.SelectedIndex = 1;
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CustomCalibrationMode.Visibility = Visibility.Visible;
                    CalibrationModeCombo.SelectedIndex = 2;
                }
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
                    Options.Instance.smothening_zone_radius = (int)SmotheningZoneRadius.Value;
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
                }
                else if (sender == DoubleSpeedUpTimeMs)
                {
                    Options.Instance.double_speedup_press_time_ms = (int)DoubleSpeedUpTimeMs.Value;
                }
                else if (sender == ModifierShortPressTimeMs)
                {
                    Options.Instance.modifier_short_press_duration_ms = (int)ModifierShortPressTimeMs.Value;
                }
                else if (sender == SmootheningPointsCount)
                {
                    Options.Instance.smothening_points_count = (int)SmootheningPointsCount.Value;
                }
                OptionsChanged?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile();
            }
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
                    OptionsChanged?.Invoke(this, new EventArgs());

                    Options.Instance.SaveToFile();
                    UpdateSliders();
                }
            }
        }


        private class KeyBindingControl
        {
            public Key key;
            public Button set_new_binding_button;
            public Button set_default_binding_button;
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
                                    Options.Instance.SaveToFile();
                                    KeyBindingsChanged?.Invoke(this, new EventArgs());
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
                        Options.Instance.SaveToFile();
                        KeyBindingsChanged?.Invoke(this, new EventArgs());
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
                    new KeyBindingControl { key = Key.CalibrateDown, set_new_binding_button = CalibrateDown, set_default_binding_button = CalibrateDownDefault },
                    new KeyBindingControl { key = Key.CalibrateLeft, set_new_binding_button = CalibrateLeft, set_default_binding_button = CalibrateLeftDefault },
                    new KeyBindingControl { key = Key.CalibrateRight, set_new_binding_button = CalibrateRigth, set_default_binding_button = CalibrateRigthDefault },
                    new KeyBindingControl { key = Key.CalibrateUp, set_new_binding_button = CalibrateUp, set_default_binding_button = CalibrateUpDefault },
                    new KeyBindingControl { key = Key.LeftMouseButton, set_new_binding_button = LeftMouseButton, set_default_binding_button = LeftMouseButtonDefault },
                    new KeyBindingControl { key = Key.Modifier, set_new_binding_button = EnableMouseControll, set_default_binding_button = EnableMouseControllDefault },
                    new KeyBindingControl { key = Key.RightMouseButton, set_new_binding_button = RightMouseButton, set_default_binding_button = RightMouseButtonDefault },
                    new KeyBindingControl { key = Key.ScrollDown, set_new_binding_button = ScrollDown, set_default_binding_button = ScrollDownDefault },
                    new KeyBindingControl { key = Key.ScrollLeft, set_new_binding_button = ScrollLeft, set_default_binding_button = ScrollLeftDefault },
                    new KeyBindingControl { key = Key.ScrollRight, set_new_binding_button = ScrollRight, set_default_binding_button = ScrollRightDefault },
                    new KeyBindingControl { key = Key.ScrollUp, set_new_binding_button = ScrollUp, set_default_binding_button = ScrollUpDefault },
                    new KeyBindingControl { key = Key.ShowCalibrationView, set_new_binding_button = CalibrationView, set_default_binding_button = CalibrationViewDefault },
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
                    Interceptor.Keys key = Options.Instance.key_bindings[key_binding_control.key];

                    key_binding_control.set_new_binding_button.Content = Helpers.GetKeyString(key, key_binding_control.key == Key.Modifier ? Options.Instance.key_bindings.is_modifier_e0 : false);
                    key_binding_control.set_new_binding_button.IsEnabled = InterceptionMethod.SelectedIndex == 1 && is_driver_loaded;
                    key_binding_control.set_new_binding_button.Background = new SolidColorBrush(Colors.White);

                    key_binding_control.set_default_binding_button.IsEnabled = InterceptionMethod.SelectedIndex == 1 && is_driver_loaded;
                }

                WinApiWarning.Visibility = InterceptionMethod.SelectedIndex == 0 || !is_driver_loaded ? Visibility.Visible : Visibility.Hidden;
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
                    Options.Instance.SaveToFile();
                    KeyBindingsChanged?.Invoke(this, new EventArgs());

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
                Options.Instance.SaveToFile();
                bool success = input_manager.UpdateInterceptionMethod();
                if (!success)
                {
                    if (Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.WinApi)
                    {
                        throw new Exception("Failed setting WinAPI interception method.");
                    }

                    if (!Options.Instance.key_bindings.is_driver_installed)
                    {
                        if (MessageBox.Show(
                            "This interception method will require driver installation and OS reboot.\nContinue?",
                            Helpers.application_name,
                            MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            var driver_installation_window = new DriverInstallationWindow();
                            driver_installation_window.ShowDialog();
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "Failed loading interception driver." +
                            "Reinstall EyeTrackingMouse or install the driver from command line:" +
                            " https://github.com/oblitum/Interception. Application will run using WinAPI.",
                            Helpers.application_name, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                UpdateKeyBindingControls();
                UpdateTexts();
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
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }
                else if (CalibrationModeCombo.SelectedIndex == 1)
                {
                    Options.Instance.calibration_mode = Options.CalibrationMode.SingleDimensionPreset;
                    CustomCalibrationMode.Visibility = Visibility.Collapsed;
                }

                CalibrationModeChanged?.Invoke(this, null);
                OptionsChanged?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile();
            }
        }

        private void AdvancedCalibrationSettings_Click(object sender, RoutedEventArgs e)
        {
            CalibrationSettings calibration_settings = new CalibrationSettings();
            calibration_settings.ShowDialog();
            UpdateSliders();
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

    }
    public class IconToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = value as Icon;
            if (icon == null)
            {
                Trace.TraceWarning("Attempted to convert {0} instead of Icon object in IconToImageSourceConverter", value);
                return null;
            }

            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return imageSource;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}