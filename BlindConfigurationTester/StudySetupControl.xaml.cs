using System;
using System.Collections.Generic;
using System.IO;
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

namespace BlindConfigurationTester
{
    /// <summary>
    /// Interaction logic for StudySetupControl.xaml
    /// </summary>
    public partial class StudySetupControl : UserControl
    {
        public StudySetupControl()
        {
            if (!Directory.Exists(Study.StudiesFolder))
            {
                Directory.CreateDirectory(Study.StudiesFolder);
            }

            InitializeComponent();
            UpdateCombobox(null, null);

            watcher = new FileSystemWatcher(Study.StudiesFolder);
            watcher.Changed += OnStudiesFolderChanged;
            watcher.Renamed += OnStudiesFolderChanged;
            watcher.Created += OnStudiesFolderChanged;
            watcher.Deleted += OnStudiesFolderChanged;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

        }

        FileSystemWatcher watcher;

        public Study SelectedStudy
        {
            get; set;
        }

        public void OnStudiesFolderChanged(object source, FileSystemEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                UpdateCombobox(null, null);
                if (SelectedStudy != null)
                    TextBlock_Info.Text = SelectedStudy.GetInfo();
            }));
        }

        private void UpdateCombobox(object sender, EventArgs args)
        {
            string current_study = Combo_Study.SelectedItem?.ToString();
            Combo_Study.Items.Clear();
            if (!Directory.Exists(Study.StudiesFolder))
                Directory.CreateDirectory(Study.StudiesFolder);
            string[] studies = Directory.GetDirectories(Study.StudiesFolder);
            for (int i = 0; i < studies.Length; i++)
            {
                studies[i] = Path.GetFileName(studies[i]);
            }

            foreach (var study in studies)
            {
                Combo_Study.Items.Add(study);
                if (study == current_study || Combo_Study.Items.Count == 1)
                    Combo_Study.SelectedIndex = Combo_Study.Items.Count - 1;
            }
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            var name_input = new NameInput();
            if (name_input.ShowDialog() == true && name_input.NameValue.Length > 0)
            {
                new Study(name_input.NameValue).SaveToFile();
            }
            UpdateCombobox(null, null);
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedStudy == null)
                return;
            if (MessageBox.Show("Sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            Directory.Delete(SelectedStudy.StudyResultsFolder, true);

            UpdateCombobox(null, null);
        }

        private void Combo_Study_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Combo_Study.SelectedItem == null)
            {
                SelectedStudy = null;
                TextBlock_Info.Text = "No selected study";
                return;
            }


            SelectedStudy = Study.Load(Combo_Study.SelectedItem.ToString());

            if (SelectedStudy == null)
            {
                SelectedStudy = new Study(Combo_Study.SelectedItem.ToString());
                TextBlock_Info.Text = "Format error. Running session will reset the study file.";
                return;
            }

            TextBlock_Info.Text = SelectedStudy.GetInfo();
            Button_StartSession.Content = "Start Session " + (SelectedStudy.number_of_completed_sessions + 1);
        }

        private void Button_RunExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedStudy == null)
                return;
            System.Diagnostics.Process.Start(SelectedStudy.StudyResultsFolder);
        }


        private void Button_StartSession_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedStudy == null)
                return;

            SelectedStudy.StartSession();
        }
    }
}
