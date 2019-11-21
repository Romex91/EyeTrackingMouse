using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// Interaction logic for CalibrationWindow.xaml
    /// </summary>
    public partial class CalibrationWindow : Window
    {

        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        private readonly List<Petzold.Media2D.ArrowLine> arrows = new List<Petzold.Media2D.ArrowLine>();

        private void Update(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lock (Helpers.locker)
                {
                    foreach (var arrow in arrows)
                    {
                        Canvas.Children.Remove(arrow);
                    }

                    arrows.Clear();

                    foreach (var shift in ShiftsStorage.Instance.Shifts)
                    {
                        var arrow = new Petzold.Media2D.ArrowLine();
                        arrow.X1 = shift.Position.X;
                        arrow.Y1 = shift.Position.Y;
                        arrow.X2 = shift.Position.X + shift.Shift.X;
                        arrow.Y2 = shift.Position.Y + shift.Shift.Y;
                        arrow.Stroke = Options.Instance.calibration.multidimension_calibration_type == MultidimensionCalibrationType.None ?
                            Brushes.Red : new SolidColorBrush(shift.Position.GetColor());
                        arrow.StrokeThickness = 3;

                        Canvas.Children.Add(arrow);
                        arrows.Add(arrow);
                    }

                    Description.Text =
                        "CALIBRATIONS COUNT: " + ShiftsStorage.Instance.Shifts.Count + "/" + Options.Instance.calibration.max_zones_count + " \n" +
                        "HIDE CALIBRATION VIEW: " + Options.Instance.key_bindings[Key.Modifier].ToString().ToUpper() + "+" + Options.Instance.key_bindings[Key.ShowCalibrationView] + "\n" +
                        "YOU CAN RESET CALIBRATIONS VIA TRAY ICON MENU";
                }
            }));
        }

        private void UpdateColor(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lock (Helpers.locker)
                {

                    MultidimensionCalibrationType type = Options.Instance.calibration.multidimension_calibration_type;

                    if (type == MultidimensionCalibrationType.None)
                    {
                        HeadPositionDescription.Text = "";
                        CurrentHeadPositionColor.Background = Brushes.Red;
                        return;
                    }

                    CurrentHeadPositionColor.Background = new SolidColorBrush(ShiftsStorage.Instance.LastPosition.GetColor());

                    string head_position_description = "";
                    var last_position = ShiftsStorage.Instance.LastPosition;

                    if ((type & MultidimensionCalibrationType.HeadPosition) != MultidimensionCalibrationType.None)
                    {
                        head_position_description += "Head position: \nX: " + last_position.HeadPosition.X + "\n" + "Y: " + last_position.HeadPosition.Y + "\n" + "Z: " + last_position.HeadPosition.Z;
                    }

                    if ((type & MultidimensionCalibrationType.HeadDirection) != MultidimensionCalibrationType.None)
                    {
                        head_position_description += "\nHead direction: \n Pitch: " + last_position.HeadDirection.X + "\n" + "Yaw: " + last_position.HeadDirection.Y + "\n" + "Roll: " + last_position.HeadDirection.Z;
                    }

                    if ((type & MultidimensionCalibrationType.LeftEye) != MultidimensionCalibrationType.None)
                    {
                        head_position_description += "\nLeft eye: \nX: " + last_position.LeftEye.X + "\n" + "Y: " + last_position.LeftEye.Y + "\n" + "Z: " + last_position.LeftEye.Z;
                    }

                    if ((type & MultidimensionCalibrationType.RightEye) != MultidimensionCalibrationType.None)
                    {
                        head_position_description += "\nRight eye: \nX: " + last_position.LeftEye.X + "\n" + "Y: " + last_position.LeftEye.Y + "\n" + "Z: " + last_position.LeftEye.Z;
                    }

                    HeadPositionDescription.Text = head_position_description;
                }
            }));
        }

        public CalibrationWindow()
        {
            InitializeComponent();
            Update(null, null);
            ShiftsStorage.Changed += Update;
            Settings.KeyBindingsChanged += Update;
            ShiftsStorage.CursorPositionUpdated += UpdateColor;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            ShiftsStorage.Changed -= Update;
            Settings.KeyBindingsChanged -= Update;
            ShiftsStorage.CursorPositionUpdated -= UpdateColor;
            base.OnClosed(e);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowExTransparent(hwnd);
        }
    }
}
