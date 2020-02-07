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
    public class EyeTrackingMouse : IDisposable
    {
        private readonly Tobii.Interaction.Host host;
        private readonly Tobii.Interaction.GazePointDataStream gaze_point_data_stream;

        private readonly Tobii.Interaction.EyePositionStream eye_position_stream;
        private readonly Tobii.Interaction.HeadPoseStream head_pose_stream;

        private Point gaze_point = new Point(0, 0);
        private readonly GazeSmoother gaze_smoother = new GazeSmoother();

        private Tobii.Interaction.Vector3 left_eye;
        private Tobii.Interaction.Vector3 right_eye;
        private Tobii.Interaction.Vector3 head_position;
        private Tobii.Interaction.Vector3 head_direction;

        // |gaze_point| is not accurate. To enable precise cursor control the application supports calibration by W/A/S/D.
        // |calibration_shift| is result of such calibration. Application sets cursor position to |gaze_point| + |calibration_shift| when in |Controlling| state.
        private Point calibration_shift = new Point(0, 0);

        // A point uzer looked at when he started calibration. 
        // If user's gaze leaves area around this point the calibration will be over.
        private Point calibration_start_gaze_point = new Point(0, 0);

        // Updating |calibration_shift| may be expensive. These variables tracks whether update is required.
        private DateTime last_shift_update_time = DateTime.Now;

        private DateTime freeze_until = DateTime.Now;

        private DateTime last_gaze_point = DateTime.Now;

        private Statistics statistics = Statistics.LoadFromFile();

        public enum MouseState
        {
            // Application does nothing. 
            Idle,
            // Application moves cursor to gaze point applying previous calibrations. To enable this state user should press modifier key.
            Controlling,
            // User can press W/A/S/D while holding modifier to calibrate cursor position to fit user's gaze more accurately. 
            Calibrating
        };

        public MouseState mouse_state { private set; get; } = MouseState.Idle;

        private void UpdateCursorPosition()
        {
            MouseButtons.Move(gaze_point.X + calibration_shift.X, gaze_point.Y + calibration_shift.Y);
        }

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
            }
        }

        private void OnGazePoint(double x, double y, double ts)
        {
            lock (Helpers.locker)
            {
                last_gaze_point = DateTime.Now;
                if (mouse_state == MouseState.Controlling || mouse_state == MouseState.Calibrating)
                {
                    if (DateTime.Now > freeze_until)
                    {
                        gaze_smoother.AddGazePoint(new Point((int)(x), (int)(y)));
                        gaze_point = gaze_smoother.GetSmoothenedGazePoint();

                        if (mouse_state == MouseState.Calibrating && Helpers.GetDistance(gaze_point, calibration_start_gaze_point) > Options.Instance.reset_calibration_zone_size)
                        {
                            mouse_state = MouseState.Controlling;
                        }

                        if (mouse_state == MouseState.Controlling &&
                            (DateTime.Now - last_shift_update_time).TotalMilliseconds > Options.Instance.calibration_mode.update_period_ms)
                        {
                            last_shift_update_time = DateTime.Now;
                            calibration_shift = ShiftsStorage.Instance.GetShift(new ShiftsStorage.Position(
                                gaze_point.X, gaze_point.Y, left_eye, right_eye, head_position, head_direction));
                        }
                    }

                    UpdateCursorPosition();
                }
            }
        }

        public void StopControlling()
        {
            freeze_until = DateTime.Now;
            mouse_state = MouseState.Idle;

            if (MouseButtons.RightPressed)
                MouseButtons.RightUp();
            if (MouseButtons.LeftPressed)
                MouseButtons.LeftUp();
        }

        private void StartControlling()
        {
            lock (Helpers.locker) {
                mouse_state = MouseState.Controlling;
                // TODO: remove this when issue resolved:
                // https://developer.tobii.com/community/forums/topic/bug-tobii-interaction-gazepointdatastream-gazepoint-sometimes-has-no-effect/
                if ((DateTime.Now - last_gaze_point).TotalSeconds > 1)
                {
                    gaze_point_data_stream.IsEnabled = false;
                    gaze_point_data_stream.IsEnabled = true;
                }
            }
        }

        private void StartCalibration()
        {
            if (mouse_state != MouseState.Calibrating)
            {
                calibration_start_gaze_point = gaze_point;
                mouse_state = MouseState.Calibrating;
            }
        }

        public bool OnKeyPressed(
            Key key,
            KeyState key_state,
            double speed_up,
            bool is_short_modifier_press,
            bool is_repetition,
            bool is_modifier,
            InputProvider input_provider)
        {
            // The application grabs control over cursor when modifier is pressed.
            if (key == Key.Modifier)
            {
                if (key_state == KeyState.Down)
                {
                    StartControlling();
                    return true;
                }

                if (key_state == KeyState.Up)
                {
                    if (mouse_state == EyeTrackingMouse.MouseState.Idle)
                    {
                        return false;
                    }

                    bool handled = true;
                    if (is_short_modifier_press)
                    {
                        input_provider.SendModifierDown();
                        input_provider.SendModifierUp();
                        handled = true;
                    }
                    StopControlling();
                    return handled;
                }
            }

            if (mouse_state == EyeTrackingMouse.MouseState.Idle)
            {
                return false;
            }

            if (key == Key.Unbound)
            {
                // The application intercepts modifier key presses. We do not want to lose modifier when handling unbound keys.
                // We stop controlling cursor when facing the first unbound key and send modifier keystroke to OS before handling pressed key.
                if (!is_modifier)
                {
                    input_provider.SendModifierDown();
                    StopControlling();
                }
                return false;
            }

            var repetition_white_list = new SortedSet<Key> {
                    Key.ScrollDown,
                    Key.ScrollUp,
                    Key.ScrollLeft,
                    Key.ScrollRight,
                    Key.CalibrateLeft,
                    Key.CalibrateRight,
                    Key.CalibrateUp,
                    Key.CalibrateDown,
                };

            if (is_repetition && !repetition_white_list.Contains(key))
                return true;

            if (key_state == KeyState.Down)
            {
                // Calibration
                int calibration_step = (int)(Options.Instance.calibration_step * speed_up);
                if (key == Key.CalibrateLeft)
                {
                    StartCalibration();
                    calibration_shift.X -= calibration_step;
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.calibrate_freeze_time_ms);
                    statistics.OnCalibrate();
                }
                if (key == Key.CalibrateRight)
                {
                    StartCalibration();
                    calibration_shift.X += calibration_step;
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.calibrate_freeze_time_ms);
                    statistics.OnCalibrate();
                }
                if (key == Key.CalibrateUp)
                {
                    StartCalibration();
                    calibration_shift.Y -= calibration_step;
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.calibrate_freeze_time_ms);
                    statistics.OnCalibrate();
                }
                if (key == Key.CalibrateDown)
                {
                    StartCalibration();
                    calibration_shift.Y += calibration_step;
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.calibrate_freeze_time_ms);
                    statistics.OnCalibrate();
                }

                // Scroll
                if (key == Key.ScrollDown)
                {
                    MouseButtons.WheelDown((int)(Options.Instance.vertical_scroll_step * speed_up));
                }
                if (key == Key.ScrollUp)
                {
                    MouseButtons.WheelUp((int)(Options.Instance.vertical_scroll_step * speed_up));
                }
                if (key == Key.ScrollLeft)
                {
                    MouseButtons.WheelLeft((int)(Options.Instance.horizontal_scroll_step * speed_up));
                }
                if (key == Key.ScrollRight)
                {
                    MouseButtons.WheelRight((int)(Options.Instance.horizontal_scroll_step * speed_up));
                }
            }

            // Mouse buttons
            if (mouse_state == MouseState.Calibrating &&
                (key == Key.LeftMouseButton || key == Key.RightMouseButton))
            {
                ShiftsStorage.Instance.AddShift(
                    new ShiftsStorage.Position(gaze_point.X, gaze_point.Y, left_eye, right_eye, head_position, head_direction),
                    calibration_shift);
                mouse_state = MouseState.Controlling;
            }

            if (key == Key.LeftMouseButton)
            {
                if (key_state == KeyState.Down)
                {
                    // Freeze cursor for a short period of time after mouse clicks to make double clicks esier.
                    MouseButtons.LeftDown();
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.click_freeze_time_ms);
                    statistics.OnClick();
                }
                else if (key_state == KeyState.Up)
                {
                    MouseButtons.LeftUp();
                }
            }

            if (key == Key.RightMouseButton)
            {
                if (key_state == KeyState.Down)
                {
                    // Freeze cursor for a short period of time after mouse clicks to make double clicks esier.
                    MouseButtons.RightDown();
                    freeze_until = DateTime.Now.AddMilliseconds(Options.Instance.click_freeze_time_ms);
                    statistics.OnClick();
                }
                else if (key_state == KeyState.Up)
                {
                    MouseButtons.RightUp();
                }
            }

            if (key == Key.ShowCalibrationView && key_state == KeyState.Down)
            {
                App.ToggleCalibrationWindow();
            }

            return true;
        }

        public void UpdateTobiiStreams(object sender, EventArgs e)
        {
            MultidimensionCalibrationType type = Options.Instance.calibration_mode.multidimension_calibration_type;
            head_pose_stream.IsEnabled = (type & MultidimensionCalibrationType.HeadPosition) != MultidimensionCalibrationType.None ||
                (type & MultidimensionCalibrationType.HeadDirection) != MultidimensionCalibrationType.None;

            eye_position_stream.IsEnabled = (type & MultidimensionCalibrationType.LeftEye) != MultidimensionCalibrationType.None ||
                (type & MultidimensionCalibrationType.RightEye) != MultidimensionCalibrationType.None;
        }

        public EyeTrackingMouse()
        {
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

        public void Dispose()
        {
            host.Dispose();
        }
    }
}
