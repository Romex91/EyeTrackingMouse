using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace eye_tracking_mouse
{
    class WinApiInputProvider : InputProvider
    {
        private IntPtr win_api_hook_id = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc win_api_callback;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private bool ignore_next_key_press = false;

        private IntPtr OnKeyPressed(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (ignore_next_key_press)
            {
                ignore_next_key_press = false;
                return CallNextHookEx(win_api_hook_id, nCode, wParam, lParam);
            }
            if (nCode >= 0)
            {
                Dictionary<System.Windows.Forms.Keys, Key> bindings = new Dictionary<System.Windows.Forms.Keys, Key>
                {
                    {System.Windows.Forms.Keys.LWin, Key.Modifier},
                    {System.Windows.Forms.Keys.J, Key.LeftMouseButton},
                    {System.Windows.Forms.Keys.K, Key.RightMouseButton},
                    {System.Windows.Forms.Keys.N, Key.ScrollDown},
                    {System.Windows.Forms.Keys.H,Key.ScrollUp},
                    {System.Windows.Forms.Keys.Oemcomma,Key.ScrollLeft},
                    {System.Windows.Forms.Keys.OemPeriod, Key.ScrollRight},
                    {System.Windows.Forms.Keys.A, Key.CalibrateLeft},
                    {System.Windows.Forms.Keys.D,Key.CalibrateRight},
                    {System.Windows.Forms.Keys.W, Key.CalibrateUp},
                    {System.Windows.Forms.Keys.S, Key.CalibrateDown},
                    {System.Windows.Forms.Keys.Escape, Key.StopCalibration},
                    {System.Windows.Forms.Keys.Space, Key.Accessibility_SaveCalibration},
                };

                System.Windows.Forms.Keys key_code = (System.Windows.Forms.Keys)Marshal.ReadInt32(lParam);
                Key key = Key.Unbound;
                if (bindings.ContainsKey(key_code))
                    key = bindings[key_code];
                KeyState key_state;

                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    key_state = KeyState.Down;
                }
                else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                {
                    key_state = KeyState.Up;
                }
                else
                {
                    return CallNextHookEx(win_api_hook_id, nCode, wParam, lParam);
                }

                if (receiver.OnKeyPressed(key, key_state, Helpers.IsModifier(key_code), this))
                    return new IntPtr(1);
            }

            return CallNextHookEx(win_api_hook_id, nCode, wParam, lParam);
        }


        public WinApiInputProvider(IInputReceiver receiver) : base(receiver) { }
        public override bool IsLoaded
        {
            get
            {
                return win_api_hook_id != IntPtr.Zero;
            }
        }

        public override void Load()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                win_api_callback = OnKeyPressed;
                win_api_hook_id = SetWindowsHookEx(WH_KEYBOARD_LL, win_api_callback,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public override void Unload()
        {
            if (win_api_hook_id != IntPtr.Zero)
            {
                UnhookWindowsHookEx(win_api_hook_id);
                win_api_hook_id = IntPtr.Zero;
            }
        }

        public override void SendModifierDown()
        {
            ignore_next_key_press = true;
            keybd_event((byte)System.Windows.Forms.Keys.LWin, 0, 1, 0);
        }

        public override void SendModifierUp()
        {
            ignore_next_key_press = true;
            keybd_event((byte)System.Windows.Forms.Keys.LWin, 0, 2 | 1, 0);
        }

        public override void ReadKey(Action<ReadKeyResult> callback)
        {
            throw new NotImplementedException("WinApi doesn't support proper custom key bindings.");
        }
    }
}
