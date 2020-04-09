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
        // All coordinates involved in the performance bottleneck calculations are stored here.
        // Rationale is to decrease number of cache misses.
        // The coordinates count is alligned to 8-dimensions. 
        //    E.G. if you use 6-dimensional space last two dimensions will always be zero.
        // First 8-doubles are allocated for cursor position.
        // Next 8 * mode.max_zones_count are allocated for adjusted coordinates of saved points
        // After cursor position changes there is calculation of distance from cursor position to each saved point.
        // Next 8 * mode.max_zones_count are allocated for the results of subtract operation.
        // Next mode.max_zones_count are allocated for the results of distance calculations.
        private readonly double[] cached_data;

        private struct StartingIndex
        {
            public int cached_coordinates;
            public int subtract_results;
            public int distances;
        }
        private readonly StartingIndex starting_index;
        private Options.CalibrationMode mode;
        private int number_of_shift_positions = 0;
        private double[] coordinate_scales;

        public ShiftStorageCache(Options.CalibrationMode mode)
        {
            this.mode = mode;

            int aligned_max_zones_count = 8;
            for (; aligned_max_zones_count < this.mode.max_zones_count; aligned_max_zones_count *= 2) ;
            this.mode.max_zones_count = aligned_max_zones_count;

            var scales_in_percents = mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
            coordinate_scales = scales_in_percents.Select(x => x/100.0).ToArray();

            cached_data = new double[ 
                AlignedCoordinatesCount +                                   // cursor position
                AlignedCoordinatesCount * mode.max_zones_count +            // cached coordinates
                AlignedCoordinatesCount * mode.max_zones_count +            // subtruction results
                mode.max_zones_count];                                      // distances

            starting_index = new StartingIndex
            {
                cached_coordinates = AlignedCoordinatesCount,
                subtract_results = AlignedCoordinatesCount * mode.max_zones_count + AlignedCoordinatesCount,
                distances = 2 * AlignedCoordinatesCount * mode.max_zones_count + AlignedCoordinatesCount
            };
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


        public int AllocateIndex()
        {
            return number_of_shift_positions++;
        }

        public void FreeIndex(int index)
        {
            number_of_shift_positions--;
            for (int i = index; i < number_of_shift_positions; ++i)
            {
                int saved_coordinates_index = starting_index.cached_coordinates + i * AlignedCoordinatesCount;
                for (int j = 0; j < AlignedCoordinatesCount; j++)
                    cached_data[saved_coordinates_index + j] =
                        cached_data[saved_coordinates_index + j + AlignedCoordinatesCount];
            }
        }

        public void Clear()
        {
            number_of_shift_positions = 0;
        }

        public void SaveToCache(double[] coordinates, int cache_index)
        {
            Debug.Assert(coordinates.Length <= AlignedCoordinatesCount);
            Debug.Assert(coordinates.Length == coordinate_scales.Length);

            int i = 0;
            int coordinates_shift =  starting_index.cached_coordinates + cache_index * AlignedCoordinatesCount;
            for (; i < coordinates.Length; ++i)
            {
                cached_data[coordinates_shift + i] =
                    coordinate_scales[i] * coordinates[i];
            }
            for (; i < AlignedCoordinatesCount; ++i)
            {
                cached_data[coordinates_shift + i] = 0;
            }
        }

        private double[] GetSubtractionResult(int cache_index)
        {
            Debug.Assert(cache_index >= 0);

            var retval = new double[coordinate_scales.Length];
            int subtract_results_index = starting_index.subtract_results + cache_index * AlignedCoordinatesCount;
            for (int i = 0; i < retval.Length; i++)
            {
                retval[i] = cached_data[subtract_results_index + i];
            }

            return retval;
        }

        public double GetDistanceFromCursor(int cache_index)
        {
            Debug.Assert(cache_index >= 0);
            return cached_data[starting_index.distances + cache_index];
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
                    cached_data,
                    i * vector_size);
            }

            System.Numerics.Vector<double> zero = System.Numerics.Vector<double>.Zero;
            for (int i = 0; i < number_of_shift_positions; i += vector_size)
            {
                zero.CopyTo(cached_data, i + starting_index.distances);
            }

            for (int i = 0; i < number_of_shift_positions * vectors_per_point; ++i)
            {
                int saved_coordinates_index = i * vector_size + starting_index.cached_coordinates;
                int subtract_results_index = i * vector_size + starting_index.subtract_results;

                var saved_coordinates = new System.Numerics.Vector<double>(
                    cached_data, 
                    saved_coordinates_index);
                var subtract_result = (saved_coordinates - cursor_position[i % vectors_per_point]);
                subtract_result.CopyTo(cached_data, subtract_results_index);

                cached_data[starting_index.distances + i / vectors_per_point] +=
                    System.Numerics.Vector.Dot(subtract_result, subtract_result);
            }

            for (int i = 0; i < number_of_shift_positions; i += vector_size)
            {
                var dot_products_vec = new System.Numerics.Vector<double>(cached_data, i + starting_index.distances);
                System.Numerics.Vector.SquareRoot(dot_products_vec).CopyTo(cached_data, i + starting_index.distances);
            }
        }


        private void FindDistancesFromCursor()
        {
            // Subtract 
            for (int i = 0; i < number_of_shift_positions * AlignedCoordinatesCount; i += AlignedCoordinatesCount)
            {
                int saved_coordinates_index = i + starting_index.cached_coordinates;
                int subtract_results_index = i + starting_index.subtract_results;
                for (int j = 0; j < AlignedCoordinatesCount; ++j)
                {
                    cached_data[subtract_results_index + j] = cached_data[saved_coordinates_index + j] - cached_data[j];
                }
            }

            // Length of subtract results
            for (int i = 0; i < number_of_shift_positions; ++i)
            {
                int subtract_results_index = i * AlignedCoordinatesCount + starting_index.subtract_results;
                double dot_product = 0;
                for (int j = 0; j < AlignedCoordinatesCount; ++j)
                {
                    double k = cached_data[subtract_results_index + j];
                    dot_product += k * k;
                }
                int distance_index = i + starting_index.distances;
                cached_data[distance_index] = Math.Sqrt(dot_product);
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
            for (int i = starting_index.distances; i < starting_index.distances + number_of_shift_positions; ++i)
            {
                int j = 0;
                for (; j < indexes_count; j++)
                {
                    if (cached_data[i] < cached_data[indexes_closest_to_cursor[j]])
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
                    distance = cached_data[index] > 0.0001 ? cached_data[index] : 0.0001,
                    index = index - starting_index.distances,
                    vector_from_correction_to_cursor = GetSubtractionResult(index - starting_index.distances),
                    weight = 1
                });
            }
        }
    }
}
