using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BlindConfigurationTester
{
    public struct DataPoint
    {
        public eye_tracking_mouse.TobiiCoordinates tobii_coordinates;
        public Point true_location_on_screen;
    }
    public struct Session
    {
        public struct PointsSequence
        {
            public int seed;
            public int points_count;
        }
        public int size_of_circle;
        public string instructions;
        public string tag;

        public PointsSequence[] points_sequences;

        public List<DataPoint> Start(bool data_set_mode, int rand_seed)
        {
            if ((new SessionInstructionsWindow(instructions)).ShowDialog() != true)
                return null;

            if (!data_set_mode && !InputInitWindow.input.IsLoaded)
                (new InputInitWindow()).ShowDialog();


            List<Tuple<int, int>> points = new List<Tuple<int, int>>();

            foreach (var points_sequence in points_sequences)
            {
                var rand = new Random(points_sequence.seed);

                for (int i = 0; i < points_sequence.points_count; i++)
                    points.Add(new Tuple<int, int>(rand.Next(0, int.MaxValue), rand.Next(0, int.MaxValue)));
            }

            var session_window = new SessionWindow(points, size_of_circle, data_set_mode);
            session_window.ShowDialog();

            return session_window.DataPoints;
        }
    }
}
