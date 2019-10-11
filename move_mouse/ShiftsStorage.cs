using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

namespace move_mouse
{
    class ShiftsStorage
    {
        public Point GetShift(Point cursor_position)
        {
            var closest_indices = GetClosestShiftIndexes(cursor_position);
            if (closest_indices == null)
            {
                Debug.Assert(shifts_.Count() == 0);
                return new Point(0, 0);
            }

            double sum_of_reverse_distances = 0;
            foreach(var index in closest_indices)
            {
                sum_of_reverse_distances += (1/index.Item2);
            }

            Point resulting_shift = new Point(0, 0);
            foreach (var index in closest_indices)
            {
                resulting_shift.X += (int)(shifts_[index.Item1].Item2.X / index.Item2 / sum_of_reverse_distances);
                resulting_shift.Y += (int)(shifts_[index.Item1].Item2.Y / index.Item2 / sum_of_reverse_distances);
            }

            return resulting_shift;
        }

        public void Reset()
        {
            shifts_.Clear();
        }
        public void ResetClosest(Point position)
        {
            var closest_indices = GetClosestShiftIndexes(position);
            if (closest_indices != null)
            {
                shifts_.RemoveAt(closest_indices[0].Item1);
            }
        }
        public void AddShift(Point cursor_position, Point shift)
        {
            var indices = GetClosestShiftIndexes(cursor_position);
            if (shifts_.Count() < max_size_)
            {
                if (indices == null || indices[0].Item2 > 150)
                    shifts_.Add(new Tuple<Point, Point>(cursor_position, shift));
                else
                    shifts_[indices[0].Item1] = new Tuple<Point, Point>(cursor_position, shift);
            }
            else
            {
                shifts_[indices[0].Item1] = new Tuple<Point, Point>(cursor_position, shift);
            }
        }
        private Tuple<int /*index*/, double /*distance*/>[] GetClosestShiftIndexes(Point cursor_position)
        {
            if (shifts_.Count() == 0)
                return null;

            Tuple<int, double>[] retval = new Tuple<int, double>[3] {
                new Tuple<int, double>(-1, double.MaxValue),
                new Tuple<int, double>(-1, double.MaxValue),
                new Tuple<int, double>(-1, double.MaxValue),
            };

            for (int i = 0; i < shifts_.Count(); i++)
            {
                double distance = Math.Pow(Math.Pow(shifts_[i].Item1.X - cursor_position.X, 2) + Math.Pow(shifts_[i].Item1.Y - cursor_position.Y, 2), 0.5);
                if (distance < 1)
                    distance = 1;

                if (distance < retval[0].Item2)
                {
                    retval[2] = retval[1];
                    retval[1] = retval[0];
                    retval[0] = new Tuple<int, double>(i, distance);
                    if (retval[1].Item1 == -1)
                        retval[1] = retval[0];
                    if (retval[2].Item1 == -1)
                        retval[2] = retval[0];
                }
                else if (distance < retval[1].Item2)
                {
                    retval[2] = retval[1];
                    retval[1] = new Tuple<int, double>(i, distance);
                }
                else if (distance < retval[1].Item2)
                {
                    retval[2] = new Tuple<int, double>(i, distance);
                }
            }
            return retval;
        }

        private List<Tuple<Point, Point>> shifts_ = new List<Tuple<Point, Point>>();
        private const int max_size_ = 40;

    }
}
