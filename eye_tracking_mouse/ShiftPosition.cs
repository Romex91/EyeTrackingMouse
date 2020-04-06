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
            coordinates_scales_in_percents = mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
            operator_minus_result_cache = new double[mode.max_zones_count][];
            for (int i = 0; i < mode.max_zones_count; i++ )
            {
                operator_minus_result_cache[i] = new double [coordinates_scales_in_percents.Length];
            }
        }

        public int[] coordinates_scales_in_percents = Options.Instance.calibration_mode.additional_dimensions_configuration.CoordinatesScalesInPercents;

        // For the purposes of optimisation results of |ShiftPosition.Operator-| are stored here.
        // Rationale is to avoid memory allocation during each algorithm iteration.
        private double[][] operator_minus_result_cache;
        private int last_operator_minus_result_index = 0;


        // WARNING. All |ShiftPosition.Operator-| results acquired in the past will become invalid!
        public void ClearCachedResults()
        {
            last_operator_minus_result_index = 0;
        }

        public double[] GetMemoryForNextResult()
        {
            Debug.Assert(
                last_operator_minus_result_index < operator_minus_result_cache.Length,
                "Either increase size of cache or add |ClearCachedResults()| call before calling |ShiftPosition.Operator-|");
            return operator_minus_result_cache[last_operator_minus_result_index++];
        }

        public override ShiftPosition Create(Type objectType)
        {
            return new ShiftPosition(this);
        }
    }

    // A multidimensional vector where first two coordinates represent 2d point on the display.
    // Other dimensions represent user body position.
    public class ShiftPosition
    {
        [JsonProperty]
        private double[] Coordinates
        {
            get { return coordinates; }
            set
            {
                coordinates = value;
                adjusted_coordinates = new double[coordinates.Length];
                Debug.Assert(coordinates.Length == cache.coordinates_scales_in_percents.Length);

                for (int i = 0; i < coordinates.Length; i++)
                {

                    adjusted_coordinates[i] = cache.coordinates_scales_in_percents[i] / 100.0 * coordinates[i];
                }
            }
        }

        [JsonIgnore]
        private double[] coordinates;

        [JsonIgnore]
        private double[] adjusted_coordinates;

        [JsonIgnore]
        private ShiftPositionCache cache;

        public ShiftPosition(ShiftPositionCache cache)
        {
            this.cache = cache;
        }

        public ShiftPosition(double[] coordinates, ShiftPositionCache cache)
        {
            this.cache = cache;
            this.Coordinates = coordinates;
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
                return adjusted_coordinates[i];
            }
        }

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

        public static double[] operator -(ShiftPosition a, ShiftPosition b)
        {
            Debug.Assert(a.coordinates.Length == b.coordinates.Length);
            Debug.Assert(a.cache == b.cache);

            var retval = a.cache.GetMemoryForNextResult();

            for (int i = 0; i < a.coordinates.Length; i++)
            {
                retval[i] = a.adjusted_coordinates[i] - b.adjusted_coordinates[i];
            }
            return retval;
        }
    }
}
