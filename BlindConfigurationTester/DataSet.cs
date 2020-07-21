using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace BlindConfigurationTester
{
    public class DataSet
    {
        public class Section
        {
            public string name;
            public List<DataPoint> data_points;
        }

        public static DataSet LoadSingleSection(string section_name)
        {
            var builder = DataSetBuilder.Load(section_name);

            return new DataSet { sections = new Section[] {new Section { name = section_name, data_points = builder.data_points} } };
        }

        public static DataSet LoadEverything()
        {
            var section_names = DataSetBuilder.ListDataSetsNames();
            DataSet retval = new DataSet { sections = new Section[section_names.Length] };
            for (int i = 0; i < section_names.Length; i++ )
            {
                var builder = DataSetBuilder.Load(section_names[i]);
                retval.sections[i] = new Section { name = section_names[i], data_points = builder.data_points };
            }

            return retval;
        }

        // Sections are unrelated to each other. E.G. they contain data collected on different people or Tobii calibration settings.
        public Section[] sections;
    }

    public static partial class Helpers
    {
        public class TestResult
        {
            public struct Error
            {
                public float before_correction { get; set; }
                public float after_correction { get; set; }
            }

            [JsonIgnore]
            public List<Error> errors = new List<Error>();

            public string section_name;
            public string ToText()
            {
                return 
                    "\nSection Name: " + section_name + 
                    " \n Utility: " + UtilityFunction +
                    "\n avg_error_before: " + TestInfo.avg_error_before +
                    "\n avg_error_after: " + TestInfo.avg_error_after;
            }


            public struct Info
            {
                public float avg_error_before;
                public float avg_error_after;
            }

            public Info TestInfo
            {
                get
                {
                    float total_error_before = 0;
                    float total_error_after = 0;
                    foreach (var error in errors)
                    {
                        total_error_before += error.before_correction;
                        total_error_after += error.after_correction;
                    }

                    return new Info
                    {
                        avg_error_before = total_error_before / errors.Count,
                        avg_error_after = total_error_after / errors.Count
                    };
                }
            }

            public float UtilityFunction
            {
                get
                {
                    var info = TestInfo;
                    var utility = (info.avg_error_before - info.avg_error_after) * 100f / info.avg_error_before;
                    if (float.IsNaN(utility) || float.IsInfinity(utility))
                        return 0;
                    return utility;
                }
            }
        }

        public static float GetCombinedUtility(TestResult[] results)
        {
            float total_utility = 0;
            foreach(var result in results)
            {
                total_utility += result.UtilityFunction;
            }
            return total_utility / results.Length;
        }
        public static eye_tracking_mouse.Options.CalibrationMode GetCalibrationMode(string configuration)
        {
            eye_tracking_mouse.FilesSavingQueue.DisabledForTesting = true;
            eye_tracking_mouse.Options options = eye_tracking_mouse.Options.LoadFromFile(
                Path.Combine(Utils.GetConfigurationDir(configuration), "options.json"));
            return options.calibration_mode;
        }

        public static eye_tracking_mouse.ICalibrationManager SetupCalibrationManager(eye_tracking_mouse.Options.CalibrationMode calibration_mode)
        {
            eye_tracking_mouse.FilesSavingQueue.DisabledForTesting = true;
            var calibration_manager = eye_tracking_mouse.CalibrationManager.BuildCalibrationManagerForTesting(calibration_mode);
            calibration_manager.Reset();
            return calibration_manager;
        }


        public static Helpers.TestResult[] TestCalibrationMode(DataSet data_set, eye_tracking_mouse.Options.CalibrationMode calibration_mode)
        {
            return TestCalibrationManager(SetupCalibrationManager(calibration_mode),
                data_set, calibration_mode.additional_dimensions_configuration);
        }

        public static TestResult[] RunPerfTest(
            eye_tracking_mouse.ICalibrationManager calibration_manager,
            DataSet data_set,
            eye_tracking_mouse.AdditionalDimensionsConfguration config,
            out int avg_mcs)
        {
            int iterations_number = 4;

            Helpers.TestCalibrationManager(calibration_manager, data_set, config);
            calibration_manager.Reset();
            GC.Collect();

            var time_before = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            for (int i = 0; i < iterations_number; i++)
            {
                TestCalibrationManager(
                    calibration_manager,
                    data_set,
                    config);
                calibration_manager.Reset();
                GC.Collect();
            }

            var time_after = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            double total_time_ms = (time_after - time_before).TotalMilliseconds;

            avg_mcs = (int)(total_time_ms / iterations_number * 1000);
            return Helpers.TestCalibrationManager(calibration_manager, data_set, config);
        }

        public static TestResult[] TestCalibrationManager(
            eye_tracking_mouse.ICalibrationManager calibration_manager,
            DataSet data_set,
            eye_tracking_mouse.AdditionalDimensionsConfguration config)
        {
            TestResult[] result = new TestResult[data_set.sections.Length];
            for (int i = 0; i < data_set.sections.Length; i++)
            {
                calibration_manager.Reset();
                result[i] = new TestResult { section_name = data_set.sections[i].name };
                foreach (var data_point in data_set.sections[i].data_points)
                {
                    result[i].errors.Add(AddDataPoint(calibration_manager, data_point, config));
                }
            }

            return result;
        }

        public static TestResult.Error AddDataPoint(
            eye_tracking_mouse.ICalibrationManager calibration_manager,
            DataPoint data_point,
            eye_tracking_mouse.AdditionalDimensionsConfguration config)
        {
            float[] shift_position = data_point.tobii_coordinates.ToCoordinates(config);
            var tobii_gaze_point = new Point(
                        data_point.tobii_coordinates.gaze_point.X,
                        data_point.tobii_coordinates.gaze_point.Y);
            shift_position[0] = (float)tobii_gaze_point.X;
            shift_position[1] = (float)tobii_gaze_point.Y;
            var shift = calibration_manager.GetShift(shift_position);
            var corrected_gaze_point = new Point(
                tobii_gaze_point.X + shift.X,
                tobii_gaze_point.Y + shift.Y);

            TestResult.Error error = new TestResult.Error
            {
                before_correction = (float)Point.Subtract(
                    data_point.true_location_on_screen,
                    tobii_gaze_point).Length,
                after_correction = (float)Point.Subtract(
                    data_point.true_location_on_screen,
                    corrected_gaze_point).Length,
            };

            var correction = Point.Subtract(data_point.true_location_on_screen, tobii_gaze_point);
            if (error.after_correction > 5)
                calibration_manager.AddShift(shift_position, new System.Drawing.Point((int)correction.X, (int)correction.Y));
            return error;
        }
    }
}
