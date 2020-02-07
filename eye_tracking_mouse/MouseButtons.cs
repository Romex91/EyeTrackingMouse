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


        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSystemMetrics(int nIndex);
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        public static bool LeftPressed { private set; get; } = false;
        public static bool RightPressed { private set; get; } = false;

        private static void MouseEvent(uint dwFlags, int cButtons)
        {
            mouse_event(dwFlags, 0, 0, cButtons, 0);
        }

        private static Task move_mouse_task = null;

        public static void Move(int x, int y)
        {
            if (move_mouse_task == null || move_mouse_task.IsCompleted)
            {
                move_mouse_task = Task.Factory.StartNew(() => {
                    mouse_event(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE, (uint)(x * (65536 / GetSystemMetrics(SM_CXSCREEN))), (uint)(y * (65536 / GetSystemMetrics(SM_CYSCREEN))), 0, 0);
                });
            }
        }

        public static void LeftDown()
        {
            LeftPressed = true;
            MouseEvent(MOUSEEVENTF_LEFTDOWN, 0);
        }

        public static void LeftUp()
        {
            LeftPressed = false;
            MouseEvent(MOUSEEVENTF_LEFTUP, 0);
        }

        public static void RightDown()
        {
            RightPressed = true;
            MouseEvent(MOUSEEVENTF_RIGHTDOWN , 0);
        }

        public static void RightUp()
        {
            RightPressed = false;
            MouseEvent(MOUSEEVENTF_RIGHTUP, 0);
        }

        public static void WheelDown(int steps)
        {
            MouseEvent(MOUSEEVENTF_WHEEL, -20 * steps);
        }
        public static void WheelUp(int steps)
        {
            MouseEvent(MOUSEEVENTF_WHEEL, 20 * steps);
        }
        public static void WheelLeft(int steps)
        {
            MouseEvent(MOUSEEVENTF_HWHEEL, -20 * steps);
        }
        public static void WheelRight(int steps)
        {
            MouseEvent(MOUSEEVENTF_HWHEEL, 20 * steps);
        }
        
    }
}
