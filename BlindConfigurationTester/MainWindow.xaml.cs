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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, EventArgs args)
        {
            Study study = Study.LoadUnfinished();
            if (study != null && !study.IsFinished)
            {
                var window = new StudyWindow(study);

                window.ShowDialog();
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Button_Run_Click(object sender, RoutedEventArgs e)
        {
            int points_per_session = 0;
            int number_of_sessions = 0;
            int size_of_circle = 0;

            int.TryParse(TextBox_PointsPerSession.Text, out points_per_session);
            int.TryParse(TextBox_NumberOfSessions.Text, out number_of_sessions);
            int.TryParse(TextBox_SizeOfCircle.Text, out size_of_circle);

            List<Configuration> configurations = new List<Configuration>();
            configurations.Add(new Configuration
            {
                name = Configuration_A.GetSelectedConfiguration(),
                save_changes = Configuration_A.Checkbox_SaveProgress.IsChecked == true
            });
            if (Combobox_Mode.SelectedIndex == 1)
            {
                configurations.Add(new Configuration
                {
                    name = Configuration_B.GetSelectedConfiguration(),
                    save_changes = Configuration_B.Checkbox_SaveProgress.IsChecked == true
                });
            }

            Study study = new Study(points_per_session, number_of_sessions, size_of_circle, TextBox_Description.Text, configurations);
            (new StudyWindow(study)).ShowDialog();
        }
    }
}
