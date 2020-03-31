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
        public int[] coordinates_scales_in_percents = Options.Instance.calibration_mode.additional_dimensions_configuration.CoordinatesScalesInPercents;

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

        public static ShiftPosition operator -(ShiftPosition a, ShiftPosition b)
        {
            Debug.Assert(a.coordinates.Length == b.coordinates.Length);
            Debug.Assert(a.cache == b.cache);

            double[] coordinates = new double[a.coordinates.Length];

            for (int i = 0; i < a.coordinates.Length; i++)
                coordinates[i] = a.coordinates[i] - b.coordinates[i];
            return new ShiftPosition(coordinates, a.cache);
        }
    }
}
