﻿using Newtonsoft.Json;
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
    public struct TobiiCoordinates
    {
        public Point gaze_point;
        public Tobii.Interaction.Vector3 left_eye;
        public Tobii.Interaction.Vector3 right_eye;
        public Tobii.Interaction.Vector3 head_position;
        public Tobii.Interaction.Vector3 head_direction;
        [JsonIgnore]
        public Tobii.Interaction.Vector3 AngleBetweenEyes
        {
            get
            {
                var vector = new Tobii.Interaction.Vector3(
                    (left_eye.X - right_eye.X) / 200,
                    (left_eye.Y - right_eye.Y) / 100,
                    (left_eye.Z - right_eye.Z) / 10);

                return new Tobii.Interaction.Vector3(
                                GetAngleBetweenVectorAndXAxis(vector.Y, vector.X) * 180 / Math.PI,
                                GetAngleBetweenVectorAndXAxis(vector.Z, vector.X) * 180 / Math.PI,
                                0);
            }
        }


        private double GetAngleBetweenVectorAndXAxis(double x, double y)
        {
            return Math.Acos(x / Math.Sqrt(x * x + y * y));
        }
        public float[] ToCoordinates(AdditionalDimensionsConfguration config)
        {
            float[] coordinates = new float[config.CoordinatesCount];

            int index = 0;
            coordinates[index++] = gaze_point.X;
            coordinates[index++] = gaze_point.Y;

            foreach (var vector3 in new List<Tuple<Tobii.Interaction.Vector3, Vector3Percents>> {
                    new Tuple<Tobii.Interaction.Vector3, Vector3Percents> (left_eye, config.LeftEye),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Percents> (right_eye, config.RightEye),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Percents> (AngleBetweenEyes, config.AngleBetweenEyes),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Percents> (head_direction, config.HeadDirection),
                    new Tuple<Tobii.Interaction.Vector3, Vector3Percents> (head_position, config.HeadPosition)})
            {
                if (vector3.Item2.X > 0)
                    coordinates[index++] = (float)vector3.Item1.X;
                if (vector3.Item2.Y > 0)
                    coordinates[index++] = (float)vector3.Item1.Y;
                if (vector3.Item2.Z > 0)
                    coordinates[index++] = (float)vector3.Item1.Z;
            }
            return coordinates;
        }
    }

    // Handles all interaction with the Eye Tracking Device. 
    // Provides as much coordinates as specified in |Options.Instance|.
    public class TobiiCoordinatesProvider : IDisposable
    {
        private readonly Tobii.Interaction.Host host;
        private readonly Tobii.Interaction.GazePointDataStream gaze_point_data_stream;

        private readonly Tobii.Interaction.EyePositionStream eye_position_stream;
        private readonly Tobii.Interaction.HeadPoseStream head_pose_stream;

        private Action<TobiiCoordinates> on_coordinates_callback;
        TobiiCoordinates coordinates = new TobiiCoordinates
        {
            gaze_point = new Point(),
            left_eye = new Tobii.Interaction.Vector3(),
            right_eye = new Tobii.Interaction.Vector3(),
            head_position = new Tobii.Interaction.Vector3(),
            head_direction = new Tobii.Interaction.Vector3()
        };

        private DateTime last_gaze_point = DateTime.Now;

        private void OnHeadPose(double unused, Tobii.Interaction.Vector3 head_position, Tobii.Interaction.Vector3 head_direction)
        {
            lock (Helpers.locker)
            {
                if (!(head_position.X == 0 && head_position.Y == 0 && head_position.Z == 0))
                {
                    coordinates.head_position = head_position;
                    coordinates.head_direction =
                        new Tobii.Interaction.Vector3(head_direction.X * 200, head_direction.Y * 200, head_direction.Z * 200);
                }
            }
        }

        private void OnEyePosition(Tobii.Interaction.EyePositionData obj)
        {
            lock (Helpers.locker)
            {
                if (obj.HasLeftEyePosition)
                {
                    var v = obj.LeftEyeNormalized;
                    coordinates.left_eye = new Tobii.Interaction.Vector3(v.X * 200, v.Y * 100, v.Z * 500);
                }
                if (obj.HasRightEyePosition)
                {
                    var v = obj.RightEyeNormalized;
                    coordinates.right_eye = new Tobii.Interaction.Vector3(v.X * 200, v.Y * 100, v.Z * 500);
                }
            }
        }

        private void OnGazePoint(double x, double y, double ts)
        {
            lock (Helpers.locker)
            {
                last_gaze_point = DateTime.Now;
                coordinates.gaze_point = new Point((int)x, (int)y);
                on_coordinates_callback?.Invoke(coordinates);
            }
        }

        public void UpdateTobiiStreams(object sender, EventArgs e)
        {
            lock (Helpers.locker)
            {
                AdditionalDimensionsConfguration config = Options.Instance.calibration_mode.additional_dimensions_configuration;
                head_pose_stream.IsEnabled =
                    !config.HeadPosition.Equals(Vector3Percents.Disabled) ||
                    !config.HeadDirection.Equals(Vector3Percents.Disabled);

                eye_position_stream.IsEnabled =
                    !config.LeftEye.Equals(Vector3Percents.Disabled) ||
                    !config.RightEye.Equals(Vector3Percents.Disabled) ||
                    !config.AngleBetweenEyes.Equals(Vector3Percents.Disabled);

                Debug.Assert(config.AngleBetweenEyes.Z == null);
            }
        }

        public TobiiCoordinatesProvider(Action<TobiiCoordinates> on_coordinates_callback)
        {
            this.on_coordinates_callback = on_coordinates_callback;
            try
            {
                host = new Tobii.Interaction.Host();
                gaze_point_data_stream = host.Streams.CreateGazePointDataStream();
                eye_position_stream = host.Streams.CreateEyePositionStream();
                head_pose_stream = host.Streams.CreateHeadPoseStream();

                UpdateTobiiStreams(null, null);

                Options.CalibrationMode.Changed += UpdateTobiiStreams;

                gaze_point_data_stream.GazePoint(OnGazePoint);
                eye_position_stream.EyePosition(OnEyePosition);
                head_pose_stream.HeadPose(OnHeadPose);
            }
            catch (Exception e)
            {

                if (MessageBox.Show("Eye Tracker isn't installed properly. \n\n" +
                    "Open Tobii software download page? \n",
                    "Failed Connecting to Tobii Eye Tracker",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                    Process.Start("https://gaming.tobii.com/getstarted/");
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
            lock (Helpers.locker)
            {
                on_coordinates_callback = null;
                Options.CalibrationMode.Changed -= UpdateTobiiStreams;

                try
                {
                    host.Dispose();
                }
                catch (Tobii.Interaction.Client.InteractionApiException)
                {
                    // TODO: Fix MemoryLeak
                }
            }
        }
    }
}
