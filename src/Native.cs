using System;
using System.Runtime.InteropServices;

namespace SleepTimer
{
    internal static class Native
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegisterWindowMessage(string name);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // Windows 11: yuvarlak pencere köşeleri. Eski sürümlerde sessizce yok sayılır.
        public static void RoundCorners(IntPtr hwnd)
        {
            try
            {
                int pref = 2;
                DwmSetWindowAttribute(hwnd, 33, ref pref, 4);
            }
            catch { }
        }
    }
}
