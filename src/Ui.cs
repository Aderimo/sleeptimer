using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace SleepTimer
{
    internal static class Ui
    {
        public static readonly Color Bg = Color.FromArgb(32, 33, 36);
        public static readonly Color Chip = Color.FromArgb(58, 59, 63);
        public static readonly Color ChipHover = Color.FromArgb(74, 75, 80);
        public static readonly Color Accent = Color.FromArgb(239, 159, 39);
        public static readonly Color AccentDark = Color.FromArgb(133, 79, 11);
        public static readonly Color AccentDarkHover = Color.FromArgb(156, 94, 16);
        public static readonly Color Fg = Color.FromArgb(232, 232, 234);
        public static readonly Color Muted = Color.FromArgb(154, 156, 163);
        public static readonly Color Danger = Color.FromArgb(226, 75, 74);
        public static readonly Color DangerDark = Color.FromArgb(163, 45, 45);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            int d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Fill(Graphics g, Color c, Rectangle r, int rad)
        {
            using (GraphicsPath p = Round(r, rad))
            using (var b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static void Stroke(Graphics g, Color c, Rectangle r, int rad, float width)
        {
            var rr = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
            using (GraphicsPath p = Round(rr, rad))
            using (var pen = new Pen(c, width))
                g.DrawPath(pen, p);
        }

        public static void Text(Graphics g, string t, Font f, Color c, Rectangle r, TextFormatFlags extra)
        {
            TextRenderer.DrawText(g, t, f, r, c, TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | extra);
        }

        public static string Fmt(TimeSpan t)
        {
            int sec = (int)Math.Ceiling(t.TotalSeconds);
            if (sec < 0) sec = 0;
            int h = sec / 3600, m = (sec / 60) % 60, x = sec % 60;
            return h > 0 ? string.Format("{0}:{1:00}:{2:00}", h, m, x) : string.Format("{0:00}:{1:00}", m, x);
        }

        public static float Scale(Control c)
        {
            using (Graphics g = c.CreateGraphics()) return g.DpiX / 96f;
        }

        public static Icon LoadIcon(Size size)
        {
            try
            {
                using (Stream st = typeof(Ui).Assembly.GetManifestResourceStream("SleepTimer.icon.ico"))
                    if (st != null) return new Icon(st, size);
            }
            catch { }
            return SystemIcons.Application;
        }
    }

    internal class DarkColors : ProfessionalColorTable
    {
        public DarkColors() { UseSystemColors = false; }
        public override Color ToolStripDropDownBackground { get { return Ui.Bg; } }
        public override Color MenuBorder { get { return Ui.Chip; } }
        public override Color MenuItemBorder { get { return Ui.Chip; } }
        public override Color MenuItemSelected { get { return Ui.Chip; } }
        public override Color MenuItemSelectedGradientBegin { get { return Ui.Chip; } }
        public override Color MenuItemSelectedGradientEnd { get { return Ui.Chip; } }
        public override Color ImageMarginGradientBegin { get { return Ui.Bg; } }
        public override Color ImageMarginGradientMiddle { get { return Ui.Bg; } }
        public override Color ImageMarginGradientEnd { get { return Ui.Bg; } }
        public override Color SeparatorDark { get { return Ui.Chip; } }
        public override Color SeparatorLight { get { return Ui.Bg; } }
    }
}
