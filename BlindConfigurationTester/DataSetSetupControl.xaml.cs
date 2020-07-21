using Newtonsoft.Json;
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
            if (!Directory.Exists(DataSetBuilder.DataSetsFolder))
                Directory.CreateDirectory(DataSetBuilder.DataSetsFolder);

            InitializeComponent();
            UpdateCombobox(null, null);
            watcher = new FileSystemWatcher(DataSetBuilder.DataSetsFolder);
            watcher.Changed += OnDataSestsFolderChanged;
            watcher.Renamed += OnDataSestsFolderChanged;
            watcher.Created += OnDataSestsFolderChanged;
            watcher.Deleted += OnDataSestsFolderChanged;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
        }

        FileSystemWatcher watcher;
        public void OnDataSestsFolderChanged(object source, FileSystemEventArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                UpdateCombobox(null, null);
                if (Builder != null)
                    TextBlock_Info.Text = Builder.GetInfo();
            }));
        }

        private DataSet CurrentDataSet
        {
            get
            {
                return (Builder == null ? DataSet.LoadEverything() : DataSet.LoadSingleSection(Builder.name));
            }
        }

        private DataSetBuilder Builder
        {
            get; set;
        }

        private void UpdateCombobox(object sender, EventArgs args)
        {
            string current_DataSet = Combo_DataSet.SelectedItem?.ToString();
            Combo_DataSet.Items.Clear();
            if (!Directory.Exists(DataSetBuilder.DataSetsFolder))
                Directory.CreateDirectory(DataSetBuilder.DataSetsFolder);

            Combo_DataSet.Items.Add("ALL");

            foreach (var data_set in DataSetBuilder.ListDataSetsNames())
            {
                Combo_DataSet.Items.Add(data_set);
                if (data_set == current_DataSet || Combo_DataSet.Items.Count == 1)
                    Combo_DataSet.SelectedIndex = Combo_DataSet.Items.Count - 1;
            }
        }

        private void Button_Add_Click(object sender, RoutedEventArgs e)
        {
            var name_input = new NameInput();
            if (name_input.ShowDialog() == true && name_input.NameValue.Length > 0)
            {
                new DataSetBuilder(name_input.NameValue).SaveToFile();
            }
            UpdateCombobox(null, null);
        }

        private void Button_Remove_Click(object sender, RoutedEventArgs e)
        {
            if (Builder == null)
                return;
            if (MessageBox.Show("Sure?", "", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            Directory.Delete(Builder.DataSetResultsFolder, true);

            UpdateCombobox(null, null);
        }

        private void Combo_DataSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Combo_DataSet.SelectedItem == null)
            {
                Builder = null;
                TextBlock_Info.Text = "No selected DataSet";
                return;
            }

            if (Combo_DataSet.SelectedIndex == 0)
            {
                Builder = null;
                TextBlock_Info.Text = "";
                return;
            }

            Builder = DataSetBuilder.Load(Combo_DataSet.SelectedItem.ToString());

            if (Builder == null)
            {
                Builder = new DataSetBuilder(Combo_DataSet.SelectedItem.ToString());
                TextBlock_Info.Text = "Format error. Running session will reset the DataSet file.";
                return;
            }

            TextBlock_Info.Text = Builder.GetInfo();
            Button_StartSession.Content = "Start Session " + (Builder.number_of_completed_sessions + 1);
        }

        private void Button_TestConfigurationVisually_Click(object sender, RoutedEventArgs e)
        {
            var configuration_selection_dialog = new ConfigurationSelectionDialog();

            if (configuration_selection_dialog.ShowDialog() != true || Builder == null)
                return;

            ConfigurationTestVisualisationWindow data_visualisation_window =
                new ConfigurationTestVisualisationWindow(
                    configuration_selection_dialog.GetSelectedConfiguration(),
                    Builder.data_points);

            data_visualisation_window.ShowDialog();
        }

        private void Button_GenerateConfigurationManually_Click(object sender, RoutedEventArgs e)
        {
            new ManualVisualisationWindow.ManualVisualisationWindow().ShowDialog();
        }

        private void Button_TestConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfigurationSelectionDialog();

            if (dialog.ShowDialog() != true)
                return;

            var caibration_mode = Helpers.GetCalibrationMode(dialog.GetSelectedConfiguration());
            var calibration_manager = Helpers.SetupCalibrationManager(caibration_mode);

            var data_set = (Builder == null ? DataSet.LoadEverything() : DataSet.LoadSingleSection(Builder.name));

            int avg_time;
            var results = Helpers.RunPerfTest(
                calibration_manager,
                data_set, 
                caibration_mode.additional_dimensions_configuration, 
                out avg_time);

            calibration_manager.SaveInDirectory(Utils.DataFolder);

            string results_text = "";
            foreach(var item in results)
            {
                results_text += item.ToText();
            }

            MessageBox.Show(
                "Configuration: " + (dialog.GetSelectedConfiguration() ?? "User Data") + ". " + results_text + 
                ". \nAvg time:" + avg_time);
        }

        private void CreateConfiguration(
            eye_tracking_mouse.Options.CalibrationMode mode,
            List<UtilityAndModePair> linear_search_reasults,
            List<UtilityAndModePair> extremum_search_results)
        {
            string new_config = Utils.GenerateNewConfigurationName("gen") + "_" +
                Helpers.GetCombinedUtility(Helpers.TestCalibrationMode(CurrentDataSet, mode));

            Utils.CreateConfiguration(new_config);
            (new eye_tracking_mouse.Options
            {
                calibration_mode = mode
            }).SaveToFile(System.IO.Path.Combine(Utils.GetConfigurationDir(new_config), "options.json"));

            for (int i = 0; i < 2; i++)
            {
                File.WriteAllText(
                    System.IO.Path.Combine(
                        Utils.GetConfigurationDir(new_config), 
                        i == 0 ? "linear_results.json" : "linear_results_sorted.json"),
                    JsonConvert.SerializeObject(linear_search_reasults, Formatting.Indented));
                linear_search_reasults.Sort((x, y) => { return (int)((x.utility - y.utility) * 1000); }) ;
            }

            if (extremum_search_results== null)
                return;

            File.WriteAllText(
                System.IO.Path.Combine(
                    Utils.GetConfigurationDir(new_config),
                    "extremum_search_results.json"),
                JsonConvert.SerializeObject(extremum_search_results, Formatting.Indented));
        }


        private async void Button_GenerateConfigurationOnData_Click(object sender, RoutedEventArgs e)
        {
            var window = new CalibrationModeGeneratorWindow(CurrentDataSet);
            window.ShowDialog();

            var results = await window.GetResults();

            if (results.best_calibration_mode.mode == null)
                return;
            CreateConfiguration(
                results.best_calibration_mode.mode,
                results.linear_search_results,
                results.extremum_search_results);
        }


        private void Button_RunExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (Builder == null)
                return;
            System.Diagnostics.Process.Start(Builder.DataSetResultsFolder);
        }


        private void Button_StartSession_Click(object sender, RoutedEventArgs e)
        {
            if (Builder == null)
                return;

            Builder.StartTrainingSession();
        }
    }
}
