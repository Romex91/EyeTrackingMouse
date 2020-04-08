using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    public class ShiftStorageCache
    {
        public ShiftStorageCache(Options.CalibrationMode mode)
        {
            this.mode = mode;
            var scales_in_percents = mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
            CoordinateScales = scales_in_percents.Select(x => x/100.0).ToArray();

            CoordinatesCache = new double[ 8 + 17 * mode.max_zones_count];

            SavedCoordinatesStartingIndex = 8;
            SubtractResultsStartingIndex = 8 * mode.max_zones_count + SavedCoordinatesStartingIndex;
            DistancesStartingIndex = 8 * mode.max_zones_count + SubtractResultsStartingIndex;
        }

        public double[] CoordinateScales {
            private set;
            get;
        }

        // All coordinates involved in the performance bottleneck calculations are stored here.
        // Rationale is to decrease number of cache misses.
        // The coordinates count is alligned to 8-dimensions. 
        //    E.G. if you use 6-dimensional space last two dimensions will always be zero.
        // First 8-doubles are allocated for cursor position.
        // Next 8 * mode.max_zones_count are allocated for adjusted coordinates of saved points
        // After cursor position changes there is calculation of distance from cursor position to each saved point.
        // Next 8 * mode.max_zones_count are allocated for the results of subtract operation.
        // Next mode.max_zones_count are allocated for the results of distance calculations.
        public double[] CoordinatesCache {
            private set;
            get;
        }

        public class PointInfo
        {
            public double[] vector_from_correction_to_cursor;
            public int index;
            public double distance;
            public double weight;
        }

        public List<PointInfo> ClosestPoints
        {
            private set;
            get;
        }

        public int SavedCoordinatesStartingIndex
        {
            private set;
            get;
        }
        public int SubtractResultsStartingIndex
        {
            private set;
            get;
        }
        public int DistancesStartingIndex
        {
            private set;
            get;
        }

        public int AllocateIndex()
        {
            return number_of_shift_positions++;
        }

        public void FreeIndex(int index)
        {
            number_of_shift_positions--;
            for (int i = index; i < number_of_shift_positions; ++i)
            {
                int saved_coordinates_index = SavedCoordinatesStartingIndex + i * 8;
                for (int j = 0; j < 8; j++)
                    CoordinatesCache[saved_coordinates_index + j] =
                        CoordinatesCache[saved_coordinates_index + j + 8];
            }
        }

        public void Clear()
        {
            number_of_shift_positions = 0;
        }

        private int GetCoordinatesShift(int index)
        {
            return index == -1 ? 0 : SavedCoordinatesStartingIndex + index * 8;
        }

        public void SaveToCache(double[] coordinates, int cache_index)
        {
            Debug.Assert(coordinates.Length <= 8);
            Debug.Assert(coordinates.Length == CoordinateScales.Length);

            int i = 0;
            for (; i < coordinates.Length; ++i)
            {
                CoordinatesCache[GetCoordinatesShift(cache_index) + i] =
                    CoordinateScales[i] * coordinates[i];
            }
            for (; i < 8; ++i)
            {
                CoordinatesCache[GetCoordinatesShift(cache_index) + i] = 0;
            }

        }

        // Performs cacluclation of distance from cursor to each saved point.
        // WARNING: This is the main performance bottleneck. Measure performance before and after each change.
        public void ChangeCursorPosition(double[] coordinates)
        {
            SaveToCache(coordinates, -1);
            FindDistancesFromCursor();
            FindClosestPoints();
        }

        private void FindDistancesFromCursor()
        {
            // Subtract 
            for (int i = 0; i < number_of_shift_positions * 8; i += 8)
            {
                int saved_coordinates_index = i + SavedCoordinatesStartingIndex;
                int subtract_results_index = i + SubtractResultsStartingIndex;
                for (int j = 0; j < 8; ++j)
                {
                    CoordinatesCache[subtract_results_index + j] = CoordinatesCache[saved_coordinates_index + j] - CoordinatesCache[j];
                }
            }

            // Length of subtract results
            for (int i = 0; i < number_of_shift_positions; ++i)
            {
                int subtract_results_index = i * 8 + SubtractResultsStartingIndex;
                double dot_product = 0;
                for (int j = 0; j < 8; ++j)
                {
                    double k = CoordinatesCache[subtract_results_index + j];
                    dot_product += k * k;
                }
                int distance_index = i + DistancesStartingIndex;
                CoordinatesCache[distance_index] = Math.Sqrt(dot_product);
            }
        }

        // Find indexes of points closest to cursor position.
        private void FindClosestPoints()
        {
            int indexes_count = 0;

            int[] indexes_closest_to_cursor = new int[Math.Min(mode.considered_zones_count, number_of_shift_positions)];
            for (int i = 0; i < indexes_closest_to_cursor.Length; i++)
            {
                indexes_closest_to_cursor[i] = int.MaxValue;
            }
            for (int i = DistancesStartingIndex; i < DistancesStartingIndex + number_of_shift_positions; ++i)
            {
                int j = 0;
                for (; j < indexes_count; j++)
                {
                    if (CoordinatesCache[i] < CoordinatesCache[indexes_closest_to_cursor[j]])
                    {
                        for (int k = indexes_closest_to_cursor.Length - 1; k > j; k--)
                        {
                            indexes_closest_to_cursor[k] = indexes_closest_to_cursor[k - 1];
                        }
                        indexes_closest_to_cursor[j] = i;
                        if (indexes_count < indexes_closest_to_cursor.Length)
                            indexes_count++;
                        j = indexes_closest_to_cursor.Length;
                        break;
                    }
                }
                if (j == indexes_count && indexes_count < indexes_closest_to_cursor.Length)
                {
                    indexes_closest_to_cursor[j] = i;
                    indexes_count++;
                }
            }

            if (indexes_closest_to_cursor.Length == 0)
            {
                ClosestPoints = null;
                return;
            }

            ClosestPoints = new List<PointInfo>(indexes_closest_to_cursor.Length);
            foreach (var index in indexes_closest_to_cursor)
            {
                ClosestPoints.Add(new PointInfo
                {
                    distance = CoordinatesCache[index],
                    index = index - DistancesStartingIndex,
                    vector_from_correction_to_cursor = GetSubtractionResult(index - DistancesStartingIndex),
                    weight = 1
                });
            }
        }
        public double[] GetSubtractionResult(int cache_index)
        {
                Debug.Assert(cache_index >= 0);

                var retval = new double[CoordinateScales.Length];
                int subtract_results_index = SubtractResultsStartingIndex + cache_index * 8;
                for (int i = 0; i < retval.Length; i++)
                {
                    retval[i] = CoordinatesCache[subtract_results_index + i];
                }

                return retval;
        }

        public double GetDistanceFromCursor(int cache_index)
        {
                Debug.Assert(cache_index >= 0);
                return CoordinatesCache[DistancesStartingIndex + cache_index];
        }

        private Options.CalibrationMode mode;
        private int number_of_shift_positions = 0;
    }
}
