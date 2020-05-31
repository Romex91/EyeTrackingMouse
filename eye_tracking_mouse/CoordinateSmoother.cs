using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace eye_tracking_mouse
{
    public class Vector3Smoother
    {
        public Vector3Smoother()
        {
            smoothers = new CoordinateSmoother[3];
            for (int i = 0; i < 3; i++)
            {
                smoothers[i] = new CoordinateSmoother();
            }
        }

        public Tobii.Interaction.Vector3 SmoothPoint(Tobii.Interaction.Vector3 point)
        {
            smoothers[0].AddPoint((float)point.X);
            smoothers[1].AddPoint((float)point.Y);
            smoothers[2].AddPoint((float)point.Z);

            point.X = smoothers[0].GetSmoothenedPoint();
            point.Y = smoothers[1].GetSmoothenedPoint();
            point.Z = smoothers[2].GetSmoothenedPoint();

            return point;
        }

        private CoordinateSmoother[] smoothers;
    }

    public class PointSmoother
    {
        public PointSmoother()
        {
            smoothers = new CoordinateSmoother[2];
            for (int i = 0; i < 2; i++)
            {
                smoothers[i] = new CoordinateSmoother();
            }
        }

        public Point SmoothPoint(Point point)
        {
            smoothers[0].AddPoint(point.X);
            smoothers[1].AddPoint(point.Y);

            point.X = (int) smoothers[0].GetSmoothenedPoint();
            point.Y = (int) smoothers[1].GetSmoothenedPoint();

            return point;
        }

        private CoordinateSmoother[] smoothers;
    }

    // Tracks history of gaze points deleting those which are too far from the last point.
    // Longer the user stares at one area smoother resulting gaze point.
    class CoordinateSmoother
    {
        public void AddPoint(float point)
        {
            points.Insert(0, point);
            while (points.Count > Options.Instance.smothening_points_count)
                points.RemoveAt(points.Count - 1);

            points.RemoveAll(p =>
            {
                return Math.Abs(p - point) > Options.Instance.smothening_zone_radius;
            });
        }

        public float GetSmoothenedPoint()
        {
            float X = 0;
            foreach (var point in points)
            {
                X += point;
            }

            return X / points.Count;
        }

        private readonly List<float> points = new List<float>();
    }
}
