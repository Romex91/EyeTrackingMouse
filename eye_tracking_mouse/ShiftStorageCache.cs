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
        // First 8 * mode.max_zones_count are allocated for adjusted coordinates of saved points
        // After cursor position changes there is calculation of distance from cursor position to each saved point
        // Next 8 * mode.max_zones_count are allocated for the results of subtract operation.
        private readonly double[] cached_data;
        private readonly double[] cached_distances;
        private readonly long[] distance_mask;
        private readonly double[] cursor_coordinates;
        private double distance_mask_treshold = 100;

        public int subtract_results_starting_index;
        private int number_of_shift_positions;

        private Options.CalibrationMode mode;
        private double[] coordinate_scales;

        public ShiftStorageCache(Options.CalibrationMode mode)
        {
            this.mode = mode;

            int aligned_max_zones_count = 8;
            for (; aligned_max_zones_count < this.mode.max_zones_count; aligned_max_zones_count *= 2) ;
            this.mode.max_zones_count = aligned_max_zones_count;

            var scales_in_percents = mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
            coordinate_scales = scales_in_percents.Select(x => x / 100.0).ToArray();

            cached_data = new double[
                AlignedCoordinatesCount * mode.max_zones_count +            // cached coordinates
                AlignedCoordinatesCount * mode.max_zones_count              // subtruction results
                ];

            cached_distances = new double[mode.max_zones_count];
            distance_mask = new long[mode.max_zones_count];
            cursor_coordinates = new double[AlignedCoordinatesCount];

            subtract_results_starting_index = AlignedCoordinatesCount * mode.max_zones_count;
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
                int saved_coordinates_index = i * AlignedCoordinatesCount;
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
            int coordinates_shift = cache_index * AlignedCoordinatesCount;
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
            int subtract_results_index = subtract_results_starting_index + cache_index * AlignedCoordinatesCount;
            for (int i = 0; i < retval.Length; i++)
            {
                retval[i] = cached_data[subtract_results_index + i];
            }

            return retval;
        }

        public double GetDistanceFromCursor(int cache_index)
        {
            Debug.Assert(cache_index >= 0);
            return cached_distances[cache_index];
        }

        // Performs cacluclation of distance from cursor to each saved point.
        // WARNING: This is the main performance bottleneck. Measure performance before and after each change.
        public void ChangeCursorPosition(double[] coordinates)
        {
            int i = 0;
            for (; i < coordinates.Length; ++i)
                cursor_coordinates[i] = coordinate_scales[i] * coordinates[i];
            for (; i < cursor_coordinates.Length; ++i)
                cursor_coordinates[i] = 0;
            FindDistancesFromCursor_SIMD(this);
            FindClosestPoints();
        }

        // This function is static to move all used class fields from heap to stack.
        // Doing so improves perf test results.
        private static void FindDistancesFromCursor_SIMD(ShiftStorageCache cache)
        {
            int vector_size = System.Numerics.Vector<double>.Count;
            int vectors_per_point = AlignedCoordinatesCount / vector_size;

            double[] cached_data = cache.cached_data;
            double[] distances = cache.cached_distances;
            long[] distance_mask = cache.distance_mask;
            double[] cursor_coordinates = cache.cursor_coordinates;
            double distance_mask_treshold = cache.distance_mask_treshold;
            int subtract_results_starting_index = cache.subtract_results_starting_index;
            int number_of_shift_positions = cache.number_of_shift_positions;

            int subtract_iterator = 0;
            int number_of_considered_points = Math.Min(cache.mode.considered_zones_count, number_of_shift_positions);

            System.Numerics.Vector<double>[] cursor_position = new System.Numerics.Vector<double>[vectors_per_point];
            int i = 0;
            for (i = 0; i < vectors_per_point; ++i)
            {
                cursor_position[i] = new System.Numerics.Vector<double>(
                    cursor_coordinates,
                    i * vector_size);
            }

            for (int distances_iterator = 0; distances_iterator < number_of_shift_positions; ++distances_iterator)
            {
                double dot_product = 0;
                for (int j = 0; j < vectors_per_point; ++j)
                {
                    var saved_coordinates = new System.Numerics.Vector<double>(
                        cached_data,
                        subtract_iterator);
                    var subtract_result = (saved_coordinates - cursor_position[j]);
                    subtract_result.CopyTo(cached_data, subtract_iterator + subtract_results_starting_index);

                    dot_product +=
                        System.Numerics.Vector.Dot(subtract_result, subtract_result);

                    subtract_iterator += vector_size;
                }
                distances[distances_iterator] = dot_product;
            }

            System.Numerics.Vector<double> distance_filter_vector = new System.Numerics.Vector<double>(distance_mask_treshold);
            long points_count_after_filtering = 0;
            
            for (i = 0; i < number_of_shift_positions; i += vector_size)
            {
                var dot_products_vec = new System.Numerics.Vector<double>(distances, i);
                var distance_vec = System.Numerics.Vector.SquareRoot(dot_products_vec);
                distance_vec.CopyTo(distances, i);
                var filter_vec = System.Numerics.Vector.LessThan(distance_vec, distance_filter_vector);
                points_count_after_filtering += System.Numerics.Vector.Dot(filter_vec, filter_vec);
                filter_vec.CopyTo(distance_mask, i);
            }

            points_count_after_filtering -= i - number_of_shift_positions;

            if (points_count_after_filtering < number_of_considered_points)
            {
                cache.distance_mask_treshold *= 2;
                for (i = 0; i < number_of_shift_positions; i += vector_size)
                {
                    System.Numerics.Vector<long>.One.CopyTo(distance_mask, i);
                }
            }
            else if (points_count_after_filtering > number_of_considered_points * 4)
            {
                cache.distance_mask_treshold /= 1.1;
            }
        }
        private void FindDistancesFromCursor(double[] cursor_coordinates)
        {
            // Subtract 
            for (int i = 0; i < number_of_shift_positions * AlignedCoordinatesCount; i += AlignedCoordinatesCount)
            {
                int subtract_results_index = i + subtract_results_starting_index;
                for (int j = 0; j < AlignedCoordinatesCount; ++j)
                {
                    cached_data[subtract_results_index + j] = cached_data[i + j] - cursor_coordinates[j];
                }
            }

            // Length of subtract results
            for (int i = 0; i < number_of_shift_positions; ++i)
            {
                int subtract_results_index = i * AlignedCoordinatesCount + subtract_results_starting_index;
                double dot_product = 0;
                for (int j = 0; j < AlignedCoordinatesCount; ++j)
                {
                    double k = cached_data[subtract_results_index + j];
                    dot_product += k * k;
                }
                cached_distances[i] = Math.Sqrt(dot_product);
                distance_mask[i] = 1;
            }
        }

        // Find indexes of points closest to cursor position.
        private void FindClosestPoints()
        {
            int considered_points_count = Math.Min(mode.considered_zones_count, number_of_shift_positions);
            if (considered_points_count == 0)
            {
                ClosestPoints = null;
                return;
            }


            int[] indexes = new int[considered_points_count];
            double[] distances = new double[considered_points_count];

            int i = 0;
            for (; i < indexes.Length; ++i)
            {
                indexes[i] = i;
                distances[i] = cached_distances[i];
            }

            for (; i < number_of_shift_positions; ++i)
            {
                if (distance_mask[i] == 0)
                    continue;

                int index = i;
                double distance = cached_distances[i];

                for (int j = 0; j < indexes.Length; j++)
                {
                    if (distance < distances[j])
                    {
                        double tmp_distance = distances[j];
                        int tmp_index = indexes[j];
                        distances[j] = distance;
                        indexes[j] = index;
                        distance = tmp_distance;
                        index = tmp_index;
                    }
                }
            }

            ClosestPoints = new List<PointInfo>(indexes.Length);
            for (i = 0; i < indexes.Length; i++)
            {
                ClosestPoints.Add(new PointInfo
                {
                    distance = distances[i] > 0.0001 ? distances[i] : 0.0001,
                    index = indexes[i],
                    vector_from_correction_to_cursor = GetSubtractionResult(indexes[i]),
                    weight = 1
                });
            }
            ClosestPoints.Sort((x, y) => x.distance < y.distance ? -1 : 1);
        }
    }
}
