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

namespace BlindConfigurationTester
{
    /// <summary>
    /// Interaction logic for ConfigurationControl.xaml
    /// </summary>
    public partial class ConfigurationControl : UserControl
    {
        public static EventHandler on_configurations_changed;
        public ConfigurationControl()
        {
            InitializeComponent();
            on_configurations_changed += UpdateCombobox;
            UpdateCombobox(this, null);
            var parent = Window.GetWindow(this);
            if (parent != null) parent.Closing += OnParentWindowClose;
        }

        public string GetSelectedConfiguration()
        {
            return Combo_Configuration.SelectedIndex == 0 ? null : Combo_Configuration.SelectedItem.ToString();
        }

        private void OnParentWindowClose(object sender, EventArgs args)
        {
            on_configurations_changed -= UpdateCombobox;
        }
        
        private void UpdateCombobox(object sender, EventArgs args)
        {
            string current_configuration = GetSelectedConfiguration();

            string[] configurations = Utils.GetConfigurationsList();

            Combo_Configuration.Items.Clear();
            Combo_Configuration.Items.Add("User Data");
            Combo_Configuration.SelectedIndex = 0;

            foreach (var configuration in configurations)
            {
                Combo_Configuration.Items.Add(configuration);
                if (configuration == current_configuration)
                    Combo_Configuration.SelectedIndex = Combo_Configuration.Items.Count - 1;
            }
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            var name_input = new ConfigurationNameInput();
            if( name_input.ShowDialog() == true)
            {
                Utils.CreateConfiguration(name_input.ConfigurationName);
            }

            on_configurations_changed.Invoke(this, null);

            for (int i = 1; i < Combo_Configuration.Items.Count; i++)
            {
                if (Combo_Configuration.Items[i].ToString() == name_input.ConfigurationName)
                    Combo_Configuration.SelectedIndex = i;
            }
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
           
            Utils.RemoveConfiguration(GetSelectedConfiguration());
            on_configurations_changed.Invoke(this, null);
        }

        private void Button_SaveToUserData_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            Utils.SaveToUserData(GetSelectedConfiguration());
        }

        private void Button_LoadFromUserData_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            Utils.LoadFromUserData(GetSelectedConfiguration());
        }

        private void Button_ConfigureFromApp_Click(object sender, RoutedEventArgs e)
        {
            Utils.RunApp(GetSelectedConfiguration(), true, null, () => {
                while (Utils.IsApplicationOpen())
                {
                    MessageBox.Show(
                        eye_tracking_mouse.Helpers.application_name + " is running with selected configuration. Close it to proceed.");
                }
            });
        }
    }
}
