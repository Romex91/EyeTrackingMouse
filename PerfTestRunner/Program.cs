using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTestRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            var caibration_mode = BlindConfigurationTester.Helpers.GetCalibrationMode(null);
            var calibration_manager = BlindConfigurationTester.Helpers.SetupCalibrationManager(caibration_mode);
            var data_points = BlindConfigurationTester.DataSet.Load("0roman").data_points;

            int avg_time;
            var result = BlindConfigurationTester.Helpers.RunPerfTest(
                calibration_manager,
                data_points,
                caibration_mode.additional_dimensions_configuration,
                out avg_time);

            return avg_time;
        }
    }
}
