using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tobii.Interaction;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace move_mouse
{
    class Program
    {
        static readonly object _locker = new object();

        private const int correction_step = 10;
        private static Point correction = new Point(0, 0);
        private static Boolean data_updated = false;
        private static Point gaze_point = new Point(0, 0);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static bool lwin_pressed = false;
        private static bool correction_update_mode = false;

        private static ShiftsStorage corrections_storage = new ShiftsStorage();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        private const int KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYUP = 2;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_WHEEL = 0x800;
        private const int MOUSEEVENTF_HWHEEL = 0x1000;

        private static bool ignore_next_lwin_up = false;

        private static DateTime last_interaction = DateTime.Now;

        private static void MaskedMouseEvent(uint dwFlags, int cButtons, uint dwExtraInfo)
        {
            ignore_next_lwin_up = true;

            BreakLwin();

            keybd_event((byte)Keys.LWin, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(dwFlags, X, Y, cButtons, dwExtraInfo);
            keybd_event((byte)Keys.LWin, 0, KEYEVENTF_EXTENDEDKEY, 0);
        }

        private static void LeftMouseClick()
        {
            MaskedMouseEvent(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0);
        }

        private static void RightMouseClick()
        {
            MaskedMouseEvent(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, 0, 0);
        }
        private static void MouseWheelDown()
        {
            MaskedMouseEvent(MOUSEEVENTF_WHEEL, -600, 0);
        }
        private static void MouseWheelUp()
        {
            MaskedMouseEvent(MOUSEEVENTF_WHEEL, 600, 0);
        }
        private static void MouseWheelLeft()
        {
            MaskedMouseEvent(MOUSEEVENTF_HWHEEL, -600, 0);
        }
        private static void MouseWheelRight()
        {
            MaskedMouseEvent(MOUSEEVENTF_HWHEEL, 600, 0);
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static void BreakLwin()
        {
            if (!lwin_pressed)
            {
                return;
            }

            keybd_event((byte)Keys.LControlKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event((byte)Keys.LControlKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
        }
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int key_code = Marshal.ReadInt32(lParam);
                if ((Keys)key_code == Keys.LWin && wParam == (IntPtr)WM_KEYDOWN)
                {
                    lwin_pressed = true;
                }

                if ((Keys)key_code == Keys.LWin && wParam == (IntPtr)WM_KEYUP)
                {
                    if (!ignore_next_lwin_up)
                    {
                        correction_update_mode = false;
                        lwin_pressed = false;
                    }
                        
                    ignore_next_lwin_up = false;
                }

                if (lwin_pressed && wParam == (IntPtr)WM_KEYDOWN)
                {
                    bool is_double_click = (DateTime.Now - last_interaction).TotalMilliseconds < 300;
                    if ((Keys)key_code == Keys.J || (Keys)key_code == Keys.K || (Keys)key_code == Keys.H || (Keys)key_code == Keys.N || (Keys)key_code == Keys.Oemcomma || (Keys)key_code == Keys.OemPeriod)
                    {
                        last_interaction = DateTime.Now;
                    }

                    if ((Keys)key_code == Keys.A)
                    {
                        
                        correction_update_mode = true;
                        correction.X -= correction_step;
                        return new IntPtr(1);
                    }

                    if ((Keys)key_code == Keys.D)
                    {
                        correction_update_mode = true;
                        correction.X += correction_step;
                        return new IntPtr(1);
                    }

                    if ((Keys)key_code == Keys.W)
                    {
                        correction_update_mode = true;
                        correction.Y -= correction_step;
                        return new IntPtr(1);
                    }
                    if ((Keys)key_code == Keys.S)
                    {
                        correction_update_mode = true;
                        correction.Y += correction_step;
                        return new IntPtr(1);
                    }

                    if ((Keys)key_code == Keys.J || (Keys)key_code == Keys.K)
                    {
                        if (correction_update_mode)
                        {
                            lock (_locker)
                            {
                                corrections_storage.AddShift(gaze_point, correction);
                                last_interaction = DateTime.Now;
                                correction_update_mode = false;
                            }
                        }
                    }

                    if ((Keys)key_code == Keys.J)
                    {
                        LeftMouseClick();
                        return new IntPtr(1);
                    }

                    if ((Keys)key_code == Keys.K)
                    {
                        RightMouseClick();
                        return new IntPtr(1);
                    }

                    if ((Keys)key_code == Keys.H)
                    {
                        MouseWheelUp();
                        if (is_double_click)
                            MouseWheelUp();
                        return new IntPtr(1);
                    }
                    if ((Keys)key_code == Keys.N)
                    {
                        MouseWheelDown();
                        if (is_double_click)
                            MouseWheelDown();
                        return new IntPtr(1);
                    }
                    if ((Keys)key_code == Keys.Oemcomma)
                    {
                        MouseWheelLeft();
                        if (is_double_click)
                            MouseWheelLeft();
                        return new IntPtr(1);
                    }

                    if ((Keys)key_code == Keys.OemPeriod)
                    {
                        MouseWheelRight();
                        if (is_double_click)
                            MouseWheelRight();

                        return new IntPtr(1);
                    }
                    if ((Keys)key_code == Keys.M)
                    {
                        lock (_locker)
                        {
                            corrections_storage.ResetClosest(gaze_point);
                            if (is_double_click)
                                corrections_storage.Reset();
                        }
                        return new IntPtr(1);
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        static void Main(string[] args)
        {
            _hookID = SetHook(_proc);

            Host host = new Host();
            GazePointDataStream gazePointDataStream = host.Streams.CreateGazePointDataStream();

            gazePointDataStream.GazePoint((x, y, ts) =>
            {
                lock (_locker)
                {
                    if (lwin_pressed && (DateTime.Now - last_interaction).TotalMilliseconds > 600 || correction_update_mode)
                    {
                        gaze_point.X = (int)(x / 1);
                        gaze_point.Y = (int)(y / 1);
                        Cursor.Position = new Point(gaze_point.X + correction.X, gaze_point.Y + correction.Y);
                        data_updated = true;
                    }
                }
            });

            Application.Idle += (object sender, EventArgs e) =>
            {
                lock (_locker)
                {
                    if (data_updated && !correction_update_mode)
                    {
                        correction = corrections_storage.GetShift(gaze_point);
                        data_updated = false;
                    }
                }
            };

            Console.WriteLine("Close me to stop handling tobii hotkeys");
            Application.Run();

            UnhookWindowsHookEx(_hookID);
            host.Dispose();
        }
    }
}
