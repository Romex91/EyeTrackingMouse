using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    public class ShiftPosition
    {
        public ShiftPosition(List<double> coordinates)
        {
            this.coordinates = coordinates;
        }

        public List<double> coordinates;

        [JsonIgnore]
        public double X
        {
            get { return coordinates[0]; }
        }

        [JsonIgnore]
        public double Y
        {
            get { return coordinates[1]; }
        }
        // To calculate points density we split the screen to sectors. This algprithm is not accurate but simple and fast
        [JsonIgnore]
        public int SectorX
        {
            get
            {
                return (int)(X / 500.0);
            }
        }
        [JsonIgnore]
        public int SectorY
        {
            get
            {
                return (int)(Y / 500.0);
            }
        }

        public double GetDistance(ShiftPosition other)
        {
            double squared_distance = 0;

            for (int i = 0; i < coordinates.Count; i++)
            {
                double factor = i < 2 ? 1 : Math.Pow(Options.Instance.calibration_mode.multi_dimensions_detalization / 10.0, 2);
                squared_distance += Math.Pow(coordinates[i] - other.coordinates[i], 2) * factor;
            }

            return Math.Pow(squared_distance, 0.5);
        }
    }

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
                else if (Options.Instance.calibration_mode.algorithm == "NO")
                    instance = new NoCalibrationManager();
                else throw new Exception("Wrong algorithm name in options file");
            }
           
        }
    }
}
