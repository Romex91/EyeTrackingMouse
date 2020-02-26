using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{

    // Takes average of closest corrections.
    class CalibrationManagerV0 : ICalibrationManager
    {
        public Point GetShift(ShiftPosition cursor_position)
        {
            lock (Helpers.locker)
            {
                shift_storage.calibration_window?.OnCursorPositionUpdate(cursor_position);

                var closest_indices = shift_storage.GetClosestShiftIndexes(cursor_position, Options.Instance.calibration_mode.considered_zones_count);
                if (closest_indices == null)
                {
                    Debug.Assert(shift_storage.Corrections.Count() == 0);
                    return new Point(0, 0);
                }

                double sum_of_reverse_distances = 0;
                foreach (var index in closest_indices)
                {
                    sum_of_reverse_distances += (1 / index.Item2);
                }

                Point resulting_shift = new Point(0, 0);
                foreach (var index in closest_indices)
                {
                    resulting_shift.X += (int)(shift_storage.Corrections[index.Item1].Shift.X / index.Item2 / sum_of_reverse_distances);
                    resulting_shift.Y += (int)(shift_storage.Corrections[index.Item1].Shift.Y / index.Item2 / sum_of_reverse_distances);
                }

                return resulting_shift;
            }
        }

        public void AddShift(ShiftPosition cursor_position, Point shift)
        {
            shift_storage.AddShift(cursor_position, shift);
        }

        public void Dispose()
        {
            shift_storage.Dispose();
        }

        public void Reset()
        {
            shift_storage.Reset();
        }

        public void ToggleDebugWindow()
        {
            shift_storage.ToggleDebugWindow();
        }

        private readonly ShiftsStorage shift_storage = new ShiftsStorage();
    }
}
