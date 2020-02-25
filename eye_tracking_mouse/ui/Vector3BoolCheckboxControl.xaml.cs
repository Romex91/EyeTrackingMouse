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

        public Vector3Bool Value
        {
            get
            {
                return new Vector3Bool { X = X.IsChecked == true, Y = Y.IsChecked == true, Z = Z.IsChecked == true };
            }
            set
            {
                X.IsChecked = value.X;
                Y.IsChecked = value.Y;
                Z.IsChecked = value.Z;
            }
        }
    }
}
