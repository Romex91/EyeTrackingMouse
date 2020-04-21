using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BlindConfigurationTester
{
    public class DataSet
    {
        public int number_of_completed_sessions;

        [JsonIgnore]
        public string name;
        public List<Session> sessions = new List<Session> { new Session {
            points_sequences = new Session.PointsSequence[1]{ new Session.PointsSequence { points_count = 50, seed = 0 } },
            size_of_circle = 6,
            tag = "",
            instructions = "This text will be shown to user before session." } };

        [JsonIgnore]
        public List<DataPoint> data_points = new List<DataPoint>();

        public DataSet(string name_value)
        {
            this.name = name_value;
        }

        public static string DataSetsFolder { get { return Path.Combine(Utils.DataFolder, "DataSets"); } }
        [JsonIgnore]
        public string DataSetResultsFolder { get { return GetDataSetFolder(name); } }

        public static string GetDataSetFolder(string study_name)
        {
            return Path.Combine(DataSetsFolder, study_name);
        }

        public static DataSet Load(string data_set_name)
        {
            string json_path = Path.Combine(GetDataSetFolder(data_set_name), "config.json");
            string dataset_path = Path.Combine(GetDataSetFolder(data_set_name), "data_set.json");
            if (File.Exists(json_path))
            {
                while (true)
                {
                    try
                    {
                        var data_set = JsonConvert.DeserializeObject<DataSet>(File.ReadAllText(json_path));
                        data_set.name = data_set_name;
                        if (File.Exists(dataset_path))
                            data_set.data_points = JsonConvert.DeserializeObject<List<DataPoint>>(File.ReadAllText(dataset_path));
                        return data_set;
                    }
                    catch (IOException)
                    {
                    }
                    catch
                    {
                        return null;
                    }
                }
            }


            return null;
        }

        public string GetInfo()
        {
            string info =
               "Study " + name + "\n" +
               "Number of completed sessions: " + number_of_completed_sessions + "\n";

            return info;
        }

        public void SaveToFile()
        {
            if (!Directory.Exists(DataSetResultsFolder))
                Directory.CreateDirectory(DataSetResultsFolder);
            File.WriteAllText(Path.Combine(DataSetResultsFolder, "config.json"), JsonConvert.SerializeObject(this, Formatting.Indented));
            File.WriteAllText(Path.Combine(DataSetResultsFolder, "data_set.json"), JsonConvert.SerializeObject(data_points, Formatting.None));
        }

        public void StartTrainingSession()
        {
            if (number_of_completed_sessions >= sessions.Count)
            {
                MessageBox.Show("All sessions are finished");
                return;
            }

            Session session = sessions[number_of_completed_sessions];
            int session_seed = unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId);
            var new_data_points = session.Start(true, session_seed);
            if (new_data_points != null)
            {
                data_points.InsertRange(data_points.Count, new_data_points);
                number_of_completed_sessions++;
                SaveToFile();
            }
        }

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

            public string ToString()
            {
                return " Utility: " + UtilityFunction;
            }

            public float UtilityFunction
            {
                get
                {
                    float total_correction = 0;
                    foreach (var error in errors)
                    {
                        total_correction += error.before_correction - error.after_correction;
                    }
                    if (errors.Count == 0)
                        return 0;
                    return total_correction / errors.Count;
                }
            }
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

        private static bool IsModeCorrect(eye_tracking_mouse.Options.CalibrationMode mode)
        {
            return
                mode.size_of_opaque_sector_in_percents + mode.size_of_transparent_sector_in_percents < 91;
        }

        public static Helpers.TestResult TestCalibrationMode(List<DataPoint> data_points, eye_tracking_mouse.Options.CalibrationMode calibration_mode)
        {
            if (!IsModeCorrect(calibration_mode))
                return new Helpers.TestResult();

            return TestCalibrationManager(SetupCalibrationManager(calibration_mode), 
                data_points, calibration_mode.additional_dimensions_configuration);
        }

        public static TestResult RunPerfTest(
            eye_tracking_mouse.ICalibrationManager calibration_manager,
            List<DataPoint> data_points,
            eye_tracking_mouse.AdditionalDimensionsConfguration config,
            out int avg_mcs)
        {
            int iterations_number = 4;

            var result = Helpers.TestCalibrationManager(calibration_manager, data_points, config);
            calibration_manager.Reset();
            GC.Collect();

            var time_before = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            for (int i = 0; i < iterations_number; i++)
            {
                TestCalibrationManager(
                    calibration_manager,
                    data_points,
                    config);
                calibration_manager.Reset();
                GC.Collect();
            }

            var time_after = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            double total_time_ms = (time_after - time_before).TotalMilliseconds;

            avg_mcs = (int)(total_time_ms / iterations_number * 1000);
            return result;
        }

        public static TestResult TestCalibrationManager(
            eye_tracking_mouse.ICalibrationManager calibration_manager,
            List<DataPoint> data_points,
            eye_tracking_mouse.AdditionalDimensionsConfguration config)
        {

            TestResult result = new TestResult();
            foreach (var data_point in data_points)
            {
                result.errors.Add(AddDataPoint(calibration_manager, data_point, config));
            }

            return result;
        }

        public static TestResult.Error AddDataPoint(
            eye_tracking_mouse.ICalibrationManager calibration_manager,
            DataPoint data_point,
            eye_tracking_mouse.AdditionalDimensionsConfguration config)
        {
            var shift_position = data_point.tobii_coordinates.ToCoordinates(config);
            var tobii_gaze_point = new Point(
                        data_point.tobii_coordinates.gaze_point.X,
                        data_point.tobii_coordinates.gaze_point.Y);
            var shift = calibration_manager.GetShift(shift_position);
            var corrected_gaze_point = new Point(
                tobii_gaze_point.X + shift.X,
                tobii_gaze_point.Y + shift.Y);

            TestResult.Error error = new TestResult.Error
            {
                before_correction = (float) Point.Subtract(
                    data_point.true_location_on_screen,
                    tobii_gaze_point).Length,
                after_correction = (float) Point.Subtract(
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