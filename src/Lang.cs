using System.Globalization;

namespace SleepTimer
{
    // Windows dili Türkçeyse Türkçe, değilse İngilizce.
    internal static class Lang
    {
        internal static bool? Override;

        public static bool Tr
        {
            get { return Override ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "tr"; }
        }

        public static string T(string tr, string en) { return Tr ? tr : en; }
    }
}
