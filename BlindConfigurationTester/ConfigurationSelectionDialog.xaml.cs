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
    /// Interaction logic for ConfigurationSelectionDialog.xaml
    /// </summary>
    public partial class ConfigurationSelectionDialog : Window
    {
        public ConfigurationSelectionDialog()
        {
            InitializeComponent();

            string[] configurations = Utils.GetConfigurationsList();

            Combo_Configuration.Items.Clear();
            Combo_Configuration.Items.Add("User Data");
            Combo_Configuration.SelectedIndex = 0;

            foreach (var configuration in configurations)
            {
                Combo_Configuration.Items.Add(configuration);
            }
        }

        public string GetSelectedConfiguration()
        {
            return Combo_Configuration.SelectedIndex == 0 ? null : Combo_Configuration.SelectedItem.ToString();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
