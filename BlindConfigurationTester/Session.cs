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
        public Point error;
    }
    public struct Session
    {
        public int points_count;
        public int size_of_circle;
        public string instructions;

        public List<DataPoint> Start(bool data_set_mode, int rand_seed)
        {
            if ((new SessionInstructionsWindow(instructions)).ShowDialog() != true)
                return null;

            if (!data_set_mode && !InputInitWindow.input.IsLoaded)
                (new InputInitWindow()).ShowDialog();

            var rand = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId));

            List<Tuple<int, int>> points = new List<Tuple<int, int>>();
            for (int i = 0; i < points_count; i++)
                points.Add(new Tuple<int, int>(rand.Next(0, int.MaxValue), rand.Next(0, int.MaxValue)));

            var session_window = new SessionWindow(points, size_of_circle, data_set_mode);
            session_window.ShowDialog();
            return session_window.DataPoints;
        }
    }
}
