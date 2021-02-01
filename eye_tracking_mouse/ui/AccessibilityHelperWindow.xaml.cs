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
    public partial class AccessibilityHelperWindow : Window
    {
        public AccessibilityHelperWindow()
        {
            InitializeComponent();

            CompositionTarget.Rendering += OnRendering;

            KeyBindings.Changed += OnKeyBindignsChanged;
            OnKeyBindignsChanged(null, null);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            var pos = System.Windows.Forms.Cursor.Position;

            Canvas.SetLeft(Instructions, pos.X);
            Canvas.SetTop(Instructions, pos.Y);

        }


        private void OnKeyBindignsChanged(object sender, EventArgs e)
        {
            lock (Helpers.locker)
            {
                Dictionary<Key, Run> key_to_letter_dictionary = new Dictionary<Key, Run> {
                    { Key.LeftMouseButton, TxtLeftMouseButton },
                    { Key.RightMouseButton, TxtRightMouseButton },
                    { Key.ScrollDown, TxtScrollDown },
                    { Key.ScrollUp, TxtScrollUp},
                    { Key.ScrollLeft, TxtScrollLeft},
                    { Key.ScrollRight, TxtScrollRight},
                    { Key.CalibrateDown, TxtCalibrateDown},
                    { Key.CalibrateUp, TxtCalibrateUp },
                    { Key.CalibrateRight, TxtCalibrateRight},
                    { Key.CalibrateLeft , TxtCalibrateLeft},
                    { Key.Modifier, TxtModifier},
                };

                var bindings = Options.Instance.key_bindings.interception_method == KeyBindings.InterceptionMethod.OblitaDriver ?
                    Options.Instance.key_bindings.bindings : KeyBindings.default_bindings;
                foreach ( var item in key_to_letter_dictionary)
                {
                    item.Value.Text = bindings[item.Key].ToString();
                    if (item.Value.Text == Interceptor.Keys.CommaLeftArrow.ToString())
                        item.Value.Text = "<";
                    if (item.Value.Text == Interceptor.Keys.PeriodRightArrow.ToString())
                        item.Value.Text = ">";
                    if (item.Value.Text == Interceptor.Keys.CapsLock.ToString())
                        item.Value.Text = "CapsLk";
                    if (item.Value.Text == Interceptor.Keys.WindowsKey.ToString())
                        item.Value.Text = "Win";

                }
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }
    }
}
