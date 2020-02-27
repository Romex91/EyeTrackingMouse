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
    interface ICalibrationManager : IDisposable
    {
        void ToggleDebugWindow();
        void Reset();
        void AddShift(ShiftPosition cursor_position, Point shift);
        Point GetShift(ShiftPosition cursor_position);
    }

    // Doesn't save user corrections.
    // Used as control group in studies.
    class NoCalibrationManager : ICalibrationManager
    {
        public void AddShift(ShiftPosition cursor_position, Point shift)
        {
        }

        public void Dispose()
        {
        }

        public Point GetShift(ShiftPosition cursor_position)
        {
            return new Point(0, 0);
        }

        public void Reset()
        {
        }

        public void ToggleDebugWindow()
        {
        }
    }

    static class CalibrationManager
    {
        static public ICalibrationManager Instance
        {
            get
            {
                if (instance == null)
                {

                    Settings.CalibrationModeChanged += ReloadInstance;
                    ReloadInstance(null, null);
                }
                return instance;
            }
        }

        static private ICalibrationManager instance;

        static private void ReloadInstance(object sender, EventArgs args)
        {
            lock(Helpers.locker)
            {
                instance?.Dispose();
                if (Options.Instance.calibration_mode.algorithm == "V0")
                    instance = new CalibrationManagerV0();
                else if (Options.Instance.calibration_mode.algorithm == "V1")
                    instance = new CalibrationManagerV1();
                else if (Options.Instance.calibration_mode.algorithm == "NO")
                    instance = new NoCalibrationManager();
                else throw new Exception("Wrong algorithm name in options file");
            }
           
        }
    }
}
