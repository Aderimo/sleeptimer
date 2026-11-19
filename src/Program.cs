using System;
using System.Threading;
using System.Windows.Forms;

namespace SleepTimer
{
    internal static class Program
    {
        internal static bool DryRun;
        internal static int ShowMessage;

        [STAThread]
        static void Main(string[] args)
        {
            ShowMessage = Native.RegisterWindowMessage("SleepTimer.Goster.7f3a91");
            bool created;
            using (var mutex = new Mutex(true, "SleepTimer.TekKopya.7f3a91", out created))
            {
                if (!created)
                {
                    // Zaten açık: var olan pencereyi öne getir.
                    Native.AllowSetForegroundWindow(-1);
                    Native.PostMessage((IntPtr)0xffff, ShowMessage, IntPtr.Zero, IntPtr.Zero);
                    return;
                }

                TimeSpan start = TimeSpan.Zero;
                foreach (string a in args)
                {
                    if (a == "--test" || a == "--dry") DryRun = true;
                    else
                    {
                        TimeSpan t;
                        if (Duration.TryParse(a, out t)) start = t;
                    }
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(start));
                GC.KeepAlive(mutex);
            }
        }
    }
}
