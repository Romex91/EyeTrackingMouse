using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for CalibrationSettings.xaml
    /// </summary>
    public partial class CalibrationSettings : Window
    {
        public CalibrationSettings()
        {
            InitializeComponent();
            additional_dimensions_checkboxes = new Dictionary<MultidimensionCalibrationType, CheckBox> {
                { MultidimensionCalibrationType.HeadDirection, HeadDirection},
                { MultidimensionCalibrationType.HeadPosition, HeadPosition},
                { MultidimensionCalibrationType.LeftEye, LeftEye},
                { MultidimensionCalibrationType.RightEye, RightEye},
            };

            UpdateControls();
            ignore_changes = false;
        }

        private void UpdateControls()
        {
            lock (Helpers.locker)
            {

                string modifier_key_string= Helpers.GetKeyString(Options.Instance.key_bindings[Key.Modifier], Options.Instance.key_bindings.is_modifier_e0);

                CalibrationZoneSizeTooltip.ToolTip =
                    "Size of calibration zone on screen. There can be only one calibration per zone." +
                    "Smaller zones result in a more precise but longer calibration and higher CPU usage." +
                    "You may want to increase zones count if you make zone size small." +
                    "Press " + modifier_key_string + " + " +
                    Options.Instance.key_bindings[Key.ShowCalibrationView] +
                    " to see your curent calibrations.";

                CalibrationPointsCountTooltip.ToolTip =
                    "Maximum number of calibration zones on screen. There can be only one calibration per zone. \n" +
                    "More zones mean more precise calibration and higher CPU usage.\n" +
                    "You may want to decrease zone size if you set large zones count.\n" +
                    "Press " + modifier_key_string + " + " +
                    Options.Instance.key_bindings[Key.ShowCalibrationView].ToString() +
                    " to see your curent calibrations.";

                CalibrationZoneSize.Value = Options.Instance.calibration_mode.zone_size;
                CalibrationPointsCount.Value = Options.Instance.calibration_mode.max_zones_count;
                ConsideredZonesCount.Value = Options.Instance.calibration_mode.considered_zones_count;
                MultidimensionalDetalization.Value = Options.Instance.calibration_mode.multi_dimensions_detalization;
                UpdatePeriodMs.Value = Options.Instance.calibration_mode.update_period_ms;

                foreach (var item in additional_dimensions_checkboxes)
                {
                    item.Value.IsChecked = (Options.Instance.calibration_mode.multidimension_calibration_type & item.Key) == item.Key;
                }

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
            if (ignore_changes)
                return;
            lock (Helpers.locker)
            {
                // TODO: test sliders with dinamic minimum and maximum.
                if (sender == CalibrationZoneSize)
                {
                    Options.Instance.calibration_mode.zone_size = (int)CalibrationZoneSize.Value;
                }
                else if (sender == CalibrationPointsCount)
                {
                    Options.Instance.calibration_mode.max_zones_count = (int)CalibrationPointsCount.Value;
                }
                Settings.OptionsChanged?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile();

                ignore_changes = true;
                UpdateControls();
                ignore_changes = false;

            }
        }

        private readonly Dictionary<MultidimensionCalibrationType, CheckBox> additional_dimensions_checkboxes;

        private void CheckBox_Changed(object sender, EventArgs e)
        {
            if (ignore_changes)
                return;

            lock (Helpers.locker)
            {

                foreach (var item in additional_dimensions_checkboxes)
                {
                    if (sender == item.Value)
                    {
                        if (item.Value.IsChecked == true)
                            Options.Instance.calibration_mode.multidimension_calibration_type |= item.Key;
                        else
                            Options.Instance.calibration_mode.multidimension_calibration_type &= ~item.Key;

                    }
                }

                Settings.CalibrationModeChanged?.Invoke(this, null);
                Settings.OptionsChanged?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile();

                ignore_changes = true;
                UpdateControls();
                ignore_changes = false;
            }

        }

        private void CalibrationModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ignore_changes)
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

                Settings.CalibrationModeChanged?.Invoke(this, null);
                Settings.OptionsChanged?.Invoke(this, new EventArgs());
                Options.Instance.SaveToFile();

                ignore_changes = true;
                UpdateControls();
                ignore_changes = false;
            }
        }

        private bool ignore_changes = true;
    }
}
