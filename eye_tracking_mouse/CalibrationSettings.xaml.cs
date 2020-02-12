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
                var key_bindings = Options.Instance.key_bindings;
                string calibration_buttons = key_bindings[Key.CalibrateUp].ToString() + "/"
                    + key_bindings[Key.CalibrateLeft].ToString() + "/"
                    + key_bindings[Key.CalibrateDown].ToString() + "/"
                    + key_bindings[Key.CalibrateRight].ToString();

                CalibrationPointsCount.ToolTip =
                    "Maximum number of arrows in CALIBRATION VIEW. \n" +
                    "Each arrow points from the cursor position BEFORE correction to the position AFTER correction.\n\n" +
                    "Suppose you want to click point A, but because of imperfect precision the cursor is at point B.\n" +
                    "You press " + calibration_buttons +" until the cursor reach point A and then you click it. \n" +
                    "This action creates an arrow pointing from B (imprecise cursor position) to A(click point after correction).\n\n" +
                    "More arrows result in better precision but longer algorithm learning and higher CPU usage.\n" +
                    "You may want to decrease Min distance between arrows if you set large Max arrows count.";

                CalibrationZoneSize.ToolTip =
                    "Minimum distance between two arrows.\n" +
                    "If you make a correction too close to an existing arrow this arrow will be rewritten.\n" +
                    "Smaller distance result in better precision but longer algorithm learning and higher CPU usage.\n" +
                    "You may want to increase Max arrows count if you make Min distance small.";

                ConsideredZonesCount.ToolTip = 
                    "Defines how many arrows will be used to calculate the resulting shift. \n" +
                    "Closer arrows have more influence on the resulting shift than farther ones.";

                UpdatePeriodMs.ToolTip =
                    "Energy saving option. The calibration algorithm will iterate once per this period of time. \n" +
                    "Bigger period results in less CPU load, but the cursor may shake.";

                MultidimensionalDetalization.ToolTip = 
                    "If you check any of the checkboxes below they will be represented as additional dimensions. \n" +
                    "Arrows in CALIBRATION VIEW will become colorful and the algorithm will consider color when calculating distance between arrows. \n" +
                    "This slider determines how spacious these new dimensions are. \n\n" +
                    "Don't create too many dimensions. It will produce a hyper black hole sucking all the data and giving nothing back.";

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
                if (sender == CalibrationZoneSize)
                {
                    Options.Instance.calibration_mode.zone_size = (int)CalibrationZoneSize.Value;
                }
                else if (sender == CalibrationPointsCount)
                {
                    Options.Instance.calibration_mode.max_zones_count = (int)CalibrationPointsCount.Value;
                }
                else if (sender == ConsideredZonesCount)
                {
                    Options.Instance.calibration_mode.considered_zones_count = (int)ConsideredZonesCount.Value;
                }
                else if (sender == UpdatePeriodMs) {
                    Options.Instance.calibration_mode.update_period_ms = (int)UpdatePeriodMs.Value;
                }
                else if (sender == MultidimensionalDetalization)
                {
                    Options.Instance.calibration_mode.multi_dimensions_detalization = (int)MultidimensionalDetalization.Value;
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

        private void CalibrationViewButton_Click(object sender, RoutedEventArgs e)
        {
            App.ToggleCalibrationWindow();
        }
    }
}
