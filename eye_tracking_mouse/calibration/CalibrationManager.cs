using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    public interface ICalibrationManager : IDisposable
    {
        bool IsDebugWindowEnabled { get; set; }
        void Reset();
        void AddShift(float[] coordinates, Point shift);
        Point GetShift(float[] coordinates);

        void SaveInDirectory(string directory_path);
    }

    // Doesn't save user corrections.
    // Used as control group in studies.
    class NoCalibrationManager : ICalibrationManager
    {
        public bool IsDebugWindowEnabled { get => false; set { } }

        public void AddShift(float[] coordinates, Point shift)
        {
        }

        public void Dispose()
        {
        }

        public Point GetShift(float[] coordinates)
        {
            return new Point(0, 0);
        }

        public void Reset()
        {
        }

        public void SaveInDirectory(string directory_path)
        {
        }

        public void SetDebugWindowEnabled(bool enabled)
        {
            throw new NotImplementedException();
        }
    }

    public static class CalibrationManager
    {
        static public ICalibrationManager Instance
        {
            get
            {
                if (instance == null)
                {

                    Options.CalibrationMode.Changed += ReloadInstance;
                    ReloadInstance(null, null);
                }
                return instance;
            }
        }

        static public ICalibrationManager BuildCalibrationManagerForTesting(Options.CalibrationMode calibration_mode)
        {
            return BuildCalibrationManager(calibration_mode, true);
        }

        static private ICalibrationManager instance;

        static private ICalibrationManager BuildCalibrationManager(Options.CalibrationMode calibration_mode, bool for_testing)
        {

            if (calibration_mode.algorithm == "V0")
                return new CalibrationManagerV0(calibration_mode, for_testing);
            else if (calibration_mode.algorithm == "V1")
                return new CalibrationManagerV1(calibration_mode, for_testing);
            else if (calibration_mode.algorithm == "V2")
                return new CalibrationManagerV2(calibration_mode, for_testing);
            else if (calibration_mode.algorithm == "NO")
                return new NoCalibrationManager();
            else
            {
                System.Windows.MessageBox.Show("Wrong algorithm name in options file");
                Environment.Exit(-1);
                return null;
            }
        }

        static private void ReloadInstance(object sender, EventArgs args)
        {
            lock (Helpers.locker)
            {
                instance?.Dispose();
                CoordinateSmoother.Reset();
                instance = BuildCalibrationManager(Options.Instance.calibration_mode, false);
            }
        }
    }
}
