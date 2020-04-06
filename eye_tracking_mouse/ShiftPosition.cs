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
            var scales_in_percents = mode.additional_dimensions_configuration.CoordinatesScalesInPercents;
            CoordinateScales = scales_in_percents.Select(x => x/100.0).ToArray();

            operator_minus_result_cache = new double[mode.max_zones_count][];
            for (int i = 0; i < mode.max_zones_count; i++ )
            {
                operator_minus_result_cache[i] = new double [CoordinateScales.Length];
            }
        }

        public double[] CoordinateScales {
            private set;
            get;
        }

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
                Debug.Assert(coordinates.Length == cache.CoordinateScales.Length);

                for (int i = 0; i < coordinates.Length; i++)
                {
                    adjusted_coordinates[i] = cache.CoordinateScales[i] * coordinates[i];
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
            get { return adjusted_coordinates[0]; }
        }

        [JsonIgnore]
        public double Y
        {
            get { return adjusted_coordinates[1]; }
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

        // WARNING: This is the main performance bottleneck. Measure performance before and after each change.
        public static double[] Subtract(ShiftPosition a, ShiftPosition b, out double distance)
        {
            Debug.Assert(a.coordinates.Length == b.coordinates.Length);
            Debug.Assert(a.cache == b.cache);

            var retval = a.cache.GetMemoryForNextResult();

            double dot_product = 0;

            retval[0] = a.adjusted_coordinates[0] - b.adjusted_coordinates[0]; dot_product += retval[0] * retval[0];
            retval[1] = a.adjusted_coordinates[1] - b.adjusted_coordinates[1]; dot_product += retval[1] * retval[1];
            retval[2] = a.adjusted_coordinates[2] - b.adjusted_coordinates[2]; dot_product += retval[2] * retval[2];
            retval[3] = a.adjusted_coordinates[3] - b.adjusted_coordinates[3]; dot_product += retval[3] * retval[3];
            retval[4] = a.adjusted_coordinates[4] - b.adjusted_coordinates[4]; dot_product += retval[4] * retval[4];
            retval[5] = a.adjusted_coordinates[5] - b.adjusted_coordinates[5]; dot_product += retval[5] * retval[5];
            retval[6] = a.adjusted_coordinates[6] - b.adjusted_coordinates[6]; dot_product += retval[6] * retval[6];
            retval[7] = a.adjusted_coordinates[7] - b.adjusted_coordinates[7]; dot_product += retval[7] * retval[7];

            //{
            //    var a_simd = new System.Numerics.Vector<double>(a.adjusted_coordinates, 0);
            //    var b_simd = new System.Numerics.Vector<double>(b.adjusted_coordinates, 0);
            //    var c_simd = (a_simd - b_simd);
            //    dot_product += System.Numerics.Vector.Dot(c_simd, c_simd);
            //    c_simd.CopyTo(retval, 0);
            //}
            //{
            //    var a_simd = new System.Numerics.Vector<double>(a.adjusted_coordinates, 4);
            //    var b_simd = new System.Numerics.Vector<double>(b.adjusted_coordinates, 4);
            //    var c_simd = (a_simd - b_simd);
            //    dot_product += System.Numerics.Vector.Dot(c_simd, c_simd);

            //    c_simd.CopyTo(retval, 4);
            //}

            distance = Math.Sqrt(dot_product);
            return retval;
        }
    }
}
