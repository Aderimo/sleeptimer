using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SleepTimer
{
    // Bitime 1 dakika kala sağ altta çıkan, odak çalmayan uyarı.
    internal class WarningForm : Form
    {
        public event EventHandler ExtendClicked;
        public event EventHandler CancelClicked;

        readonly float s;
        readonly int action;
        readonly Font fTitle, fBig, fBtn, fSmall;
        TimeSpan rem;
        string hover, pressed;
        readonly Dictionary<string, Rectangle> hits = new Dictionary<string, Rectangle>();

        public WarningForm(float scale, int action)
        {
            s = scale;
            this.action = action;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Ui.Bg;
            Text = "SleepTimer";
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            fTitle = new Font("Segoe UI Semibold", 10f);
            fBig = new Font("Segoe UI Semibold", 22f);
            fBtn = new Font("Segoe UI", 10f);
            fSmall = new Font("Segoe UI", 9f);
            ClientSize = new Size(S(320), S(124));
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - S(16), wa.Bottom - Height - S(16));
        }

        int S(float v) { return (int)Math.Round(v * s); }
        Rectangle R(float x, float y, float w, float h) { return new Rectangle(S(x), S(y), S(w), S(h)); }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000 | 0x80 | 0x8; // NOACTIVATE | TOOLWINDOW | TOPMOST
                cp.ClassStyle |= 0x20000;              // gölge
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.RoundCorners(Handle);
        }

        public void SetRemaining(TimeSpan r)
        {
            bool changed = Ui.Fmt(r) != Ui.Fmt(rem);
            rem = r;
            if (changed) Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Ui.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            hits.Clear();

            using (var pen = new Pen(Ui.AccentDark, S(1)))
                g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);

            Ui.Text(g, Power.WarnTitle(action), fTitle, Ui.Fg, R(16, 8, 288, 24), TextFormatFlags.Left);

            int secs = Math.Max(0, (int)Math.Ceiling(rem.TotalSeconds));
            string big = secs.ToString();
            Rectangle bigR = R(16, 32, 120, 44);
            Ui.Text(g, big, fBig, Ui.Accent, bigR, TextFormatFlags.Left);
            int w = TextRenderer.MeasureText(g, big, fBig, Size.Empty, TextFormatFlags.NoPadding).Width;
            Ui.Text(g, Lang.T("saniye kaldı", "seconds left"), fSmall, Ui.Muted,
                new Rectangle(bigR.X + w + S(8), bigR.Y + S(6), S(200), bigR.Height), TextFormatFlags.Left);

            Btn(g, "extend", Lang.T("+15 dk ertele", "Snooze 15 min"), R(16, 82, 140, 30), false);
            Btn(g, "cancel", Lang.T("İptal", "Cancel"), R(164, 82, 140, 30), true);
        }

        void Btn(Graphics g, string id, string text, Rectangle r, bool danger)
        {
            Color c = hover == id ? (danger ? Ui.DangerDark : Ui.ChipHover) : Ui.Chip;
            Ui.Fill(g, c, r, S(6));
            Ui.Text(g, text, fBtn, Ui.Fg, r, TextFormatFlags.HorizontalCenter);
            hits[id] = r;
        }

        string HitAt(Point p)
        {
            foreach (KeyValuePair<string, Rectangle> kv in hits)
                if (kv.Value.Contains(p)) return kv.Key;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            string h = HitAt(e.Location);
            if (h != hover) { hover = h; Cursor = h != null ? Cursors.Hand : Cursors.Default; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hover != null) { hover = null; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = e.Button == MouseButtons.Left ? HitAt(e.Location) : null;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            string h = HitAt(e.Location);
            string p = pressed;
            pressed = null;
            if (h == null || h != p) return;
            EventHandler ev = h == "extend" ? ExtendClicked : CancelClicked;
            if (ev != null) ev(this, EventArgs.Empty);
        }
    }
}
