using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    class ColorCalculator
    {
        public void AdjustColorBoundaries(float[] coordinates)
        {
            var color = GetColorComponents(coordinates);
            if (max_color.X < color.X)
                max_color.X = color.X;

            if (max_color.Y < color.Y)
                max_color.Y = color.Y;

            if (max_color.Z < color.Z)
                max_color.Z = color.Z;

            if (min_color.X > color.X)
                min_color.X = color.X;

            if (min_color.Y > color.Y)
                min_color.Y = color.Y;

            if (min_color.Z > color.Z)
                min_color.Z = color.Z;
        }

        public System.Windows.Media.Color GetColor(float[] coordinates)
        {
            var color_components = GetColorComponents(coordinates);
            AdjustColorBoundaries(coordinates);
            return System.Windows.Media.Color.FromArgb(
                255, (byte)((color_components.X - min_color.X) / (max_color.X - min_color.X) * 254),
                (byte)((color_components.Y - min_color.Y) / (max_color.Y - min_color.Y) * 254),
                (byte)((color_components.Z - min_color.Z) / (max_color.Z - min_color.Z) * 254));
        }

        private Tobii.Interaction.Vector3 min_color = new Tobii.Interaction.Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        private Tobii.Interaction.Vector3 max_color = new Tobii.Interaction.Vector3(float.MinValue, float.MinValue, float.MinValue);

        private Tobii.Interaction.Vector3 GetColorComponents(float[] coordinates)
        {
            var color_components = new Tobii.Interaction.Vector3(0, 0, 0);
            for (int i = 2; i < coordinates.Length; i++)
            {
                int component_index = (i - 2) % 3;
                switch (component_index)
                {
                    case 0: color_components.X += coordinates[i]; break;
                    case 1: color_components.Y += coordinates[i]; break;
                    case 2: color_components.Z += coordinates[i]; break;
                    default: Debug.Assert(false); break;
                }
            }

            return color_components;
        }
    }

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
        private readonly Petzold.Media2D.ArrowLine current_arrow = new Petzold.Media2D.ArrowLine();
        private List<TextBlock> arrows_lables = new List<TextBlock>();
        private readonly ColorCalculator color_calculator = new ColorCalculator();

        private int shifts_count = 0;
        public void UpdateText(object sender, EventArgs args)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                lock (Helpers.locker)
                {
                    Description.Text =
                            "CALIBRATIONS COUNT: " + shifts_count + "/" + Options.Instance.calibration_mode.max_zones_count + " \n" +
                            "HIDE CALIBRATION VIEW: " + Helpers.GetModifierString().ToUpper() +
                                "+" + Options.Instance.key_bindings[Key.ShowCalibrationView] + "\n" +
                            "YOU CAN RESET CALIBRATIONS FROM THE TRAY ICON MENU";
                }
            }));
        }

        public void UpdateCorrectionsLables(List<Tuple<string /*text*/, Point /*correction index*/>> lables)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                foreach (var lable in arrows_lables)
                    Canvas.Children.Remove(lable);
                arrows_lables.Clear();

                foreach (var lable in lables)
                {
                        var arrow_lable = new TextBlock { Text = lable.Item1, Visibility = Visibility.Visible };
                        Canvas.Children.Add(arrow_lable);

                        Canvas.SetTop(arrow_lable, lable.Item2.Y);
                        Canvas.SetLeft(arrow_lable, lable.Item2.X);
                        arrows_lables.Add(arrow_lable);
                }
            }));
        }

        public void UpdateCurrentCorrection(UserCorrection correction)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                color_calculator.AdjustColorBoundaries(correction.Coordinates);
                InitArrowWithUserCorrection(correction, current_arrow);
            }));
        }

        private void InitArrowWithUserCorrection(UserCorrection shift, Petzold.Media2D.ArrowLine arrow)
        {
            arrow.X1 = shift.Coordinates[0];
            arrow.Y1 = shift.Coordinates[1];
            arrow.X2 = shift.Coordinates[0] + shift.Shift.X;
            arrow.Y2 = shift.Coordinates[1] + shift.Shift.Y;
            if (arrow == current_arrow)
            {
                arrow.Stroke = Brushes.Green;
            }
            else
            {
                arrow.Stroke = Options.Instance.calibration_mode.additional_dimensions_configuration.Equals(AdditionalDimensionsConfguration.Disabled) ?
                    Brushes.Red : new SolidColorBrush(color_calculator.GetColor(shift.Coordinates));
            }
            arrow.StrokeThickness = 3;
        }

        public void UpdateCorrections(List<UserCorrection> shifts)
        {
            lock (Helpers.locker)
            {
                shifts_count = shifts.Count;
                UpdateText(null, null);
                Dispatcher.Invoke(new Action(() =>
                {
                    foreach (var arrow in arrows)
                    {
                        Canvas.Children.Remove(arrow);
                    }

                    arrows.Clear();

                    foreach (var shift in shifts)
                    {
                        color_calculator.AdjustColorBoundaries(shift.Coordinates);
                    }

                    foreach (var shift in shifts)
                    {
                        var arrow = new Petzold.Media2D.ArrowLine();

                        InitArrowWithUserCorrection(shift, arrow);
                        Canvas.Children.Add(arrow);
                        arrows.Add(arrow);
                    }
                }));
            }
        }

        public void OnCursorPositionUpdate(float[] cursor_position)
        {
            string head_position_description = "";
            Color color = Colors.Red;

            lock (Helpers.locker)
            {
                AdditionalDimensionsConfguration configuration = Options.Instance.calibration_mode.additional_dimensions_configuration;
                if (!configuration.Equals(AdditionalDimensionsConfguration.Disabled))
                {
                    color = color_calculator.GetColor(cursor_position);
                    int coordinate_index = 2;
                    foreach (var vector3 in new List<Tuple<string, Vector3Percents>> {
                        new Tuple<string, Vector3Percents> ("Left eye", configuration.LeftEye),
                        new Tuple<string, Vector3Percents> ("Right eye", configuration.RightEye),
                        new Tuple<string, Vector3Percents> ("Angle between eyes", configuration.AngleBetweenEyes),
                        new Tuple<string, Vector3Percents> ("Head direction", configuration.HeadDirection),
                        new Tuple<string, Vector3Percents> ("Head position", configuration.HeadPosition)
                    })
                    {
                        if (!vector3.Item2.Equals(Vector3Percents.Disabled))
                            head_position_description += vector3.Item1 + " \n";
                        if (vector3.Item2.X > 0)
                        {
                            head_position_description += "X: " + (int)cursor_position[coordinate_index++] + "\n";
                        }
                        if (vector3.Item2.Y > 0)
                        {
                            head_position_description += "Y: " + (int)cursor_position[coordinate_index++] + "\n";
                        }
                        if (vector3.Item2.Z > 0)
                        {
                            head_position_description += "Z: " + (int)cursor_position[coordinate_index++] + "\n";
                        }
                    }
                }
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                CurrentHeadPositionColor.Background = new SolidColorBrush(color);
                HeadPositionDescription.Text = head_position_description;
            }));
        }

        public CalibrationWindow()
        {
            InitializeComponent();

            Canvas.Children.Add(current_arrow);
            KeyBindings.Changed += UpdateText;
            Options.Changed += UpdateText;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            KeyBindings.Changed -= UpdateText;
            Options.Changed -= UpdateText;
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
