using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SleepTimer
{
    // 0 = Kapat, 1 = Uyku, 2 = Hazırda beklet
    internal static class Power
    {
        // Testler bunu ayarlar; ayarlıyken hiçbir gerçek işlem yapılmaz.
        internal static Action<int> TestHook;

        public static string Name(int a)
        {
            switch (a)
            {
                case 1: return Lang.T("Uyku", "Sleep");
                case 2: return Lang.T("Hazırda beklet", "Hibernate");
                default: return Lang.T("Kapat", "Shut down");
            }
        }

        // Tepsi ipucu: "58:12 sonra kapanır" / "Shuts down in 58:12"
        public static string Line(int a, string time)
        {
            if (Lang.Tr)
            {
                switch (a)
                {
                    case 1: return time + " sonra uyku moduna geçer";
                    case 2: return time + " sonra hazırda beklemeye geçer";
                    default: return time + " sonra kapanır";
                }
            }
            switch (a)
            {
                case 1: return "Sleeps in " + time;
                case 2: return "Hibernates in " + time;
                default: return "Shuts down in " + time;
            }
        }

        public static string WarnTitle(int a)
        {
            switch (a)
            {
                case 1: return Lang.T("Uyku moduna geçiliyor", "Going to sleep");
                case 2: return Lang.T("Hazırda beklemeye geçiliyor", "Hibernating");
                default: return Lang.T("Bilgisayar kapanıyor", "Shutting down");
            }
        }

        public static void Run(int a)
        {
            if (TestHook != null) { TestHook(a); return; }
            if (Program.DryRun)
            {
                MessageBox.Show(Lang.T("Test modu: şimdi \"" + Name(a) + "\" işlemi yapılacaktı.",
                    "Test mode: \"" + Name(a) + "\" would run now."), "SleepTimer");
                return;
            }
            try
            {
                switch (a)
                {
                    case 1:
                        Application.SetSuspendState(PowerState.Suspend, false, false);
                        break;
                    case 2:
                        Application.SetSuspendState(PowerState.Hibernate, false, false);
                        break;
                    default:
                        // Başlat > Kapat ile aynı: uygulamalar normal şekilde kapanır, zorlama yok.
                        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), "/s /t 0");
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        Process.Start(psi);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("İşlem yapılamadı: ", "Couldn't do it: ") + ex.Message, "SleepTimer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
