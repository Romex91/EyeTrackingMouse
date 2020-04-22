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
    public interface IControlModel
    {
        string Name { get; }
        string Value { get; }

        bool IsCheckboxChecked { get; set; }

        bool IsCheckboxVisible { get; }

        void Increment();
        void Decrement();
    }

    /// <summary>
    /// Interaction logic for OptionControl.xaml
    /// </summary>
    public partial class OptionControl : UserControl
    {
        IControlModel model;

        public OptionControl(
            IControlModel model)
        {
            this.model = model;
            InitializeComponent();
            Update();
        }

        public void Update()
        {
            Text_OptionName.Text = model.Name;
            Text_Value.Text = model.Value;

            CheckBox_Visualize.IsChecked = model.IsCheckboxChecked;
            CheckBox_Visualize.Visibility = model.IsCheckboxVisible ? Visibility.Visible : Visibility.Hidden;
        }

        private void Button_Increment_Click(object sender, RoutedEventArgs e)
        {
            model.Increment();
            Text_Value.Text = model.Value;
            Update();
        }

        private void Button_Decrement_Click(object sender, RoutedEventArgs e)
        {
            model.Decrement();
            Text_Value.Text = model.Value;
            Update();
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            model.IsCheckboxChecked = CheckBox_Visualize.IsChecked == true;
            Update();
        }
    }
}
