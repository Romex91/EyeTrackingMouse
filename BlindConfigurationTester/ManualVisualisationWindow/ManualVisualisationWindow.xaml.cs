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

namespace BlindConfigurationTester.ManualVisualisationWindow
{
    /// <summary>
    /// Interaction logic for ManualVisualisationWindow.xaml
    /// </summary>
    public partial class ManualVisualisationWindow : Window
    {
        private CalibrationModeIterator iterator;
        List<OptionControl> option_controls = new List<OptionControl>();

        public ManualVisualisationWindow(eye_tracking_mouse.Options.CalibrationMode mode)
        {
            InitializeComponent();
            iterator = new CalibrationModeIterator(mode);

            int row_number = 0;
            foreach (var field in iterator.Fields)
            {
                var control = new OptionControl(field, iterator, Redraw);
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

        private void Redraw()
        {
            int enabled_checkbox_count = option_controls.Count((x) =>
            {
                return x.CheckBox_Visualize.IsChecked == true;
            });

            if (enabled_checkbox_count < 2)
            {
                foreach (var control in option_controls)
                {
                    control.CheckBox_Visualize.IsEnabled = true;
                }
            }
            else
            {
                eye_tracking_mouse.Options.CalibrationMode mode = iterator.CalibrationMode.Clone();
                CalibrationModeIterator.OptionsField[] enabled_fields = new CalibrationModeIterator.OptionsField[2];
                {
                    int j = 0;
                    foreach (var control in option_controls)
                    {
                        if (control.CheckBox_Visualize.IsChecked == true)
                        {
                            enabled_fields[j++] = control.Field;
                            control.CheckBox_Visualize.IsEnabled = true;
                        }
                        else
                        {
                            control.CheckBox_Visualize.IsEnabled = false;
                        }
                    }
                }

                var data_points = DataSet.Load("0roman").data_points;

                enabled_fields[0].SetFieldValue(mode, enabled_fields[0].Min);
                enabled_fields[1].SetFieldValue(mode, enabled_fields[1].Min);
                string key =
                    enabled_fields[0].field_name + " " +
                    enabled_fields[1].field_name + " " +
                    JsonConvert.SerializeObject(mode, Formatting.None);


                PlotData plot_data;

                if (cache.ContainsKey(key))
                {
                    plot_data = cache[key];
                }
                else
                {
                    plot_data = new PlotData
                    {
                        X = new double[enabled_fields[0].Count * enabled_fields[1].Count],
                        Y = new double[enabled_fields[0].Count * enabled_fields[1].Count],
                        Z = new double[enabled_fields[0].Count * enabled_fields[1].Count]
                    };

                    var range = enabled_fields[0].range.GetRange();
                    Task[] tasks = new Task[range.Count];
                    for (int i = range.Count - 1; i >= 0 ; i--)
                    {
                        int i_copy = i;
                        tasks[i_copy] = new Task(() =>
                        {
                            var this_thread_mode_clone = mode.Clone();
                            enabled_fields[0].SetFieldValue(this_thread_mode_clone, range[i_copy]);
                            enabled_fields[1].SetFieldValue(this_thread_mode_clone, enabled_fields[1].Min);
                            int j = 0;
                            do
                            {
                                int index = i_copy * enabled_fields[0].Count + j++;
                                plot_data.X[index] = enabled_fields[0].GetFieldValue(this_thread_mode_clone);
                                plot_data.Y[index] = enabled_fields[1].GetFieldValue(this_thread_mode_clone);
                                plot_data.Z[index] = Helpers.TestCalibrationMode(data_points, this_thread_mode_clone).UtilityFunction;
                            }
                            while (enabled_fields[1].Increment(this_thread_mode_clone, 1));
                        });
                        tasks[i_copy].Start();
                    }

                    Task.WaitAll(tasks);

                    cache.Add(key, plot_data);
                }

                {
                    // Show current configuration point.
                    double x = enabled_fields[0].GetFieldValue(iterator.CalibrationMode), 
                        y = enabled_fields[1].GetFieldValue(iterator.CalibrationMode), 
                        z = Helpers.TestCalibrationMode(data_points, iterator.CalibrationMode).UtilityFunction;

                    GnuPlot.Unset("label 1");
                    GnuPlot.Set(string.Format("label 1 at {0}, {1}, {2} \"{2}\" point pt 7", x, y, z));
                }

                GnuPlot.SPlot(plot_data.X, plot_data.Y, plot_data.Z);
            }
        }
    }
}
