using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for YoutubeIndicationsWindow.xaml
    /// </summary>
    public partial class AccessibilityHelperWindow : Window
    {
        private double dpiX = 1, dpiY = 1;

        private System.Drawing.Point calibration_start_point = System.Windows.Forms.Cursor.Position;
        private Petzold.Media2D.ArrowLine calibration_arrow = new Petzold.Media2D.ArrowLine { Stroke = Brushes.Red, StrokeThickness = 3, Visibility = Visibility.Hidden };

        public AccessibilityHelperWindow()
        {
            InitializeComponent();

            CompositionTarget.Rendering += OnRendering;

            KeyBindings.Changed += OnKeyBindignsChanged;
            OnKeyBindignsChanged(null, null);

            Canvas.Children.Add(calibration_arrow);
        }

        public void ShowCalibration(System.Drawing.Point starting_point)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                calibration_arrow.Visibility = Visibility.Visible;
                calibration_start_point = starting_point;
                TxtSaved.Visibility = Visibility.Visible;
            }));
        }

        public void HideCalibration()
        {
            Dispatcher.BeginInvoke((Action)(async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300));
                calibration_arrow.Visibility = Visibility.Hidden;
                TxtSaved.Visibility = Visibility.Hidden;
            }));
        }

        public new void Show()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                this.OnRendering(null, null);
                base.Show();
            }));
        }

        public new void Hide()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                base.Hide();
            }));
        }

        private void OnRendering(object sender, EventArgs e)
        {
            var pos = System.Windows.Forms.Cursor.Position;

            Canvas.SetLeft(Instructions, pos.X / dpiX);
            Canvas.SetTop(Instructions, pos.Y / dpiY);

            calibration_arrow.X1 = calibration_start_point.X / dpiX;
            calibration_arrow.Y1 = calibration_start_point.Y / dpiY;

            calibration_arrow.X2 = pos.X / dpiX;
            calibration_arrow.Y2 = pos.Y / dpiY;
        }

        private void OnKeyBindignsChanged(object sender, EventArgs e)
        {
            lock (Helpers.locker)
            {
                Dictionary<Key, Run> key_to_letter_dictionary = new Dictionary<Key, Run> {
                    { Key.LeftMouseButton, TxtLeftMouseButton },
                    { Key.RightMouseButton, TxtRightMouseButton },
                    { Key.ScrollDown, TxtScrollDown },
                    { Key.ScrollUp, TxtScrollUp},
                    { Key.ScrollLeft, TxtScrollLeft},
                    { Key.ScrollRight, TxtScrollRight},
                    { Key.CalibrateDown, TxtCalibrateDown},
                    { Key.CalibrateUp, TxtCalibrateUp },
                    { Key.CalibrateRight, TxtCalibrateRight},
                    { Key.CalibrateLeft , TxtCalibrateLeft},
                    { Key.Modifier, TxtModifier},
                    { Key.StopCalibration, TxtExit},
                    { Key.Accessibility_SaveCalibration, TxtSaveCalibration}
                };

                var bindings = Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.OblitaDriver ?
                    Options.Instance.key_bindings.bindings : KeyBindings.default_bindings;
                foreach ( var item in key_to_letter_dictionary)
                {
                    item.Value.Text = bindings[item.Key].ToString();
                    if (item.Value.Text == Interceptor.Keys.CommaLeftArrow.ToString())
                        item.Value.Text = "<";
                    if (item.Value.Text == Interceptor.Keys.PeriodRightArrow.ToString())
                        item.Value.Text = ">";
                    if (item.Value.Text == Interceptor.Keys.CapsLock.ToString())
                        item.Value.Text = "CapsLk";
                    if (item.Value.Text == Interceptor.Keys.WindowsKey.ToString())
                        item.Value.Text = "Win";
                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        private void Window_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            dpiX = e.NewDpi.DpiScaleX;
            dpiY = e.NewDpi.DpiScaleY;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            KeyBindings.Changed -= OnKeyBindignsChanged;
        }
    }
}
