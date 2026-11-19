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
                case 1: return "Uyku";
                case 2: return "Hazırda beklet";
                default: return "Kapat";
            }
        }

        public static string Verb(int a)
        {
            switch (a)
            {
                case 1: return "uyku moduna geçer";
                case 2: return "hazırda beklemeye geçer";
                default: return "kapanır";
            }
        }

        public static string WarnTitle(int a)
        {
            switch (a)
            {
                case 1: return "Uyku moduna geçiliyor";
                case 2: return "Hazırda beklemeye geçiliyor";
                default: return "Bilgisayar kapanıyor";
            }
        }

        public static void Run(int a)
        {
            if (TestHook != null) { TestHook(a); return; }
            if (Program.DryRun)
            {
                MessageBox.Show("Test modu: şimdi \"" + Name(a) + "\" işlemi yapılacaktı.", "SleepTimer");
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
                MessageBox.Show("İşlem yapılamadı: " + ex.Message, "SleepTimer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
