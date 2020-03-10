using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace eye_tracking_mouse
{
    // Tracks history of gaze points deleting those which are too far from the last point.
    // Longer the user stares at one area smoother resulting gaze point.
    class CoordinateSmoother
    {
        public void AddPoint(double point)
        {
            points.Insert(0, point);
            while (points.Count > Options.Instance.smothening_points_count)
                points.RemoveAt(points.Count - 1);

            points.RemoveAll(p => {
                return Math.Abs(p - point) > Options.Instance.smothening_zone_radius;
            });
        }

        public double GetSmoothenedPoint()
        {
            double X = 0;
            foreach(var point in points)
            {
                X += point;
            }

            return X / points.Count;
        }

        private readonly List<double> points = new List<double>();
    }
}
