using Newtonsoft.Json;
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
using AwokeKnowing.GnuplotCSharp;
using System.Threading;
using System.Linq.Expressions;

namespace BlindConfigurationTester.ManualVisualisationWindow
{
    class DataSetControlModel : IControlModel
    {
        string[] names;
        List<DataPoint>[] data_sets;
        int selected_data_set = 0;
        private Action redraw;

        public List<DataPoint> DataPoints
        {
            get { return data_sets[selected_data_set]; }
        }

        public DataSetControlModel(Action redraw_callback)
        {
            redraw = redraw_callback;
            names = DataSet.ListDataSetsNames();
            data_sets = new List<DataPoint>[names.Length];
            for(int i = 0; i < names.Length; i++)
            {
                data_sets[i] = DataSet.Load(names[i]).data_points;
            }
        }

        public string Name => "data_set";

        public string Value => names[selected_data_set];

        public bool IsCheckboxChecked { get => false; set => throw new NotImplementedException(); }

        public bool IsCheckboxVisible => false;

        public void Decrement()
        {
            if (selected_data_set > 0)
                selected_data_set--;
            redraw.Invoke();
        }

        public void Increment()
        {
            if (selected_data_set < names.Length - 1)
              selected_data_set++;
            redraw.Invoke();
        }
    }

    class AlgorithmVersionControlModel : IControlModel
    {
        string[] algorithms = new string[] { "V0", "V1", "V2" };
        int selected_algorithm = 2;
        private Action redraw;
        private CalibrationModeIterator iterator;

        public AlgorithmVersionControlModel(
            Action redraw_callback,
            CalibrationModeIterator iterator)
        {
            this.iterator = iterator;
            for (int i = 0; i < algorithms.Length; i++)
            {
                if (algorithms[i] == iterator.CalibrationMode.algorithm)
                    selected_algorithm = i;
            }
            redraw = redraw_callback;
        }

        public string Name => "algorithm";

        public string Value => algorithms[selected_algorithm];

        public bool IsCheckboxChecked { get => false; set => throw new NotImplementedException(); }

        public bool IsCheckboxVisible => false;

        public void Decrement()
        {
            if (selected_algorithm > 0)
                selected_algorithm--;
            iterator.CalibrationMode.algorithm = algorithms[selected_algorithm];
            redraw.Invoke();
        }

        public void Increment()
        {
            if (selected_algorithm < algorithms.Length - 1)
                selected_algorithm++;
            iterator.CalibrationMode.algorithm = algorithms[selected_algorithm];
            redraw.Invoke();
        }
    }
    class IteratorOptionControlModel : IControlModel
    {
        public CalibrationModeIterator.OptionsField Field;
        private CalibrationModeIterator iterator;
        private Action redraw;
        private bool is_checkbox_enabled;

        public IteratorOptionControlModel(
           CalibrationModeIterator.OptionsField field,
           CalibrationModeIterator iterator,
           Action redraw_callback)
        {
            this.Field = field;
            this.iterator = iterator;
            redraw = redraw_callback;
        }
        public string Name => Field.field_name;
        public string Value => Field.GetFieldValue(iterator.CalibrationMode).ToString();
        public bool IsCheckboxVisible { get; set; } = true;

        public bool IsCheckboxChecked
        {
            get => is_checkbox_enabled; set
            {
                is_checkbox_enabled = value;
                redraw.Invoke();
            }
        }

        public void Decrement()
        {
            Field.Increment(iterator.CalibrationMode, -1);
            redraw.Invoke();
        }
        public void Increment()
        {
            Field.Increment(iterator.CalibrationMode, 1);
            redraw.Invoke();
        }
    }

    class ListOfModesControlModel : IControlModel
    {
        private CalibrationModeIterator iterator;
        private Action redraw;
        string name;
        int index = 0;
        List<UtilityAndModePair> modes;

