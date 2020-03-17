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
    /// Interaction logic for Vector3BoolCheckboxControl.xaml
    /// </summary>
    public partial class Vector3BoolCheckboxControl : UserControl
    {
        public EventHandler ValueChanged { get; set; }

        public Vector3BoolCheckboxControl()
        {
            InitializeComponent();
        }

        private void CheckBox_Changed(object sender, EventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        public Vector3Percents Value
        {
            get
            {
                return new Vector3Percents
                {
                    X = (X.IsChecked == true ? 700 : 0),
                    Y = (Y.IsChecked == true ? 700 : 0),
                    Z = (Z.IsChecked == true ? 700 : 0)
                };
            }
            set
            {
                X.IsChecked = value.X > 0;
                Y.IsChecked = value.Y > 0;
                Z.IsChecked = value.Z > 0;
            }
        }
    }
}
