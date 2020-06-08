using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{

    // Currently there are three versions of CalibrationManager: V0 V1 and V2
    // V0 and V1 are obsolete and persist purely for testing.
    //
    // All three versions are doing the same thing. They track history of previous error corrections and try to make sense from them.
    // Error correction is a little arrow on the screen that shows what error the eye tracker made at this point (see EyeTrackerErrorCorrection in ShiftsStorage.cs).
    //
    // V0 is a naive implementation that adjusts mouse position by average of closest to cursor error corrections.
    // Its drawback is that it considers only distances but not directions.
    // 
    // E.G.: Imagine this is a computer screen: 
    // =======================================================================
    //
    //             0            A  B
    //             ^              
    //             | 
    //             | the cursor is straight to the left of A and B
    // 
    // =======================================================================
    // 0 is cursor position.
    // A and B are error corrections made previously (arrows on screen)
    //
    // V0 measures distances from A and B (they are almost equal) and takes weighted average. 
    // The result will consider A and B almost equaly. This is okey when A and B are pointing in the same direction.
    // But when they are opposite they cancel out each other.
    // Consequence can be seen with bare eyes when you click UI elements placed close to each other.
    // It feels like the App doesn't consider you corrections so you have to press the same combination of WASD again and again.
    //
    // V1 solves this problem by "shading":
    //
    // The same screen with V0: 
    // =======================================================================
    //
    //             0            A ) B
    //                            ^
    //                            |
    //                            | A casts a shade on B
    //
    // =======================================================================
    // V1 will only conider A because B is behind the shade.
    // 
    // Unexpectedly V0 beats V1 in tests despite of the drawback being visible with bare eyes.
    // Here comes V2 that is a hybrid of V0 and V1
    // It applies V0 for cursor positions that are far away from exitsting error corrections and V1 for closer ones.
    class CalibrationManagerV2 : ICalibrationManager
    {
        private readonly ShiftsStorage shift_storage;
        private readonly Options.CalibrationMode calibration_mode;
        public CalibrationManagerV2(Options.CalibrationMode mode, bool for_testing)
        {
            calibration_mode = mode;
            shift_storage = new ShiftsStorage(calibration_mode, for_testing);
        }

        public Point GetShift(float[] cursor_position)
        {

            // |GetClosestCorrections| is computationaly heavy. We call it once and create copies for V0 and V1 parts of this function.
            List<ShiftsStorage.PointInfo> closest_corrections_v0 = new List<ShiftsStorage.PointInfo>(calibration_mode.considered_zones_count);
            List<ShiftsStorage.PointInfo> closest_corrections_v1 = new List<ShiftsStorage.PointInfo>(calibration_mode.considered_zones_count_v1);
            {
                var closest_corrections = shift_storage.GetClosestCorrections(cursor_position);
                if (closest_corrections == null || closest_corrections.Count() == 0)
                {
                    Debug.Assert(shift_storage.Corrections.Count() == 0 || !Helpers.AreCoordinatesSane(cursor_position));
                    return new Point(0, 0);
                }
                for (int i = 0; i < closest_corrections.Count; i++)
                {
                    var correction = closest_corrections[i];
                    if (i < calibration_mode.considered_zones_count)
                    {
                        closest_corrections_v0.Add(new ShiftsStorage.PointInfo { 
                            correction = correction.correction,
                            distance = correction.distance,
                            weight = correction.weight,
                            vector_from_correction_to_cursor = correction.vector_from_correction_to_cursor
                        });
                    }
                    if (i < calibration_mode.considered_zones_count_v1)
                    {
                        closest_corrections_v1.Add(new ShiftsStorage.PointInfo
                        {
                            correction = correction.correction,
                            distance = correction.distance,
                            weight = correction.weight,
                            vector_from_correction_to_cursor = correction.vector_from_correction_to_cursor
                        });
                    }
                }
            }
            // V0 part (average of closest error corrections weighted by distance):
            float sum_of_reverse_distances = 0;
            foreach (var index in closest_corrections_v0)
            {
                sum_of_reverse_distances += (1 / index.distance);
            }

            foreach (var correction in closest_corrections_v0)
            {
                correction.weight = 1 / correction.distance / sum_of_reverse_distances;
            }

            // V1 part (same as V0 plus shading)
            sum_of_reverse_distances = 0;
            foreach (var index in closest_corrections_v1)
            {
                sum_of_reverse_distances += (1 / index.distance);
            }
            ApplyShades(closest_corrections_v1);
            foreach (var correction in closest_corrections_v1)
            {
                correction.weight = correction.weight / correction.distance / sum_of_reverse_distances;
            }
            Helpers.NormalizeWeights(closest_corrections_v1);

            // Compute ratio of V0/V1 infuence on final result.
            Debug.Assert(calibration_mode.correction_fade_out_distance > 0);
            var vector_from_correction_to_cursor = closest_corrections_v1[0].vector_from_correction_to_cursor;
            float XYdistance = (float)Math.Sqrt(
                vector_from_correction_to_cursor[0] * vector_from_correction_to_cursor[0] +
                vector_from_correction_to_cursor[1] * vector_from_correction_to_cursor[1]);
            // Longer the distance lower the factor.
            float v0_vs_v1_factor = 1.0f - (float)Math.Pow(
                XYdistance / calibration_mode.correction_fade_out_distance, 4);
            if (v0_vs_v1_factor < 0)
                v0_vs_v1_factor = 0;

            // Merge v0 and v1 to single set using the computed factor.
            List<ShiftsStorage.PointInfo> v1_v0_hybrid_corrections = new List<ShiftsStorage.PointInfo>();

            foreach (var correction in closest_corrections_v1)
            {
                correction.weight = v0_vs_v1_factor * correction.weight;
                v1_v0_hybrid_corrections.Add(correction);
            }

            foreach (var correction in closest_corrections_v0)
            {
                correction.weight = (1 - v0_vs_v1_factor) * correction.weight;
                int i = 0;
                for (; i < v1_v0_hybrid_corrections.Count; i++)
                {
                    if (correction.correction == v1_v0_hybrid_corrections[i].correction)
                    {
                        v1_v0_hybrid_corrections[i].weight += correction.weight;
                        break;
                    }
                }
                if (i == v1_v0_hybrid_corrections.Count)
                    v1_v0_hybrid_corrections.Add(correction);
            }

            var result = Helpers.GetWeightedAverage(v1_v0_hybrid_corrections);

            if (shift_storage.calibration_window != null)
            {
                var lables = new List<Tuple<string /*text*/, System.Windows.Point>>();
                foreach (var correction in v1_v0_hybrid_corrections)
                    lables.Add(new Tuple<string, System.Windows.Point>((int)(correction.weight * 100) + "%",
                        new System.Windows.Point(
                            correction.correction.сoordinates[0],
                            correction.correction.сoordinates[1])));
                shift_storage.calibration_window.UpdateCorrectionsLables(lables);
                shift_storage.calibration_window.UpdateCurrentCorrection(new EyeTrackerErrorCorrection(cursor_position, result));
            }

            return result;
        }

        private float GetShadeOpacity(
            ShiftsStorage.PointInfo source_of_shade,
            ShiftsStorage.PointInfo shaded_correction)
        {
            Debug.Assert(source_of_shade.distance <= shaded_correction.distance);

            float opacity = 1;

            float angle_in_percents =(float) (Helpers.GetAngleBetweenVectors(source_of_shade, shaded_correction) * 100 / Math.PI);
            Debug.Assert(angle_in_percents <= 100);

            // Opacity descendes gradualy in the sector between opaque and transparent sectors.
            if (angle_in_percents < calibration_mode.size_of_opaque_sector_in_percents)
                opacity = 1;
            else if (angle_in_percents > 100 - calibration_mode.size_of_transparent_sector_in_percents)
                opacity = 0;
            else
                opacity = (angle_in_percents + calibration_mode.size_of_transparent_sector_in_percents - 100) /
                    (calibration_mode.size_of_opaque_sector_in_percents + calibration_mode.size_of_transparent_sector_in_percents - 100);

            float distance_from_shade_shell_to_shaded_correction = shaded_correction.distance - source_of_shade.distance;
            if (distance_from_shade_shell_to_shaded_correction < calibration_mode.shade_thickness_in_pixels)
                opacity *= distance_from_shade_shell_to_shaded_correction / calibration_mode.shade_thickness_in_pixels;

            return opacity;
        }
        private void ApplyShades(List<ShiftsStorage.PointInfo> corrections)
        {
            for (int i = 0; i < corrections.Count;)
            {
                corrections[i].weight = 1;

                for (int j = 0; j < i; j++)
                {
                    corrections[i].weight -= GetShadeOpacity(corrections[j], corrections[i]);
                }
                if (corrections[i].weight <= 0.01)
                    corrections.RemoveAt(i);
                else
                    i++;
            }
        }

        public void AddShift(float[] cursor_position, Point shift)
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

        public void SaveInDirectory(string directory_path)
        {
            shift_storage.SaveInDirectory(directory_path);
        }


        public bool IsDebugWindowEnabled
        {
            get => shift_storage.IsDebugWindowEnabled;
            set
            {
                shift_storage.IsDebugWindowEnabled = value;
            }
        }
    }
}
