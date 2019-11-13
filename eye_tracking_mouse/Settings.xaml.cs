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

                DoublePressTimeMsTooltip.ToolTip =
                    "Frequent presses speed-up calibration and scrolling. " +
                    "If you press " + calibration_buttons + " two times during this time then the second press will move cursor twice farther. " +
                    "The same is true about scrolling." +
                    "Longer the time faster to cover long distance but slower precise movements.";

                ShortPressTimeMsTooltip.ToolTip =
                    "If you press down and up " + key_bindings[Key.Modifier] + " faster than this time this press will go to OS. " +
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
            ShortPressTimeMs.Value = Options.Instance.short_click_duration_ms;
            DoublePressTimeMs.Value = Options.Instance.double_click_duration_ms;
            CalibrationFreezeTimeMs.Value = Options.Instance.calibrate_freeze_time_ms;
            ClickFreezeTimeMs.Value = Options.Instance.click_freeze_time_ms;
            HorizontalScrollStep.Value = Options.Instance.horizontal_scroll_step;
            VerticalScrollStep.Value = Options.Instance.vertical_scroll_step;
            CalibrationStep.Value = Options.Instance.calibration_step;
            CalibrationZoneSize.Value = Options.Instance.calibration_zone_size;
            CalibrationPointsCount.Value = Options.Instance.calibration_max_zones_count;
        }

        private void SmotheningZoneRadius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;
            lock (Helpers.locker)
            {
                Options.Instance.smothening_zone_radius = (int)SmotheningZoneRadius.Value;
                Options.Instance.SaveToFile();
            }
        }

        private void SmootheningPointsCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.smothening_points_count = (int)SmootheningPointsCount.Value;
                Options.Instance.SaveToFile();
            }

        }

        private void ShortPressTimeMs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.short_click_duration_ms = (int)ShortPressTimeMs.Value;
                Options.Instance.SaveToFile();
            }

        }

        private void DoublePressTimeMs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.double_click_duration_ms = (int)DoublePressTimeMs.Value;
                Options.Instance.SaveToFile();
            }

        }

        private void CalibrationFreezeTimeMs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.calibrate_freeze_time_ms = (int)CalibrationFreezeTimeMs.Value;
                Options.Instance.SaveToFile();
            }

        }

        private void ClickFreezeTimeMs_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.click_freeze_time_ms = (int)ClickFreezeTimeMs.Value;
                Options.Instance.SaveToFile();
            }
        }

        private void HorizontalScrollStep_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.horizontal_scroll_step = (int)HorizontalScrollStep.Value;
                Options.Instance.SaveToFile();
            }
        }

        private void VerticalScrollStep_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.vertical_scroll_step = (int)VerticalScrollStep.Value;
                Options.Instance.SaveToFile();
            }
        }

        private void CalibrationStep_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.calibration_step = (int)CalibrationStep.Value;
                Options.Instance.SaveToFile();
                ShiftsStorage.Instance.OnSettingsChanged();
            }
        }

        private void CalibrationPointsCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.calibration_max_zones_count = (int)CalibrationPointsCount.Value;
                Options.Instance.SaveToFile();
                ShiftsStorage.Instance.OnSettingsChanged();
            }
        }

        private void CalibrationZoneSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!is_initialized)
                return;

            lock (Helpers.locker)
            {
                Options.Instance.calibration_zone_size = (int)CalibrationZoneSize.Value;
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
            public Button new_binding;
            public Button default_binding;
        }

        private List<KeyBindingControl> key_binding_controls_list;

        private void KeyBindingButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var key_binding_control in key_binding_controls_list)
            {
                if (key_binding_control.new_binding == sender)
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
                                }
                            }));
                        }
                    });

                    key_binding_control.new_binding.Background = new SolidColorBrush(Colors.LightBlue);
                    key_binding_control.new_binding.Content = "PRESS ANY KEY";
                    return;
                }
                else if (key_binding_control.default_binding == sender)
                {
                    lock (Helpers.locker)
                    {
                        Options.Instance.key_bindings[key_binding_control.key] = KeyBindings.default_bindings[key_binding_control.key];
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

        private void UpdateKeyBindingControls()
        {
            if (key_binding_controls_list == null)
            {
                key_binding_controls_list = new List<KeyBindingControl>
                {
                    new KeyBindingControl { key = Key.CalibrateDown, new_binding = CalibrateDown, default_binding = CalibrateDownDefault },
                    new KeyBindingControl { key = Key.CalibrateLeft, new_binding = CalibrateLeft, default_binding = CalibrateLeftDefault },
                    new KeyBindingControl { key = Key.CalibrateRight, new_binding = CalibrateRigth, default_binding = CalibrateRigthDefault },
                    new KeyBindingControl { key = Key.CalibrateUp, new_binding = CalibrateUp, default_binding = CalibrateUpDefault },
                    new KeyBindingControl { key = Key.LeftMouseButton, new_binding = LeftMouseButton, default_binding = LeftMouseButtonDefault },
                    new KeyBindingControl { key = Key.Modifier, new_binding = EnableMouseControll, default_binding = EnableMouseControllDefault },
                    new KeyBindingControl { key = Key.RightMouseButton, new_binding = RightMouseButton, default_binding = RightMouseButtonDefault },
                    new KeyBindingControl { key = Key.ScrollDown, new_binding = ScrollDown, default_binding = ScrollDownDefault },
                    new KeyBindingControl { key = Key.ScrollLeft, new_binding = ScrollLeft, default_binding = ScrollLeftDefault },
                    new KeyBindingControl { key = Key.ScrollRight, new_binding = ScrollRight, default_binding = ScrollRightDefault },
                    new KeyBindingControl { key = Key.ScrollUp, new_binding = ScrollUp, default_binding = ScrollUpDefault },
                    new KeyBindingControl { key = Key.ShowCalibrationView, new_binding = CalibrationView, default_binding = CalibrationViewDefault },
                };
            }

            lock (Helpers.locker)
            {
                foreach (var key_binding_control in key_binding_controls_list)
                {
                    key_binding_control.new_binding.Content = Options.Instance.key_bindings[key_binding_control.key].ToString();
                    key_binding_control.new_binding.IsEnabled = InterceptionMethod.SelectedIndex == 1;
                    key_binding_control.new_binding.Background = new SolidColorBrush(Colors.White);

                    key_binding_control.default_binding.IsEnabled = InterceptionMethod.SelectedIndex == 1;
                }

                WinApiWarning.Visibility = InterceptionMethod.SelectedIndex == 0 ? Visibility.Visible : Visibility.Hidden;
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
                    Options.Instance.key_bindings.bindings = new Dictionary<Key, Interceptor.Keys>(KeyBindings.default_bindings);
                    Options.Instance.key_bindings.interception_method = KeyBindings.InterceptionMethod.WinApi;
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