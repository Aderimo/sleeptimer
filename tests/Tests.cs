using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using SleepTimer;

static class Tests
{
    static int fail;
    static int lastAction = -1;

    static void Check(bool ok, string name)
    {
        Console.WriteLine((ok ? "OK    " : "HATA  ") + name);
        if (!ok) fail++;
    }

    static void Parse(string input, double? minutes)
    {
        TimeSpan t;
        bool ok = Duration.TryParse(input, out t);
        if (minutes == null) Check(!ok, "'" + input + "' reddedilir");
        else Check(ok && Math.Abs(t.TotalMinutes - minutes.Value) < 0.01, "'" + input + "' = " + minutes + " dk");
    }

    static void Snap(Control c, string dir, string name)
    {
        Application.DoEvents();
        using (var b = new Bitmap(c.Width, c.Height))
        {
            c.DrawToBitmap(b, new Rectangle(0, 0, c.Width, c.Height));
            b.Save(Path.Combine(dir, name), ImageFormat.Png);
        }
    }

    [STAThread]
    static int Main(string[] args)
    {
        string dir = args.Length > 0 ? args[0] : ".";

        Parse("90", 90);
        Parse("1:30", 90);
        Parse("2s", 120);
        Parse("1,5 sa", 90);
        Parse("1.5saat", 90);
        Parse("45dk", 45);
        Parse("1s30", 90);
        Parse("1 saat 30 dakika", 90);
        Parse("2,5", 2.5);
        Parse("24s", 1440);
        Parse("0", null);
        Parse("", null);
        Parse("abc", null);
        Parse("25s", null);
        Parse("1:75", null);
        Parse("-5", null);

        Settings.DirOverride = Path.Combine(dir, "ayarlar");
        if (Directory.Exists(Settings.DirOverride)) Directory.Delete(Settings.DirOverride, true);
        Power.TestHook = a => lastAction = a;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var f = new MainForm(TimeSpan.Zero);
        f.SuppressBalloons = true;
        f.Show();
        Snap(f, dir, "1-bos.png");

        f.StartFor(TimeSpan.FromMinutes(45), 45);
        f.SetRemainingForTest(TimeSpan.FromMinutes(31) + TimeSpan.FromSeconds(12));
        f.OnTick();
        Check(f.IsRunning, "45 dk başladı");
        Snap(f, dir, "2-sayiyor.png");

        f.SetRemainingForTest(TimeSpan.FromSeconds(50));
        f.OnTick();
        Check(f.WarningShown, "son 60 sn uyarısı açıldı");
        Check(lastAction == -1, "uyarıda henüz işlem yok");
        if (f.Warning != null) Snap(f.Warning, dir, "3-uyari.png");

        f.Extend(15);
        Check(!f.WarningShown, "+15 dk uyarıyı kapattı");
        Check(Math.Abs(f.Remaining().TotalSeconds - (15 * 60 + 50)) < 3, "+15 dk süreye eklendi");

        f.SetRemainingForTest(TimeSpan.FromSeconds(-1));
        f.OnTick();
        Check(lastAction == 0 && !f.IsRunning, "süre dolunca Kapat çağrıldı");

        lastAction = -1;
        f.StartFor(TimeSpan.FromMinutes(10), 10);
        f.SetRemainingForTest(TimeSpan.FromMinutes(-5));
        f.OnTick();
        Check(lastAction == -1 && !f.IsRunning, "uykudan dönünce geç kalmışsa kapatmaz");

        f.StartFor(TimeSpan.FromMinutes(10), 10);
        f.CancelTimer();
        f.SetRemainingForTest(TimeSpan.FromSeconds(-1));
        f.OnTick();
        Check(lastAction == -1 && !f.IsRunning, "iptal sonrası işlem yok");

        f.SettingsObj.Warn = false;
        f.StartFor(TimeSpan.FromMinutes(5), 5);
        f.SetRemainingForTest(TimeSpan.FromSeconds(30));
        f.OnTick();
        Check(!f.WarningShown, "uyarı kapalıyken pencere çıkmaz");
        f.CancelTimer();
        f.SettingsObj.Warn = true;

        f.CustomText = "1:30";
        f.StartCustom();
        Check(f.IsRunning && f.Total == TimeSpan.FromMinutes(90), "özel 1:30 = 90 dk başladı");
        f.SetRemainingForTest(TimeSpan.FromMinutes(72) + TimeSpan.FromSeconds(5));
        f.OnTick();
        Snap(f, dir, "4-ozel.png");
        f.CancelTimer();

        f.CustomText = "abc";
        f.StartCustom();
        Check(!f.IsRunning && f.Error != null, "geçersiz özel süre reddedildi");
        Snap(f, dir, "5-hata.png");

        f.ExitApp();
        Application.DoEvents();

        Console.WriteLine(fail == 0 ? "Tüm testler geçti." : fail + " test başarısız.");
        return fail;
    }
}
