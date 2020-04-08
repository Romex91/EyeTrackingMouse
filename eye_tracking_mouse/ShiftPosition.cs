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
    public class ShiftPositionCache : CustomCreationConverter<ShiftPosition>
    {
        public ShiftPositionCache(Options.CalibrationMode mode)
        {
            this.mode = mode;
            var scales_in_percents = mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
            CoordinateScales = scales_in_percents.Select(x => x/100.0).ToArray();

            CoordinatesCache = new double[ 8 + 17 * mode.max_zones_count];

            SavedCoordinatesStartingIndex = 8;
            SubtractResultsStartingIndex = 8 * mode.max_zones_count + SavedCoordinatesStartingIndex;
            DistancesStartingIndex = 8 * mode.max_zones_count + SubtractResultsStartingIndex;

            cursor_position = new ShiftPosition(this, -1);
        }

        public double[] CoordinateScales {
            private set;
            get;
        }

        // All coordinates involved in the performance bottleneck calculations are stored here.
        // Rationale is to decrease number of cache misses.
        // The coordinates count is alligned to 8-dimensions. 
        //    E.G. if you use 6-dimensional space last two dimensions will always be zero.
        // First 8-doubles are allocated for |cursor_position|.
        // Next 8 * mode.max_zones_count are allocated for adjusted coordinates of saved |ShiftPosition|s
        // After |cursor_position| changes there is calculation of distance from |cursor_position| to each saved |ShiftPosition|.
        // Next 8 * mode.max_zones_count are allocated for the results of subtract operation.
        // Next mode.max_zones_count are allocated for the results of distance calculations.
        public double[] CoordinatesCache {
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
            if (vacant_indices.Count > 0)
            {
                int retval = vacant_indices.First();
                vacant_indices.Remove(retval);
                return retval;
            }


            return number_of_shift_positions++;
        }

        public void FreeIndex(int index)
        {
            vacant_indices.Add(index);
        }

        public void Clear()
        {
            vacant_indices.Clear();
            number_of_shift_positions = 0;
        }

        // WARNING: This is the main performance bottleneck. Measure performance before and after each change.
        // Performs length cacluclation to each saved |ShiftPosition|.
        public ShiftPosition ChangeCursorPosition(double[] coordinates)
        {
            cursor_position.Coordinates = coordinates;

            // Subtract 
            for(int i = 0; i < number_of_shift_positions * 8; i+= 8)
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
                int subtract_results_index = i  * 8 + SubtractResultsStartingIndex;
                double dot_product = 0;
                for (int j = 0; j < 8; ++j)
                {
                    double k = CoordinatesCache[subtract_results_index + j];
                    dot_product += k*k;
                }
                int distance_index = i + DistancesStartingIndex;
                CoordinatesCache[distance_index] = Math.Sqrt(dot_product);
            }

            return cursor_position;
        }

        private Options.CalibrationMode mode;
        private int number_of_shift_positions = 0;
        private SortedSet<int> vacant_indices = new SortedSet<int>();

        private ShiftPosition cursor_position;

        public override ShiftPosition Create(Type objectType)
        {
            return new ShiftPosition(this, AllocateIndex());
        }
    }

    // A multidimensional vector where first two coordinates represent 2d point on the display.
    // Other dimensions represent user body position.
    public class ShiftPosition
    {
        [JsonIgnore]
        private double[] coordinates;

        [JsonIgnore]
        private ShiftPositionCache cache;

        // -1 is for the temp ShiftPosition
        private int cache_index = -1;
        private readonly int distance_index;

        private int CoordinatesShift
        {
            get
            {
                return cache_index == -1 ? 0 : cache.SavedCoordinatesStartingIndex + cache_index * 8;
            }
        }

        public ShiftPosition SaveToLongTermMemory()
        {
            Debug.Assert(cache_index == -1);
            var result = new ShiftPosition(cache, cache.AllocateIndex());
            result.Coordinates = coordinates;
            return result;
        }
        public void DeleteFromLongTermMemory()
        {
            Debug.Assert(cache_index >= 0);
            cache.FreeIndex(cache_index);
            // Makes object unusable.
            cache_index = -2;
        }

        public ShiftPosition(ShiftPositionCache cache, int cache_index)
        {
            this.cache = cache;
            this.cache_index = cache_index;
            this.distance_index = cache.DistancesStartingIndex + cache_index;
        }

        [JsonProperty]
        public double[] Coordinates
        {
            get { return coordinates; }
            set
            {
                Debug.Assert(value.Length <= 8);
                coordinates = value;

              
                Debug.Assert(coordinates.Length == cache.CoordinateScales.Length);

                int i = 0;
                for (; i < coordinates.Length; ++i)
                {
                    cache.CoordinatesCache[CoordinatesShift + i] = cache.CoordinateScales[i] * coordinates[i]; 
                }
                for (; i < 8; ++i)
                {
                    cache.CoordinatesCache[CoordinatesShift + i] = 0;
                }
            }
        }

        public double[] SubtractionResult
        {
            get
            {
                Debug.Assert(cache_index >= 0);

                var retval = new double[coordinates.Length];
                int subtract_results_index = cache.SubtractResultsStartingIndex + cache_index * 8;
                for (int i = 0; i < retval.Length; i++)
                {
                    retval[i] = cache.CoordinatesCache[subtract_results_index + i];
                }

                return retval;
            }
        }

        public double DistanceFromCursor
        {
            get
            {
                Debug.Assert(cache_index >= 0);
                return cache.CoordinatesCache[distance_index];
            }
        }


        [JsonIgnore]
        public int Count
        {
            get
            {
                if (coordinates == null)
                    return 0;
                return coordinates.Length;
            }
        }

        public double this[int i]
        {
            get
            {
                return cache.CoordinatesCache[i + CoordinatesShift];
            }
        }

        [JsonIgnore]
        public double X
        {
            get { return this[0]; }
        }

        [JsonIgnore]
        public double Y
        {
            get { return this[1]; }
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
    }
}
