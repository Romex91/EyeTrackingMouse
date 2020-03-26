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
                public double before_correction { get; set; }
                public double after_correction { get; set; }
            }

            public List<Error> errors = new List<Error>();
            public long time_ms;

            public string ToString()
            {
                return " Utility: " + UtilityFunction + " Milliseconds Elapsed: " + time_ms;
            }

            [JsonIgnore]
            public double UtilityFunction
            {
                get
                {
                    double total_correction = 0;
                    foreach (var error in errors)
                    {
                        total_correction += error.before_correction - error.after_correction;
                    }

                    return total_correction / errors.Count;
                    }
            }
        }

        public static eye_tracking_mouse.ICalibrationManager SetupCalibrationManager(string configuration)
        {
            eye_tracking_mouse.Options.Instance = eye_tracking_mouse.Options.LoadFromFile(
                Path.Combine(Utils.GetConfigurationDir(configuration), "options.json"));
            eye_tracking_mouse.Options.CalibrationMode.Changed?.Invoke(null, null);

            eye_tracking_mouse.FilesSavingQueue.DisabledForTesting = true;
            var calibration_manager = eye_tracking_mouse.CalibrationManager.Instance;
            calibration_manager.Reset();
            return calibration_manager;
        }

        public static eye_tracking_mouse.ICalibrationManager SetupCalibrationManager(eye_tracking_mouse.Options.CalibrationMode calibration_mode)
        {
            eye_tracking_mouse.Options.Instance.calibration_mode = calibration_mode;
            eye_tracking_mouse.Options.CalibrationMode.Changed?.Invoke(null, null);
            var calibration_manager = eye_tracking_mouse.CalibrationManager.Instance;
            calibration_manager.Reset();
            return calibration_manager;
        }

        public static Helpers.TestResult TestCalibrationMode(List<DataPoint> data_points, eye_tracking_mouse.Options.CalibrationMode calibration_mode)
        {
            return TestCalibrationManager(SetupCalibrationManager(calibration_mode), data_points);
        }


        public static TestResult TestCalibrationManager(
            eye_tracking_mouse.ICalibrationManager calibration_manager,
            List<DataPoint> data_points)
        {
            var time_before = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            
            TestResult result = new TestResult();
            foreach (var data_point in data_points)
            {
                result.errors.Add(AddDataPoint(calibration_manager, data_point));
            }

            var time_after = System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime;
            
            result.time_ms = (int)(time_after - time_before).TotalMilliseconds;
            return result;
        }
        
        public static TestResult.Error AddDataPoint(eye_tracking_mouse.ICalibrationManager calibration_manager, DataPoint data_point)
        {
            var shift_position = new eye_tracking_mouse.ShiftPosition(data_point.tobii_coordinates.GetEnabledCoordinates());
            var tobii_gaze_point = new Point(
                        data_point.tobii_coordinates.gaze_point.X,
                        data_point.tobii_coordinates.gaze_point.Y);
            var shift = calibration_manager.GetShift(shift_position);
            var corrected_gaze_point = new Point(
                tobii_gaze_point.X + shift.X,
                tobii_gaze_point.Y + shift.Y);

            TestResult.Error error =  new TestResult.Error
            {
                before_correction = Point.Subtract(
                    data_point.true_location_on_screen,
                    tobii_gaze_point).Length,
                after_correction = Point.Subtract(
                    data_point.true_location_on_screen,
                    corrected_gaze_point).Length,
            };

            var correction = Point.Subtract(data_point.true_location_on_screen, tobii_gaze_point);
            calibration_manager.AddShift(shift_position, new System.Drawing.Point((int)correction.X, (int)correction.Y));
            return error;
        }
    }
}