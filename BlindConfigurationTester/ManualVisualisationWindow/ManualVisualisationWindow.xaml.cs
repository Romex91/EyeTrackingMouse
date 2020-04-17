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

                int i = 0;
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
                    do
                    {
                        enabled_fields[1].SetFieldValue(mode, enabled_fields[1].Min);
                        do
                        {
                            plot_data.X[i] = enabled_fields[0].GetFieldValue(mode);
                            plot_data.Y[i] = enabled_fields[1].GetFieldValue(mode);
                            plot_data.Z[i] = Helpers.TestCalibrationMode(data_points, mode).UtilityFunction;
                            i++;
                        }
                        while (enabled_fields[1].Increment(mode, 1));
                    }
                    while (enabled_fields[0].Increment(mode, 1));

                    cache.Add(key, plot_data);
                }

                GnuPlot.SPlot(plot_data.X, plot_data.Y, plot_data.Z);
            }
        }
    }
}
