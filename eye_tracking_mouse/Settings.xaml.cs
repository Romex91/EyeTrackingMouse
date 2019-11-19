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

        public Settings(InputManager input_manager)
        {
            this.input_manager = input_manager;

            InitializeComponent();

            lock (Helpers.locker)
            {
                UpdateSliders();
                UpdateTooltips();
                UpdateKeyBindingControls();

                is_initialized = true;

            }
        }

        // TODO: make sure it updates when changing key bindings.
        public void UpdateTooltips()
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


                CalibrationStepTooltip.ToolTip = "How fast cursor moves when you press " + calibration_buttons;

                ClickFreezeTimeMsTooltip.ToolTip =
                    "How long cursor will stay still ignoring your gaze after you click something. " +
                    "Longer the time easier to doubleclick but slower overall mouse control.";

                CalibrationFreezeTimeMsTooltip.ToolTip =
                    "How long cursor will stay still ignoring your gaze after you calibrate (" + calibration_buttons + "). " +
                    "Longer the time easier to click tiny areas but slightly worse long-term calibration.";

                DoubleSpeedUpTimeMsTooltip.ToolTip =
                    "Frequent presses speed-up calibration and scrolling. " +
                    "If you press " + calibration_buttons + " twice during this time then the second press will move cursor twice farther. " +
                    "The same is true about scrolling.";

                QuadrupleSpeedUpTimeMsTooltip.ToolTip =
                    "Frequent presses speed-up calibration and scrolling. " +
                    "If you press " + calibration_buttons + " twice during this time then the second press will move cursor four times farther. " +
                    "The same is true about scrolling.";

                ModifierShortPressTimeMsTooltip.ToolTip =
                    "If you press " + key_bindings[Key.Modifier] + " for short period of time this press will go to OS. " +
                    "The reason this option exist is to make Windows Start Menu available." +
                    "If you see Start Menu more often than you want then decrease this time." +
                    "If you cannot open Start Menu because " + Helpers.application_name + " intercepts your key presses then increase it.";

                SmootheningPointsCountTooltip.ToolTip =
                    "Number of gaze points used by " + Helpers.application_name + " to calculate cursor position. " +
                    "More points means less cursor shaking but slower movement and higher CPU usage.";

                SmotheningZoneRadiusTooltip.ToolTip =
                    Helpers.application_name + " considers only gaze points fitting to zone with this radius when calculating cursor position." +
                    "Bigger the radius means less cursor shaking but slower movement when moving cursor to a huge distance." +
                    "If you move your gaze farther than this radius cursor will move instantly. Otherwise it will move smoothly.";

                CalibrationZoneSizeTooltip.ToolTip =
                    "Size of calibration zone on screen. There can be only one calibration per zone." +
                    "Smaller zones mean more precise but longer calibration and higher CPU usage." +
                    "You may want to increase zones count if you make zone size small." +
                    "Press " + Options.Instance.key_bindings[Key.Modifier].ToString() + " + " +
                    Options.Instance.key_bindings[Key.ShowCalibrationView] +
                    " to see your curent calibrations.";

                CalibrationPointsCountTooltip.ToolTip =
                    "Maximal number of calibration zones on screen. There can be only one calibration per zone. \n" +
                    "More zones mean more precise calibration and higher CPU usage.\n" +
                    "You may want to decrease zone size if you set large zones count.\n" +
                    "Press " + Options.Instance.key_bindings[Key.Modifier].ToString() + " + " +
                    Options.Instance.key_bindings[Key.ShowCalibrationView].ToString() +
                    " to see your curent calibrations.";

            }
        }

        private void UpdateSliders()
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
            CalibrationZoneSize.Value = Options.Instance.calibration_zone_size;
            CalibrationPointsCount.Value = Options.Instance.calibration_max_zones_count;
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
                else if (sender == CalibrationZoneSize)
                {
                    Options.Instance.calibration_zone_size = (int)CalibrationZoneSize.Value;
                }
                else if (sender == CalibrationPointsCount)
                {
                    Options.Instance.calibration_max_zones_count = (int)CalibrationPointsCount.Value;
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
                    input_manager.ReadKeyAsync((read_key_result) =>
                    {
                        lock (Helpers.locker)
                        {
                            if (key_binding == Key.Modifier)
                                Options.Instance.key_bindings.is_modifier_e0 = read_key_result.is_e0_key;
                            Options.Instance.key_bindings[key_binding] = read_key_result.key;

                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                lock (Helpers.locker)
                                {
                                    UpdateKeyBindingControls();
                                    UpdateTooltips();
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
                        UpdateTooltips();
                        Options.Instance.SaveToFile();
                        KeyBindingsChanged?.Invoke(this, new EventArgs());
                    }
                    return;
                }
            }

            throw new Exception("Click event from unassigned button");
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
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
                    Interceptor.Keys key= Options.Instance.key_bindings[key_binding_control.key];

                    string button_content = "";
                    if (key_binding_control.key == Key.Modifier && (key == Interceptor.Keys.RightAlt || key == Interceptor.Keys.Control))
                    {
                        button_content = Options.Instance.key_bindings.is_modifier_e0 ? "Right" : "Left";
                    }

                    if (key == Interceptor.Keys.RightAlt)
                    {
                        button_content += "Alt"; 
                    } else
                    {
                        button_content += key.ToString();
                    }

                    key_binding_control.set_new_binding_button.Content = button_content;
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
                    UpdateTooltips();
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
                if (!success) {
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
                    } else
                    {
                        MessageBox.Show(
                            "Failed loading interception driver." +
                            "Reinstall EyeTrackingMouse or install the driver from command line:" +
                            " https://github.com/oblitum/Interception. Application will run using WinAPI.",
                            Helpers.application_name, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }                    
                UpdateKeyBindingControls();
                UpdateTooltips();
            }
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