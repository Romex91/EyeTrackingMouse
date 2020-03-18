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
using System.Windows.Shapes;

namespace BlindConfigurationTester
{
    /// <summary>
    /// Interaction logic for DataSetSetupControl.xaml
    /// </summary>
    public partial class DataSetSetupControl : UserControl
    {
        public DataSetSetupControl()
        {
            InitializeComponent();
            UpdateCombobox(null, null);
            watcher.Changed += OnDataSestsFolderChanged;
            watcher.Renamed += OnDataSestsFolderChanged;
            watcher.Created += OnDataSestsFolderChanged;
            watcher.Deleted += OnDataSestsFolderChanged;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        FileSystemWatcher watcher = new FileSystemWatcher(DataSet.DataSetsFolder);
        public void OnDataSestsFolderChanged(object source, FileSystemEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                UpdateCombobox(null, null);
                if (SelectedDataSet != null)
                    TextBlock_Info.Text = SelectedDataSet.GetInfo();
            }));
        }
        private DataSet SelectedDataSet
        {
            get; set;
        }

        private void UpdateCombobox(object sender, EventArgs args)
        {
            string current_DataSet = Combo_DataSet.SelectedItem?.ToString();
            Combo_DataSet.Items.Clear();
            if (!Directory.Exists(DataSet.DataSetsFolder))
                Directory.CreateDirectory(DataSet.DataSetsFolder);
            string[] studies = Directory.GetDirectories(DataSet.DataSetsFolder);
            for (int i = 0; i < studies.Length; i++)
            {
                studies[i] = System.IO.Path.GetFileName(studies[i]);
            }

            foreach (var DataSet in studies)
            {
                Combo_DataSet.Items.Add(DataSet);
                if (DataSet == current_DataSet || Combo_DataSet.Items.Count == 1)
                    Combo_DataSet.SelectedIndex = Combo_DataSet.Items.Count - 1;
            }
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            var name_input = new NameInput();
            if (name_input.ShowDialog() == true && name_input.NameValue.Length > 0)
            {
                new DataSet(name_input.NameValue).SaveToFile();
            }
            UpdateCombobox(null, null);
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDataSet == null)
                return;
            if (MessageBox.Show("Sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            Directory.Delete(SelectedDataSet.DataSetResultsFolder, true);

            UpdateCombobox(null, null);
        }

        private void Combo_DataSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Combo_DataSet.SelectedItem == null)
            {
                SelectedDataSet = null;
                TextBlock_Info.Text = "No selected DataSet";
                return;
            }

            SelectedDataSet = DataSet.Load(Combo_DataSet.SelectedItem.ToString());

            if (SelectedDataSet == null)
            {
                SelectedDataSet = new DataSet(Combo_DataSet.SelectedItem.ToString());
                TextBlock_Info.Text = "Format error. Running session will reset the DataSet file.";
                return;
            }

            TextBlock_Info.Text = SelectedDataSet.GetInfo();
            Button_StartSession.Content = "Start Session " + (SelectedDataSet.number_of_completed_sessions + 1);
        }

        private void Button_TestConfigurationVisually_Click(object sender, RoutedEventArgs e)
        {
            var configuration_selection_dialog = new ConfigurationSelectionDialog();

            if (configuration_selection_dialog.ShowDialog() != true || SelectedDataSet == null)
                return;

            ConfigurationTestVisualisationWindow data_visualisation_window =
                new ConfigurationTestVisualisationWindow(
                    configuration_selection_dialog.GetSelectedConfiguration(),
                    SelectedDataSet.data_points);

            data_visualisation_window.ShowDialog();
        }

        private void Button_TestConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfigurationSelectionDialog();

            if (dialog.ShowDialog() != true || SelectedDataSet == null)
                return;

            var calibration_manager = Helpers.SetupCalibrationManager(dialog.GetSelectedConfiguration());
            var result = Helpers.TestCalibrationManager(calibration_manager, SelectedDataSet.data_points);
            eye_tracking_mouse.CalibrationManager.Instance.SaveInDirectory(Utils.DataFolder);

            using (var writer = new StreamWriter(
                System.IO.Path.Combine(SelectedDataSet.DataSetResultsFolder,
                (dialog.GetSelectedConfiguration() ?? "User Data") + ".csv")))
            {
                using (var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(result.errors);
                }
            }

            MessageBox.Show("Configuration: "+ (dialog.GetSelectedConfiguration() ?? "User Data") + ". Utility: " + result.UtilityFunction);
        }

        private void Button_GenerateConfigurationOnData_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDataSet == null)
                return;
            var options = new eye_tracking_mouse.Options();
            options.calibration_mode = CalibrationModeGenerator.GenerateConfiguration(SelectedDataSet.data_points);
            options.SaveToFile(System.IO.Path.Combine(SelectedDataSet.DataSetResultsFolder, "options.json"));
        }

        private void Button_RunExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDataSet == null)
                return;
            System.Diagnostics.Process.Start(SelectedDataSet.DataSetResultsFolder);
        }


        private void Button_StartSession_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDataSet == null)
                return;

            SelectedDataSet.StartTrainingSession();
        }
    }
}
