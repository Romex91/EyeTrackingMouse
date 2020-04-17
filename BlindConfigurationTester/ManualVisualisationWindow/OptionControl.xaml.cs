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

namespace BlindConfigurationTester.ManualVisualisationWindow
{
    /// <summary>
    /// Interaction logic for OptionControl.xaml
    /// </summary>
    public partial class OptionControl : UserControl
    {
        public CalibrationModeIterator.OptionsField Field;
        private CalibrationModeIterator iterator;
        private Action redraw;

        public OptionControl(
            CalibrationModeIterator.OptionsField field,
            CalibrationModeIterator iterator,
            Action redraw_callback)
        {
            InitializeComponent();
            this.Field = field;
            this.iterator = iterator;
            redraw = redraw_callback;
            Text_OptionName.Text = field.field_name;
            Text_Value.Text = field.GetFieldValue(iterator.CalibrationMode).ToString();
            if (field.field_name == "coordinate 2" || field.field_name == "coordinate 3")
                CheckBox_Visualize.IsChecked = true;
        }

        private void Button_Increment_Click(object sender, RoutedEventArgs e)
        {
            if (!Field.Increment(iterator.CalibrationMode, 1))
                return;
            Text_Value.Text = Field.GetFieldValue(iterator.CalibrationMode).ToString();
            redraw.Invoke();
        }

        private void Button_Decrement_Click(object sender, RoutedEventArgs e)
        {
            if (!Field.Increment(iterator.CalibrationMode, -1))
                return;
            Text_Value.Text = Field.GetFieldValue(iterator.CalibrationMode).ToString();
            redraw.Invoke();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            redraw.Invoke();
        }
    }
}