        public ListOfModesControlModel(
            string name,
           CalibrationModeIterator iterator,
           List<UtilityAndModePair> modes,
           Action redraw_callback)
        {
            this.name = name;
            this.modes = modes;
            this.iterator = iterator;
            redraw = redraw_callback;
        }

        public List<UtilityAndModePair> Modes
        {
            get => modes;
        }

        public string Name => name;

        public string Value => index.ToString();

        public bool IsCheckboxChecked { get => false; set => throw new NotImplementedException(); }

        public bool IsCheckboxVisible => false;

        public void Decrement()
        {
            if (modes == null || modes.Count == 0)
                return;
            if (index > 0)
            {
                iterator.CalibrationMode = modes[--index].mode;
                redraw.Invoke();
            }
        }

        public void Increment()
        {
            if (modes == null || modes.Count == 0)
                return;
            if (index < modes.Count - 1)
            {
                iterator.CalibrationMode = modes[++index].mode;
                redraw.Invoke();
            }
        }
    }

    /// <summary>
    /// Interaction logic for ManualVisualisationWindow.xaml
    /// </summary>
    public partial class ManualVisualisationWindow : Window
    {
        private CalibrationModeIterator iterator;
        List<OptionControl> option_controls = new List<OptionControl>();
        List<IteratorOptionControlModel> iterator_option_control_models = new List<IteratorOptionControlModel>();
        DataSetControlModel data_set_control_model;
        AlgorithmVersionControlModel algorithm_control_model;
        ListOfModesControlModel extremums_control_model;
        ListOfModesControlModel linear_results_control_model;

        eye_tracking_mouse.Options.CalibrationMode backup_mode = null;
        List<UtilityAndModePair> backup_extremums = null;
        List<UtilityAndModePair> backup_linear_results = null;

        public ManualVisualisationWindow()
        {
            InitializeComponent();
            LoadConfiguration(null);
        }

        private void LoadConfiguration(string configuration)
        {
            List<UtilityAndModePair> extremums = null ;
            List<UtilityAndModePair> linear_results= null;
            {
                string extremums_file_path = System.IO.Path.Combine(Utils.GetConfigurationDir(configuration), "extremum_search_results.json");
                if (System.IO.File.Exists(extremums_file_path))
                {
                    extremums = JsonConvert.DeserializeObject<List<UtilityAndModePair>>(System.IO.File.ReadAllText(extremums_file_path));
                }
            }

            {
                string linear_results_file_path = System.IO.Path.Combine(Utils.GetConfigurationDir(configuration), "linear_results_sorted.json");
                if (System.IO.File.Exists(linear_results_file_path))
                {
                    linear_results = JsonConvert.DeserializeObject<List<UtilityAndModePair>>(System.IO.File.ReadAllText(linear_results_file_path));
                }
            }


            Reset(Helpers.GetCalibrationMode(configuration),
                extremums,
                linear_results);
        }

        private void Reset(
            eye_tracking_mouse.Options.CalibrationMode mode,
            List<UtilityAndModePair> extremums,
            List<UtilityAndModePair> linear_results)
        {
            iterator = new CalibrationModeIterator(mode);
            foreach (var option_control in option_controls)
            {
                Grid.Children.Remove(option_control);
            }
            option_controls.Clear();
            iterator_option_control_models.Clear();

            List<IControlModel> models = new List<IControlModel>();
            foreach (var field in iterator.Fields)
            {
                var model = new IteratorOptionControlModel(field, iterator, Redraw);
                if (model.Name == "coordinate 2" || model.Name == "coordinate 3")
                    model.IsCheckboxChecked = true;
                models.Add(model);
                iterator_option_control_models.Add(model);
            }

            data_set_control_model = new DataSetControlModel(Redraw);
            algorithm_control_model = new AlgorithmVersionControlModel(Redraw, iterator);
            extremums_control_model = new ListOfModesControlModel("extremum", iterator, extremums, Redraw);
            linear_results_control_model = new ListOfModesControlModel("linear results", iterator, linear_results, Redraw);


            models.Add(data_set_control_model);
            models.Add(algorithm_control_model);
            models.Add(extremums_control_model);
            models.Add(linear_results_control_model);

            int row_number = 0;
            foreach (var model in models)
            {
                var control = new OptionControl(model);
                option_controls.Add(control);
                Grid.Children.Add(control);
                Grid.SetRow(control, row_number++);
            }

            Redraw();
        }

