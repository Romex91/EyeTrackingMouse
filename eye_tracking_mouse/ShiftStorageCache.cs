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
        static readonly int AlignedCoordinatesCount = Math.Max(8, System.Numerics.Vector<double>.Count);

        public ShiftStorageCache(Options.CalibrationMode mode)
        {
            this.mode = mode;

            int aligned_max_zones_count = 8;
            for (; aligned_max_zones_count < this.mode.max_zones_count; aligned_max_zones_count *= 2) ;
            this.mode.max_zones_count = aligned_max_zones_count;

            var scales_in_percents = mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
            CoordinateScales = scales_in_percents.Select(x => x/100.0).ToArray();

            CoordinatesCache = new double[ 
                AlignedCoordinatesCount +                                   // cursor position
                AlignedCoordinatesCount * mode.max_zones_count +            // cached coordinates
                AlignedCoordinatesCount * mode.max_zones_count +            // subtruction results
                mode.max_zones_count];                                      // distances

            SavedCoordinatesStartingIndex = AlignedCoordinatesCount;
            SubtractResultsStartingIndex = AlignedCoordinatesCount * mode.max_zones_count + SavedCoordinatesStartingIndex;
            DistancesStartingIndex = AlignedCoordinatesCount * mode.max_zones_count + SubtractResultsStartingIndex;
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
                int saved_coordinates_index = SavedCoordinatesStartingIndex + i * AlignedCoordinatesCount;
                for (int j = 0; j < AlignedCoordinatesCount; j++)
                    CoordinatesCache[saved_coordinates_index + j] =
                        CoordinatesCache[saved_coordinates_index + j + AlignedCoordinatesCount];
            }
        }

        public void Clear()
        {
            number_of_shift_positions = 0;
        }

        private int GetCoordinatesShift(int index)
        {
            return index == -1 ? 0 : SavedCoordinatesStartingIndex + index * AlignedCoordinatesCount;
        }

        public void SaveToCache(double[] coordinates, int cache_index)
        {
            Debug.Assert(coordinates.Length <= AlignedCoordinatesCount);
            Debug.Assert(coordinates.Length == CoordinateScales.Length);

            int i = 0;
            for (; i < coordinates.Length; ++i)
            {
                CoordinatesCache[GetCoordinatesShift(cache_index) + i] =
                    CoordinateScales[i] * coordinates[i];
            }
            for (; i < AlignedCoordinatesCount; ++i)
            {
                CoordinatesCache[GetCoordinatesShift(cache_index) + i] = 0;
            }

        }

        // Performs cacluclation of distance from cursor to each saved point.
        // WARNING: This is the main performance bottleneck. Measure performance before and after each change.
        public void ChangeCursorPosition(double[] coordinates)
        {
            SaveToCache(coordinates, -1);
            FindDistancesFromCursor_SIMD();
            FindClosestPoints();
        }

        private void FindDistancesFromCursor_SIMD()
        {
            int vector_size = System.Numerics.Vector<double>.Count;
            int vectors_per_point = AlignedCoordinatesCount / vector_size;

            System.Numerics.Vector<double>[] cursor_position = new System.Numerics.Vector<double>[vectors_per_point];
            for (int i = 0; i < vectors_per_point; ++i)
            {
                cursor_position[i] = new System.Numerics.Vector<double>(
                    CoordinatesCache,
                    i * vector_size);
            }

            System.Numerics.Vector<double> zero = System.Numerics.Vector<double>.Zero;
            for (int i = 0; i < number_of_shift_positions; i += vector_size)
            {
                zero.CopyTo(CoordinatesCache, i + DistancesStartingIndex);
            }

            for (int i = 0; i < number_of_shift_positions * vectors_per_point; ++i)
            {
                int saved_coordinates_index = i * vector_size + SavedCoordinatesStartingIndex;
                int subtract_results_index = i * vector_size + SubtractResultsStartingIndex;

                var saved_coordinates = new System.Numerics.Vector<double>(
                    CoordinatesCache, 
                    saved_coordinates_index);
                var subtract_result = (saved_coordinates - cursor_position[i % vectors_per_point]);
                subtract_result.CopyTo(CoordinatesCache, subtract_results_index);

                CoordinatesCache[DistancesStartingIndex + i / vectors_per_point] +=
                    System.Numerics.Vector.Dot(subtract_result, subtract_result);
            }

            for (int i = 0; i < number_of_shift_positions; i += vector_size)
            {
                var dot_products_vec = new System.Numerics.Vector<double>(CoordinatesCache, i + DistancesStartingIndex);
                System.Numerics.Vector.SquareRoot(dot_products_vec).CopyTo(CoordinatesCache, i + DistancesStartingIndex);
            }
        }


        private void FindDistancesFromCursor()
        {
            // Subtract 
            for (int i = 0; i < number_of_shift_positions * AlignedCoordinatesCount; i += AlignedCoordinatesCount)
            {
                int saved_coordinates_index = i + SavedCoordinatesStartingIndex;
                int subtract_results_index = i + SubtractResultsStartingIndex;
                for (int j = 0; j < AlignedCoordinatesCount; ++j)
                {
                    CoordinatesCache[subtract_results_index + j] = CoordinatesCache[saved_coordinates_index + j] - CoordinatesCache[j];
                }
            }

            // Length of subtract results
            for (int i = 0; i < number_of_shift_positions; ++i)
            {
                int subtract_results_index = i * AlignedCoordinatesCount + SubtractResultsStartingIndex;
                double dot_product = 0;
                for (int j = 0; j < AlignedCoordinatesCount; ++j)
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
                    distance = CoordinatesCache[index] > 0.0001 ? CoordinatesCache[index] : 0.0001,
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
                int subtract_results_index = SubtractResultsStartingIndex + cache_index * AlignedCoordinatesCount;
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
