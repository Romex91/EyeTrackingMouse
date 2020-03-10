using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eye_tracking_mouse
{
    // Handles all interaction with the Eye Tracking Device. 
    // Provides as much coordinates as specified in |Options.Instance|.
    class TobiiCoordinatesProvider : IDisposable
    {
        private readonly Tobii.Interaction.Host host;
        private readonly Tobii.Interaction.GazePointDataStream gaze_point_data_stream;

        private readonly Tobii.Interaction.EyePositionStream eye_position_stream;
        private readonly Tobii.Interaction.HeadPoseStream head_pose_stream;

        private Point gaze_point = new Point(0, 0);

        private Tobii.Interaction.Vector3 left_eye = new Tobii.Interaction.Vector3();
        private Tobii.Interaction.Vector3 right_eye = new Tobii.Interaction.Vector3();
        private Tobii.Interaction.Vector3 angle_between_eyes = new Tobii.Interaction.Vector3();
        private Tobii.Interaction.Vector3 head_position = new Tobii.Interaction.Vector3();
        private Tobii.Interaction.Vector3 head_direction = new Tobii.Interaction.Vector3();

        CoordinateSmoother[] smoothers;
        private Action<List<double>> on_coordinates_callback;

        private DateTime last_gaze_point = DateTime.Now;

        private void OnHeadPose(double unused, Tobii.Interaction.Vector3 head_position, Tobii.Interaction.Vector3 head_direction)
        {
            lock (Helpers.locker)
            {
                if (!(head_position.X == 0 && head_position.Y == 0 && head_position.Z == 0))
                {
                    this.head_position = head_position;
                    this.head_direction = new Tobii.Interaction.Vector3(head_direction.X * 200, head_direction.Y * 200, head_direction.Z * 200);
                }
            }
        }


        private double GetAngleBetweenVectorAndXAxis(double x, double y)
        {
            return Math.Acos(x / Math.Sqrt(x * x + y * y));
        }

        private void OnEyePosition(Tobii.Interaction.EyePositionData obj)
        {
            lock (Helpers.locker)
            {
                if (obj.HasLeftEyePosition)
                {
                    var v = obj.LeftEyeNormalized;
                    this.left_eye = new Tobii.Interaction.Vector3(v.X * 200, v.Y * 100, v.Z * 500);
                }
                if (obj.HasRightEyePosition)
                {
                    var v = obj.RightEyeNormalized;
                    this.right_eye = new Tobii.Interaction.Vector3(v.X * 200, v.Y * 100, v.Z * 500);
                }


                if (obj.HasRightEyePosition && obj.HasLeftEyePosition)
                {
                    var vector = new Tobii.Interaction.Vector3(
                        obj.LeftEyeNormalized.X - obj.RightEyeNormalized.X,
                        obj.LeftEyeNormalized.Y - obj.RightEyeNormalized.Y,
                        (obj.LeftEyeNormalized.Z - obj.RightEyeNormalized.Z) * 50);
                    this.angle_between_eyes.X = GetAngleBetweenVectorAndXAxis(vector.Y, vector.X) * 180 / Math.PI;
                    this.angle_between_eyes.Y = GetAngleBetweenVectorAndXAxis(vector.Z, vector.X) * 180 / Math.PI;
                }
            }
        }

        private void OnGazePoint(double x, double y, double ts)
        {
            lock (Helpers.locker)
            {
                last_gaze_point = DateTime.Now;
                gaze_point = new Point((int)x, (int)y);

                List<double> coordinates = new List<double>();

                coordinates.Add(x);
                coordinates.Add(y);

                var config = Options.Instance.calibration_mode.additional_dimensions_configuration;
                foreach (var vector3 in new List<Tuple<Tobii.Interaction.Vector3, Vector3Bool>> {
                    new Tuple<Tobii.Interaction.Vector3, Vector3Bool> (left_eye, config.LeftEye),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Bool> (right_eye, config.RightEye),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Bool> (angle_between_eyes, config.AngleBetweenEyes),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Bool> (head_direction, config.HeadDirection),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Bool> (head_position, config.HeadPosition)
                   })
                {
                    if (vector3.Item2.X)
                        coordinates.Add(vector3.Item1.X);
                    if (vector3.Item2.Y)
                        coordinates.Add(vector3.Item1.Y);
                    if (vector3.Item2.Z)
                        coordinates.Add(vector3.Item1.Z);
                }

                Debug.Assert(smoothers.Length == coordinates.Count);
                for (int i = 0; i < coordinates.Count; i++)
                {
                    smoothers[i].AddPoint(coordinates[i]);
                    coordinates[i] = smoothers[i].GetSmoothenedPoint();
                }

                on_coordinates_callback?.Invoke(coordinates);
            }
        }

        public void UpdateTobiiStreams(object sender, EventArgs e)
        {
            lock (Helpers.locker)
            {
                AdditionalDimensionsConfguration config = Options.Instance.calibration_mode.additional_dimensions_configuration;
                smoothers = new CoordinateSmoother[config.CoordinatesCount];
                for (int i = 0; i < config.CoordinatesCount; i++)
                {
                    smoothers[i] = new CoordinateSmoother();
                }

                head_pose_stream.IsEnabled =
                    !config.HeadPosition.Equals(Vector3Bool.Disabled) ||
                    !config.HeadDirection.Equals(Vector3Bool.Disabled);

                eye_position_stream.IsEnabled =
                    !config.LeftEye.Equals(Vector3Bool.Disabled) ||
                    !config.RightEye.Equals(Vector3Bool.Disabled) ||
                    !config.AngleBetweenEyes.Equals(Vector3Bool.Disabled);

                Debug.Assert(config.AngleBetweenEyes.Z == false);

            }
        }

        public TobiiCoordinatesProvider(Action<List<double>> on_coordinates_callback)
        {
            this.on_coordinates_callback = on_coordinates_callback;
            try
            {
                host = new Tobii.Interaction.Host();
                gaze_point_data_stream = host.Streams.CreateGazePointDataStream();
                eye_position_stream = host.Streams.CreateEyePositionStream();
                head_pose_stream = host.Streams.CreateHeadPoseStream();

                UpdateTobiiStreams(null, null);

                Settings.CalibrationModeChanged += UpdateTobiiStreams;

                gaze_point_data_stream.GazePoint(OnGazePoint);
                eye_position_stream.EyePosition(OnEyePosition);
                head_pose_stream.HeadPose(OnHeadPose);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + ". Try reinstalling driver for Tobii Eye Tracker 4C.");
                Environment.Exit(0);
            }
        }

        // TODO: remove this when issue resolved:
        // https://developer.tobii.com/community/forums/topic/bug-tobii-interaction-gazepointdatastream-gazepoint-sometimes-has-no-effect/
        public void Restart()
        {
            if ((DateTime.Now - last_gaze_point).TotalSeconds > 1)
            {
                gaze_point_data_stream.IsEnabled = false;
                gaze_point_data_stream.IsEnabled = true;
            }
        }

        public void Dispose()
        {
            on_coordinates_callback = null;
            Settings.CalibrationModeChanged -= UpdateTobiiStreams;
            host.Dispose();
        }
    }
}