        class PlotData
        {
            public double[] X;
            public double[] Y;
            public double[] Z;
        }

        Dictionary<string, PlotData> cache = new Dictionary<string, PlotData>();

        private string GetKey(
            eye_tracking_mouse.Options.CalibrationMode mode,
            string data_point_name,
            CalibrationModeIterator.OptionsField[] enabled_fields)
        {
            return data_point_name + enabled_fields[0].field_name + " " +
                  enabled_fields[1].field_name + " " + mode.algorithm + iterator.GetUniqueKey(mode);
        }

        private static PlotData CalculatePlotData(
            eye_tracking_mouse.Options.CalibrationMode mode,
            CalibrationModeIterator.OptionsField[] enabled_fields,
            List<DataPoint> data_points)
        {
            var plot_data = new PlotData
            {
                X = new double[enabled_fields[0].Range.Length * enabled_fields[1].Range.Length],
                Y = new double[enabled_fields[0].Range.Length * enabled_fields[1].Range.Length],
                Z = new double[enabled_fields[0].Range.Length * enabled_fields[1].Range.Length]
            };

            Task[] tasks = new Task[enabled_fields[0].Range.Length];
            for (int i = tasks.Length - 1; i >= 0; i--)
            {
                int i_copy = i;
                tasks[i_copy] = new Task(() =>
                {
                    var this_thread_mode_clone = mode.Clone();
                    enabled_fields[0].SetFieldValue(this_thread_mode_clone, enabled_fields[0].Range[i_copy]);
                    enabled_fields[1].SetFieldValue(this_thread_mode_clone, enabled_fields[1].Min);
                    int j = 0;
                    do
                    {
                        int index = i_copy * enabled_fields[1].Range.Length + j++;
                        plot_data.X[index] = enabled_fields[0].GetFieldValue(this_thread_mode_clone);
                        plot_data.Y[index] = enabled_fields[1].GetFieldValue(this_thread_mode_clone);
                        plot_data.Z[index] = Helpers.TestCalibrationMode(data_points, this_thread_mode_clone).UtilityFunction;
                    }
                    while (enabled_fields[1].Increment(this_thread_mode_clone, 1));
                });
                tasks[i_copy].Start();
            }

            Task.WaitAll(tasks);
            return plot_data;
        }
        private void UpdateControls ()
        {
            foreach (var control in option_controls)
            {
                control.Update();
            }
        }

