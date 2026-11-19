using System;
using System.Collections.Generic;
using System.IO;

namespace SleepTimer
{
    internal class Settings
    {
        internal static string DirOverride;

        public int Action;
        public bool Warn = true;
        public string Custom = "";
        public bool TrayHintShown;

        static string FilePath
        {
            get
            {
                string dir = DirOverride ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SleepTimer");
                return Path.Combine(dir, "ayarlar.txt");
            }
        }

        public static Settings Load()
        {
            var st = new Settings();
            try
            {
                if (!File.Exists(FilePath)) return st;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    string k = line.Substring(0, i).Trim(), v = line.Substring(i + 1).Trim();
                    int n;
                    switch (k)
                    {
                        case "eylem": if (int.TryParse(v, out n) && n >= 0 && n <= 2) st.Action = n; break;
                        case "uyari": st.Warn = v != "0"; break;
                        case "ozel": st.Custom = v; break;
                        case "ipucu": st.TrayHintShown = v == "1"; break;
                    }
                }
            }
            catch { }
            return st;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, new List<string> {
                    "eylem=" + Action,
                    "uyari=" + (Warn ? "1" : "0"),
                    "ozel=" + Custom,
                    "ipucu=" + (TrayHintShown ? "1" : "0"),
                }.ToArray());
            }
            catch { }
        }
    }
}
