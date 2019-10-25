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
    class GazeSmoother
    {
        public GazeSmoother(Options options)
        {
            this.options = options;
        }

        public void AddGazePoint(Point point)
        {
            points.Insert(0, point);
            while (points.Count > options.smothening_points_count)
                points.RemoveAt(points.Count - 1);

            points.RemoveAll(p => {
                return Math.Pow(Math.Pow(p.X - point.X, 2) + Math.Pow(p.Y - point.Y, 2), 0.5) > options.smothening_zone_radius;
            });
        }

        public Point GetSmoothenedGazePoint()
        {
            double X = 0;
            double Y = 0;
            foreach(var point in points)
            {
                X += point.X;
                Y += point.Y;
            }

            return new Point( (int) (X / points.Count), (int)(Y / points.Count));
        }

        private readonly Options options;

        private readonly List<Point> points = new List<Point>();
    }
}