        private void Redraw()
        {
            int enabled_checkbox_count = iterator_option_control_models.Count((x) =>
            {
                return x.IsCheckboxChecked;
            });

            if (enabled_checkbox_count < 2)
            {
                foreach (var model in iterator_option_control_models)
                {
                    model.IsCheckboxVisible = true;
                }
                UpdateControls();
                return;
            }

            eye_tracking_mouse.Options.CalibrationMode mode = iterator.CalibrationMode.Clone();
            CalibrationModeIterator.OptionsField[] enabled_fields = new CalibrationModeIterator.OptionsField[2];
            {
                int j = 0;
                foreach (var model in iterator_option_control_models)
                {
                    if (model.IsCheckboxChecked)
                    {
                        enabled_fields[j++] = model.Field;
                        model.IsCheckboxVisible = true;
                    }
                    else
                    {
                        model.IsCheckboxVisible = false;
                    }
                }
            }

            UpdateControls();

            enabled_fields[0].SetFieldValue(mode, enabled_fields[0].Min);
            enabled_fields[1].SetFieldValue(mode, enabled_fields[1].Min);

            string key = GetKey(mode, data_set_control_model.Value, enabled_fields);

            PlotData plot_data = null;

            if (cache.ContainsKey(key))
            {
                plot_data = cache[key];
            }
            if (plot_data == null)
            {
                plot_data = CalculatePlotData(mode, enabled_fields, data_set_control_model.DataPoints);
                if (!cache.ContainsKey(key))
                    cache.Add(key, plot_data);
            }

            double max_z = 0;
            double max_x = 0;
            double max_y = 0;
            for ( int i = 0; i < plot_data.Z.Length; i++)
            {
                if (plot_data.Z[i] > max_z)
                {
                    max_z = plot_data.Z[i];
                    max_x = plot_data.X[i];
                    max_y = plot_data.Y[i];
                }
            }

            {
                // Show current configuration point.
                double x = enabled_fields[0].GetFieldValue(iterator.CalibrationMode),
                    y = enabled_fields[1].GetFieldValue(iterator.CalibrationMode),
                    z = Helpers.TestCalibrationMode(data_set_control_model.DataPoints, iterator.CalibrationMode).UtilityFunction;
                GnuPlot.Unset("label 1");
                GnuPlot.Set(string.Format("label 1 at {0}, {1}, {2} \"{2}\" point pt 4", x, y, z));
                // Show current configuration point.

                GnuPlot.Unset("label 3");
                GnuPlot.Set(string.Format("label 3 at {0}, {1}, {2} point pt 7", x, y, max_z));

                GnuPlot.Unset("label 4");
                GnuPlot.Set(string.Format("label 4 at {0}, {1}, {2} \"{2}\" point pt 5", max_x, max_y, max_z));

            }

            if (backup_mode != null)
            {
                double x = enabled_fields[0].GetFieldValue(backup_mode),
                    y = enabled_fields[1].GetFieldValue(backup_mode),
                    z = Helpers.TestCalibrationMode(data_set_control_model.DataPoints, backup_mode).UtilityFunction;
                GnuPlot.Unset("label 2");
                GnuPlot.Set(string.Format("label 2 at {0}, {1}, {2} point pt 6", x, y, z));
            }

            GnuPlot.SPlot(plot_data.X, plot_data.Y, plot_data.Z);
        }

        private void SaveToConfig(string config_name)
        {
            (new eye_tracking_mouse.Options
            {
                calibration_mode = iterator.CalibrationMode
            }).SaveToFile(System.IO.Path.Combine(Utils.GetConfigurationDir(config_name), "options.json"));

        }

        private void Button_SaveToExistingConfig_Click(object sender, RoutedEventArgs e)
        {
            var configuration_selection_dialog = new ConfigurationSelectionDialog();
            if (configuration_selection_dialog.ShowDialog() != true)
                return;
            SaveToConfig(configuration_selection_dialog.GetSelectedConfiguration());
        }

        private void Button_SaveToNewConfig_Click(object sender, RoutedEventArgs e)
        {
            string new_config = Utils.GenerateNewConfigurationName("manual");
            Utils.CreateConfiguration(new_config);
            SaveToConfig(new_config);
        }

        private void Button_LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var configuration_selection_dialog = new ConfigurationSelectionDialog();
            if (configuration_selection_dialog.ShowDialog() != true)
                return;

            LoadConfiguration(configuration_selection_dialog.GetSelectedConfiguration());
        }

        private void Button_SetBackup_Click(object sender, RoutedEventArgs e)
        {
            backup_mode = iterator.CalibrationMode.Clone();
            backup_extremums = extremums_control_model.Modes;
            backup_linear_results = linear_results_control_model.Modes;
        }

        private void Button_RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (backup_mode == null)
                return;

            Reset(backup_mode.Clone(), backup_extremums, backup_linear_results);
        }
    }
}
