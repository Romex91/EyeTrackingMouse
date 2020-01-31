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

        }

        private void OnRendering(object sender, EventArgs e)
        {
            var pos = System.Windows.Forms.Cursor.Position;

            Canvas.SetLeft(Instructions, pos.X);
            Canvas.SetTop(Instructions, pos.Y);

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
        }


        private void Window_Deactivated(object sender, EventArgs e)
        {
            Topmost = true;
        }
    }
}
