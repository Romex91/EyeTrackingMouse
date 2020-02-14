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
    /// Interaction logic for StudyWindow.xaml
    /// </summary>
    public partial class StudyWindow : Window
    {
        private Study study;
        public StudyWindow(Study study)
        {
            InitializeComponent();
            this.study = study;
            UpdateDescription();
        }

        private void UpdateDescription()
        {

            TextBlock_Description.Text = "" +
                "Study #" + study.study_index + "\n" +
                study.description + "\n" +
                "Points per session: " + study.points_per_session + "\n" +
                "Number of sessions: " + study.number_of_completed_sessions + "/" + study.number_of_sessions + "\n" +
                "Configurations:\n";

            foreach (var configuration in study.configurations)
            {
                TextBlock_Description.Text +=
                    (configuration.name == null ? "User Data" : configuration.name) + ". Saving changes? " +
                    (configuration.save_changes ? "Yes" : "No") + "\n";
            }

        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Button_Abort_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            study.Abort();
            Closing -= WindowClosing;
            Close();
        }

        private void Button_Quit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Button_StartSession_Click(object sender, RoutedEventArgs e)
        {
            study.StartSession();
            if (study.IsFinished)
            {
                Closing -= WindowClosing;
                Close();
            }
        }
    }
}
