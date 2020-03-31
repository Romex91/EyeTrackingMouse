﻿using Newtonsoft.Json;
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
        void AddShift(List<double> coordinates, Point shift);
        Point GetShift(List<double> coordinates);

        void SaveInDirectory(string directory_path);
    }

    // Doesn't save user corrections.
    // Used as control group in studies.
    class NoCalibrationManager : ICalibrationManager
    {
        public bool IsDebugWindowEnabled { get => false; set { } }

        public void AddShift(List<double> coordinates, Point shift)
        {
        }

        public void Dispose()
        {
        }

        public Point GetShift(List<double> coordinates)
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
            return BuildCalibrationManager(calibration_mode);
        }

        static private ICalibrationManager instance;

        static private ICalibrationManager BuildCalibrationManager(Options.CalibrationMode calibration_mode)
        {

            if (calibration_mode.algorithm == "V0")
                return new CalibrationManagerV0(calibration_mode);
            else if (calibration_mode.algorithm == "V1")
                return new CalibrationManagerV1(calibration_mode);
            else if (calibration_mode.algorithm == "NO")
                return new NoCalibrationManager();
            else throw new Exception("Wrong algorithm name in options file");
        }

        static private void ReloadInstance(object sender, EventArgs args)
        {
            lock(Helpers.locker)
            {
                instance?.Dispose();
                instance = BuildCalibrationManager(Options.Instance.calibration_mode);
            }
        }
    }
}
