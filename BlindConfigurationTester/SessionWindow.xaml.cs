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
    /// Interaction logic for SessionWindow.xaml
    /// </summary>
    public partial class SessionWindow : Window
    {
        List<Tuple<int, int>> points;
        int current_index = 0;

        public SessionWindow(List<Tuple<int,int>> points, int size_of_circle)
        {
            this.points = points;
            InitializeComponent();
            Circle.Width = size_of_circle;
            Circle.Height = size_of_circle;

            Dispatcher.BeginInvoke((Action)(()=>{ NextPoint(null, null); }));
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (current_index == points.Count)
                return;
            MessageBoxResult result = MessageBox.Show(
                "Aborting this session will fuck up scientific objectivity of the study!\n" +
                "Do you really want to do that?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
        public void NextPoint(object sender, EventArgs args)
        {
            if (current_index == points.Count)
            {
                Close();
                return;
            }

            Circle.SetValue(Canvas.LeftProperty, points[current_index].Item1 % Canvas.ActualWidth);
            Circle.SetValue(Canvas.TopProperty, points[current_index].Item2 % Canvas.ActualHeight);

            TextBlock_PointsLeft.Text = ++current_index + "/" + points.Count;
        }
    }
}
