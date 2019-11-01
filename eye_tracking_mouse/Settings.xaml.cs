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
        public Settings()
        {

            InitializeComponent();
            UpdateSliders();

            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

            UpdateTooltips();

            is_initialized = true;

        }

        // TODO: make sure it updates when changing key bindings.
        public void UpdateTooltips()
        {
            var key_bindings = Options.Instance.key_bindings;
            VerticalScrollStepTooltip.ToolTip = "How fast mouse wheel 'spins' when you press "
                + key_bindings.scroll_up.ToString() + "/"
                + key_bindings.scroll_down.ToString();

            HorizontalScrollStepTooltip.ToolTip = "How fast mouse wheel 'spins' when you press "
                + key_bindings.scroll_left.ToString() + "/"
                + key_bindings.scroll_right.ToString();

            string calibration_buttons = key_bindings.calibrate_up.ToString() + "/"
                + key_bindings.calibrate_left.ToString() + "/"
                + key_bindings.calibrate_down.ToString() + "/"
                + key_bindings.calibrate_right.ToString();


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
                "If you press down and up " + key_bindings.modifier + " faster than this time this press will go to OS. " +
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
