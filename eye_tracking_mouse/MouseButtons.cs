using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eye_tracking_mouse
{
    class MouseButtons
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_WHEEL = 0x800;
        private const int MOUSEEVENTF_HWHEEL = 0x1000;

        private static void MouseEvent(uint dwFlags, int cButtons)
        {
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(dwFlags, X, Y, cButtons, 0);
        }

        public static void LeftDown()
        {
            MouseEvent(MOUSEEVENTF_LEFTDOWN, 0);
        }

        public static void LeftUp()
        {
            MouseEvent(MOUSEEVENTF_LEFTUP, 0);
        }

        public static void RightDown()
        {
            MouseEvent(MOUSEEVENTF_RIGHTDOWN , 0);
        }

        public static void RightUp()
        {
            MouseEvent(MOUSEEVENTF_RIGHTUP, 0);
        }

        public static void WheelDown(int steps)
        {
            MouseEvent(MOUSEEVENTF_WHEEL, -100 * steps);
        }
        public static void WheelUp(int steps)
        {
            MouseEvent(MOUSEEVENTF_WHEEL, 100 * steps);
        }
        public static void WheelLeft(int steps)
        {
            MouseEvent(MOUSEEVENTF_HWHEEL, -100 * steps);
        }
        public static void WheelRight(int steps)
        {
            MouseEvent(MOUSEEVENTF_HWHEEL, 100 * steps);
        }
        
    }
}
