using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace eye_tracking_mouse
{
    /// <summary>
    /// Interaction logic for YoutubeIndicationsWindow.xaml
    /// </summary>
    public partial class YoutubeIndicationsWindow : Window
    {
        private MediaPlayer click_down_player = new MediaPlayer();
        private MediaPlayer click_up_player = new MediaPlayer();
        private MediaPlayer press_down_player = new MediaPlayer();
        private MediaPlayer press_up_player = new MediaPlayer();

        Petzold.Media2D.ArrowLine arrow_left = new Petzold.Media2D.ArrowLine { Stroke = Brushes.Red, StrokeThickness = 7, Visibility = Visibility.Hidden };
        Petzold.Media2D.ArrowLine arrow_right = new Petzold.Media2D.ArrowLine { Stroke = Brushes.Red, StrokeThickness = 7, Visibility = Visibility.Hidden };
        Petzold.Media2D.ArrowLine arrow_up = new Petzold.Media2D.ArrowLine { Stroke = Brushes.Red, StrokeThickness = 7, Visibility = Visibility.Hidden };
        Petzold.Media2D.ArrowLine arrow_down = new Petzold.Media2D.ArrowLine { Stroke = Brushes.Red, StrokeThickness = 7, Visibility = Visibility.Hidden};

        public YoutubeIndicationsWindow()
        {
            InitializeComponent();
            press_down_player.Open(new Uri("youtube_media_files/press_down.wav", UriKind.Relative));
            press_up_player.Open(new Uri("youtube_media_files/press_up.wav", UriKind.Relative));
            click_up_player.Open(new Uri("youtube_media_files/click_up.wav", UriKind.Relative));
            click_down_player.Open(new Uri("youtube_media_files/click_down.wav", UriKind.Relative));

            CompositionTarget.Rendering += OnRendering;

            Canvas.Children.Add(arrow_left);
            Canvas.Children.Add(arrow_right);
            Canvas.Children.Add(arrow_up);
            Canvas.Children.Add(arrow_down);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            var pos = System.Windows.Forms.Cursor.Position;
            pos.X = (int)(pos.X / 1.75);
            pos.Y = (int)(pos.Y / 1.75);


            Canvas.SetLeft(ImgMouse, pos.X + 30);
            Canvas.SetTop(ImgMouse, pos.Y + 50);
            Canvas.SetLeft(TxtPressedButtons, pos.X + 35);
            Canvas.SetTop(TxtPressedButtons, pos.Y);
            

            arrow_left.X1 = pos.X - 15;
            arrow_left.Y1 = pos.Y;
            arrow_left.X2 = pos.X - 45;
            arrow_left.Y2 = pos.Y;

            arrow_right.X1 = pos.X + 15;
            arrow_right.Y1 = pos.Y;
            arrow_right.X2 = pos.X + 45;
            arrow_right.Y2 = pos.Y;

            arrow_up.X1 = pos.X;
            arrow_up.Y1 = pos.Y - 15;
            arrow_up.X2 = pos.X;
            arrow_up.Y2 = pos.Y - 45;

            arrow_down.X1 = pos.X;
            arrow_down.Y1 = pos.Y + 15;
            arrow_down.X2 = pos.X;
            arrow_down.Y2 = pos.Y + 45;

        }

        public void OnKeyPressed(
            Key key,
            KeyState key_state,
            bool is_repeatition)
        {
            if (key == Key.Unbound || is_repeatition)
                return;
            if (key == Key.LeftMouseButton || key == Key.RightMouseButton)
            {
                if (key_state == KeyState.Down)
                {
                    click_down_player.Position = new TimeSpan();
                    click_down_player.Play();
                }
                else
                {
                    click_up_player.Position = new TimeSpan();
                    click_up_player.Play();
                }
            } else
            {
                if (key_state == KeyState.Down)
                {
                    press_down_player.Position = new TimeSpan();
                    press_down_player.Play();
                }
                else if (key == Key.Modifier)
                {
                    press_up_player.Position = new TimeSpan();
                    press_up_player.Play();
                }
            }

            if (key == Key.Modifier)
            {
                ImgWinBtn.Brush = key_state == KeyState.Down ? Brushes.Red : Brushes.LightGray;
            }

            if (key == Key.LeftMouseButton ||
                key == Key.RightMouseButton ||
                key == Key.ScrollDown ||
                key == Key.ScrollUp ||
                key == Key.ScrollLeft ||
                key == Key.ScrollRight)
            {
                ImgMouse.Visibility = key_state == KeyState.Down ? Visibility.Visible : Visibility.Hidden;
                if (key == Key.LeftMouseButton)
                {
                    ImgLMB.Brush = key_state == KeyState.Down ? Brushes.Red : Brushes.LightGray;
                }

                if (key == Key.RightMouseButton)
                {
                    ImgRMB.Brush = key_state == KeyState.Down ? Brushes.Red : Brushes.LightGray;
                }

                if (key == Key.ScrollDown ||
                    key == Key.ScrollUp ||
                    key == Key.ScrollLeft ||
                    key == Key.ScrollRight)
                {
                    ImgScroll.Brush = key_state == KeyState.Down ? Brushes.Red : Brushes.LightGray;
                }
            }

            Dictionary<Key, string> key_to_letter_dictionary = new Dictionary<Key, string> {
                { Key.LeftMouseButton, "J" },
                { Key.RightMouseButton, "K" },
                { Key.ScrollDown, "N" },
                { Key.ScrollUp, "H" },
                { Key.ScrollLeft, "<" },
                { Key.ScrollRight, ">" },
                { Key.CalibrateDown, "S" },
                { Key.CalibrateUp, "W" },
                { Key.CalibrateLeft, "A" },
                { Key.CalibrateRight, "D" },
            };

            if (key_to_letter_dictionary.ContainsKey(key))
            {
                if (key_state == KeyState.Down)
                    TxtPressedButtons.Text = key_to_letter_dictionary[key];
                else
                    TxtPressedButtons.Text = "";
            }

            if (key == Key.CalibrateDown)
                arrow_down.Visibility = key_state == KeyState.Down ? Visibility.Visible : Visibility.Hidden;
            if (key == Key.CalibrateUp)
                arrow_up.Visibility = key_state == KeyState.Down ? Visibility.Visible : Visibility.Hidden;
            if (key == Key.CalibrateLeft)
                arrow_left.Visibility = key_state == KeyState.Down ? Visibility.Visible : Visibility.Hidden;
            if (key == Key.CalibrateRight)
                arrow_right.Visibility = key_state == KeyState.Down ? Visibility.Visible : Visibility.Hidden;
        }


        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }
    }
}
