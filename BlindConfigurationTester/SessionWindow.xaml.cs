using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for SessionWindow.xaml
    /// </summary>
    public partial class SessionWindow : Window
    {
        List<Tuple<int, int>> points;
        int current_index = 0;
        bool dataset_training_mode = false;

        DateTime time_when_user_started_looking_at_point = DateTime.Now;
        bool is_user_looking_at_point = false;
        float current_angle = 0;
        float current_angle_delta = 0;
        int size_of_circle;

        eye_tracking_mouse.TobiiCoordinatesProvider coordinatesProvider;

        public List<DataPoint> DataPoints { get; private set; }
        public List<DataPoint> SmoothenedDataPoints { get; private set; }

        public SessionWindow(List<Tuple<int, int>> points, int size_of_circle, bool dataset_training_mode)
        {
            eye_tracking_mouse.Options.Instance.calibration_mode.additional_dimensions_configuration = new eye_tracking_mouse.AdditionalDimensionsConfguration
            {
                HeadDirection = new eye_tracking_mouse.Vector3Percents { X = 70, Y = 70, Z = 70 },
                HeadPosition = new eye_tracking_mouse.Vector3Percents { X = 70, Y = 70, Z = 70 },
                LeftEye = new eye_tracking_mouse.Vector3Percents { X = 70, Y = 70, Z = 70 },
                RightEye = new eye_tracking_mouse.Vector3Percents { X = 70, Y = 70, Z = 70 },
                AngleBetweenEyes = new eye_tracking_mouse.Vector3Percents { X = 70, Y = 70, Z = null }
            };

            this.points = points;
            InitializeComponent();
            Circle.Width = size_of_circle * (dataset_training_mode ? 2: 1);
            Circle.Height = size_of_circle;

            this.dataset_training_mode = dataset_training_mode;
            if (dataset_training_mode)
                DataPoints = new List<DataPoint>();

            this.size_of_circle = size_of_circle;

            Dispatcher.BeginInvoke((Action)(() => { NextPoint(null, null); }));
        }

        private void OnCoordinates(eye_tracking_mouse.TobiiCoordinates coordinates)
        {
            coordinatesProvider.Restart();
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Point gaze_point = new Point(coordinates.gaze_point.X, coordinates.gaze_point.Y);
                if (PresentationSource.FromVisual(Circle) == null)
                    return;

                Point location_of_point_on_screen = this.Circle.PointToScreen(new Point(size_of_circle, size_of_circle / 2));

                if (Point.Subtract(gaze_point, location_of_point_on_screen).Length < 100)
                {
                    if (!is_user_looking_at_point)
                    {
                        is_user_looking_at_point = true;
                        time_when_user_started_looking_at_point = DateTime.Now;
                    }

                    Circle.RenderTransform = new RotateTransform(current_angle, size_of_circle, size_of_circle / 2);
                    if ((DateTime.Now - time_when_user_started_looking_at_point).TotalMilliseconds > 500)
                    {
                        current_angle_delta += 0.4f;
                        if (current_angle_delta > 10)
                            current_angle_delta = 10;
                    } else
                    {
                        current_angle_delta -= 0.4f;
                        if (current_angle_delta < 0)
                            current_angle_delta = 0;
                    }
                    current_angle += current_angle_delta;

                    if ((DateTime.Now - time_when_user_started_looking_at_point).TotalMilliseconds > 1000)
                    {
                        DataPoints.Add(new DataPoint
                        {
                            true_location_on_screen = location_of_point_on_screen,
                            tobii_coordinates = coordinates
                        });

                        is_user_looking_at_point = false;

                        var dt = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Send);
                        dt.Tick += (s, e) =>
                        {
                            dt.Stop();
                            is_user_looking_at_point = false;
                            NextPoint(null, null);
                        };
                        dt.Interval = TimeSpan.FromMilliseconds(450);
                        dt.Start();
                    }

                }
                else
                {
                    is_user_looking_at_point = false;
                }
            }));

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (current_index != points.Count)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Aborting this session will fuck up scientific objectivity!\n" +
                    "Do you really want to do that?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            coordinatesProvider?.Dispose();
        }

        public void NextPoint(object sender, EventArgs args)
        {
            if (current_index == points.Count)
            {
                Close();
                return;
            }

            Circle.SetValue(Canvas.LeftProperty, points[current_index].Item1 % (Canvas.ActualWidth - 20));
            Circle.SetValue(Canvas.TopProperty, points[current_index].Item2 % (Canvas.ActualHeight - 20));

            TextBlock_PointsLeft.Text = ++current_index + "/" + points.Count;

            if (dataset_training_mode && coordinatesProvider == null)
            {
                coordinatesProvider = new eye_tracking_mouse.TobiiCoordinatesProvider(OnCoordinates);
                coordinatesProvider.UpdateTobiiStreams(null, null);
            }
        }
    }
}
