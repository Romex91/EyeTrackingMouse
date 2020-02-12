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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for IntegerPropertyControl.xaml
    /// </summary>
    public partial class IntegerPropertyControl : UserControl
    {
        public IntegerPropertyControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ValueChanged?.Invoke(this, e);
        }
        public RoutedPropertyChangedEventHandler<double> ValueChanged { get; set; }

        public string Label
        {
            get { return TextBlock.Text; }
            set { TextBlock.Text = value; }
        }

        public int Minimum
        {
            get { return (int)Slider.Minimum; }
            set { Slider.Minimum = value; }
        }
        public int Maximum
        {
            get { return (int)Slider.Maximum; }
            set { Slider.Maximum = value; }
        }
        public int Value { 
            get { return (int)Slider.Value; } 
            set{ Slider.Value = value; } 
        }
    }
}
