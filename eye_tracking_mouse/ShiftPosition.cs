using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    // A multidimensional vector where first two coordinates represent 2d point on the display.
    // Other dimensions represent user body position.
    public class ShiftPosition
    {
        [JsonProperty]
        private readonly List<double> coordinates;

        [JsonIgnore]
        private readonly List<double> adjusted_coordinates;

        public ShiftPosition(List<double> coordinates)
        {
            this.coordinates = coordinates;
            adjusted_coordinates = new List<double>(coordinates.Count);
            for (int i = 0; i < coordinates.Count; i++)
            {
                if (i < 2)
                    adjusted_coordinates.Add(coordinates[i]);
                else
                    adjusted_coordinates.Add(coordinates[i] * Options.Instance.calibration_mode.multi_dimensions_detalization / 10.0);

            }
        }

        [JsonIgnore]
        public int Count
        {
            get
            {
                if (coordinates == null)
                    return 0;
                return coordinates.Count;
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
            Debug.Assert(a.coordinates.Count == b.coordinates.Count);
            List<double> coordinates = new List<double>(a.coordinates.Count);

            for (int i = 0; i < a.coordinates.Count; i++)
                coordinates.Add(a.coordinates[i] - b.coordinates[i]);
            return new ShiftPosition(coordinates);
        }
    }
}
