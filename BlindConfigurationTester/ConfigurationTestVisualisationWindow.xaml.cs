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

namespace BlindConfigurationTester
{
    /// <summary>
    /// Interaction logic for ConfigurationTestVisualisationWindow.xaml
    /// </summary>
    public partial class ConfigurationTestVisualisationWindow : Window
    {
        private eye_tracking_mouse.ICalibrationManager calibration_manager;
        private List<DataPoint> data_points;
        private int current_point_index = 0;
        private string configuration;
        private eye_tracking_mouse.Options.CalibrationMode calibration_mode;

        public ConfigurationTestVisualisationWindow(string configuration, List<DataPoint> data_points)
        {
            InitializeComponent();
            this.data_points = data_points;
            calibration_mode = Helpers.GetCalibrationMode(configuration);
            calibration_manager = Helpers.SetupCalibrationManager(calibration_mode);
            this.configuration = configuration ?? "User Data";
            this.Closing += ConfigurationTestVisualisationWindow_Closing;
            this.KeyDown += ConfigurationTestVisualisationWindow_KeyDown;
            OnCurrentPointChanged();
        }

        private void ConfigurationTestVisualisationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            calibration_manager.IsDebugWindowEnabled = false;
        }

        private void ConfigurationTestVisualisationWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                SetCurrentPointIndex(current_point_index - 1);
                e.Handled = true;
            }
            if (e.Key == Key.Up)
            {
                calibration_manager.IsDebugWindowEnabled = !calibration_manager.IsDebugWindowEnabled;
            }
            if (e.Key == Key.Right && current_point_index + 1 < data_points.Count)
            {
                SetCurrentPointIndex(current_point_index + 1);
                e.Handled = true;
            }
        }

        private void OnCurrentPointChanged()
        {
            if (PresentationSource.FromVisual(Circle_Red) == null)
                return;

            if (current_point_index >= data_points.Count)
                return;

            Point true_location_on_screen = Canvas.PointFromScreen(data_points[current_point_index].true_location_on_screen);

            Canvas.SetTop(Circle_Red, true_location_on_screen.Y - 5);
            Canvas.SetLeft(Circle_Red, true_location_on_screen.X - 5);

            Point gaze_point = Canvas.PointFromScreen(
                new Point(
                    data_points[current_point_index].tobii_coordinates.gaze_point.X,
                    data_points[current_point_index].tobii_coordinates.gaze_point.Y));

            Canvas.SetTop(Circle_Green, gaze_point.Y - 5);
            Canvas.SetLeft(Circle_Green, gaze_point.X - 5);

            TextBlock_Info.Text = "Configuration " + configuration + " \n" +
                "point " + current_point_index + "/" + data_points.Count;

            calibration_manager.GetShift(data_points[current_point_index].tobii_coordinates.ToCoordinates(calibration_mode.additional_dimensions_configuration));
        }

        private void SetCurrentPointIndex(int new_current_point_index)
        {
            if (new_current_point_index < current_point_index)
            {
                calibration_manager.Reset();
                current_point_index = 0;
            }

            while (current_point_index < new_current_point_index)
            {
                Helpers.AddDataPoint(calibration_manager, data_points[current_point_index++], calibration_mode.additional_dimensions_configuration);
            }

            OnCurrentPointChanged();
        }
    }
}
