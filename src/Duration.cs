using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SleepTimer
{
    // Elle yazılan süreyi çözer: "90", "1:30", "2s", "1,5 sa", "45dk", "1s30", "1 saat 30 dk".
    internal static class Duration
    {
        public static readonly TimeSpan Min = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan Max = TimeSpan.FromHours(24);

        static readonly Regex Pattern = new Regex(
            @"^(?:(?<h>\d+(?:[.,]\d+)?)(?:saat|sa|s|h))?(?:(?<m>\d+(?:[.,]\d+)?)(?:dakika|dk|d|min|m)?)?$",
            RegexOptions.CultureInvariant);

        public static bool TryParse(string text, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (text == null) return false;
            string s = text.Trim().ToLower(new CultureInfo("tr-TR")).Replace(" ", "");
            if (s.Length == 0) return false;

            double minutes;
            if (s.Contains(":"))
            {
                string[] p = s.Split(':');
                int h, m;
                if (p.Length != 2
                    || !int.TryParse(p[0], NumberStyles.None, CultureInfo.InvariantCulture, out h)
                    || !int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out m)
                    || m >= 60)
                    return false;
                minutes = h * 60 + m;
            }
            else
            {
                Match match = Pattern.Match(s);
                if (!match.Success) return false;
                Group hg = match.Groups["h"], mg = match.Groups["m"];
                if (!hg.Success && !mg.Success) return false;
                minutes = 0;
                if (hg.Success) minutes += Number(hg.Value) * 60;
                if (mg.Success) minutes += Number(mg.Value);
            }

            TimeSpan t = TimeSpan.FromSeconds(Math.Round(minutes * 60));
            if (t < Min || t > Max) return false;
            result = t;
            return true;
        }

        static double Number(string v)
        {
            return double.Parse(v.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }
    }
}
