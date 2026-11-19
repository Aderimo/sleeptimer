using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SleepTimer
{
    internal class MainForm : Form
    {
        static readonly int[] Presets = { 5, 10, 15, 20, 30, 45, 60 };
        const int LW = 400, LH = 166; // mantıksal boyut (96 DPI)

        readonly float s;
        readonly Settings settings;
        readonly System.Windows.Forms.Timer tick;
        readonly NotifyIcon tray;
        readonly ContextMenuStrip trayMenu;
        readonly TextBox custom;
        readonly Font fSmall, fChip, fBig, fHint;
        WarningForm warning;

        bool running, warned, exiting;
        DateTime endUtc;
        TimeSpan total;
        int activePreset = -1;
        string lastText;

        readonly List<KeyValuePair<string, Rectangle>> hits = new List<KeyValuePair<string, Rectangle>>();
        string hover, pressed, error;

        internal bool SuppressBalloons;

        public MainForm(TimeSpan startWith)
        {
            settings = Settings.Load();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Text = "SleepTimer";
            BackColor = Ui.Bg;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            s = Ui.Scale(this);

            fSmall = new Font("Segoe UI", 9f);
            fChip = new Font("Segoe UI", 10f);
            fBig = new Font("Segoe UI Semibold", 19f);
            fHint = new Font("Segoe UI", 12f);
            Font = fSmall;
            Icon = Ui.LoadIcon(SystemInformation.IconSize);

            ClientSize = new Size(S(LW), S(LH));
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - S(16), wa.Bottom - Height - S(16));

            custom = new TextBox();
            custom.BorderStyle = BorderStyle.None;
            custom.BackColor = Ui.Chip;
            custom.ForeColor = Ui.Fg;
            custom.Font = fChip;
            custom.TextAlign = HorizontalAlignment.Center;
            custom.MaxLength = 14;
            custom.Text = settings.Custom;
            custom.KeyDown += Custom_KeyDown;
            custom.TextChanged += delegate { if (error != null) { error = null; Invalidate(); } };
            custom.GotFocus += delegate { Invalidate(); };
            custom.LostFocus += delegate { Invalidate(); };
            custom.HandleCreated += delegate { Native.SendMessage(custom.Handle, 0x1501, (IntPtr)1, Lang.T("özel", "custom")); };
            Controls.Add(custom);
            Rectangle cr = CustomRect();
            int th = custom.PreferredHeight;
            custom.Bounds = new Rectangle(cr.X + S(4), cr.Y + (cr.Height - th) / 2, cr.Width - S(8), th);

            tick = new System.Windows.Forms.Timer();
            tick.Interval = 250;
            tick.Tick += delegate { OnTick(); };

            trayMenu = new ContextMenuStrip();
            trayMenu.Renderer = new ToolStripProfessionalRenderer(new DarkColors());
            trayMenu.ShowImageMargin = false;
            trayMenu.BackColor = Ui.Bg;
            trayMenu.ForeColor = Ui.Fg;
            trayMenu.Font = fChip;
            trayMenu.Opening += (o, e) => { BuildTrayMenu(); e.Cancel = false; };

            tray = new NotifyIcon();
            tray.Icon = Ui.LoadIcon(SystemInformation.SmallIconSize);
            tray.ContextMenuStrip = trayMenu;
            tray.MouseClick += (o, e) => { if (e.Button == MouseButtons.Left) ShowFromTray(); };
            UpdateTrayText(TimeSpan.Zero);
            tray.Visible = true;

            ActiveControl = null;
            if (startWith > TimeSpan.Zero)
            {
                int m = (int)startWith.TotalMinutes;
                StartFor(startWith, startWith.TotalMinutes == m && Array.IndexOf(Presets, m) >= 0 ? m : -1);
            }
        }

        int S(float v) { return (int)Math.Round(v * s); }
        Rectangle R(float x, float y, float w, float h) { return new Rectangle(S(x), S(y), S(w), S(h)); }
        Rectangle ChipRect(int i) { return R(14 + i * 42, 90, 36, 30); }
        Rectangle CustomRect() { return R(14 + Presets.Length * 42 + 4, 90, LW - 14 - (14 + Presets.Length * 42 + 4), 30); }
        Rectangle MainBtnRect() { return R(LW - 90, 40, 76, 32); }
        Rectangle PlusBtnRect() { return R(LW - 90 - 60, 40, 54, 32); }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;          // gölge
                cp.Style |= 0x20000 | 0x80000;     // görev çubuğundan küçültme
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.RoundCorners(Handle);
        }

        // ---------- Durum ----------

        internal bool IsRunning { get { return running; } }
        internal TimeSpan Total { get { return total; } }
        internal bool WarningShown { get { return warning != null; } }
        internal WarningForm Warning { get { return warning; } }
        internal Settings SettingsObj { get { return settings; } }
        internal string CustomText { get { return custom.Text; } set { custom.Text = value; } }
        internal string Error { get { return error; } }
        internal void SetRemainingForTest(TimeSpan r) { endUtc = DateTime.UtcNow + r; }

        internal TimeSpan Remaining()
        {
            return running ? endUtc - DateTime.UtcNow : TimeSpan.Zero;
        }

        internal void StartFor(TimeSpan d, int preset)
        {
            total = d;
            endUtc = DateTime.UtcNow + d;
            running = true;
            warned = false;
            activePreset = preset;
            error = null;
            CloseWarning();
            tick.Interval = Visible ? 250 : 1000;
            tick.Start();
            Refresh(true);
        }

        internal void StartCustom()
        {
            TimeSpan d;
            if (!Duration.TryParse(custom.Text, out d))
            {
                error = custom.Text.Trim().Length == 0
                    ? Lang.T("Süre yaz: 90, 1:30, 2s", "Type a time: 90, 1:30, 2h")
                    : Lang.T("Geçersiz süre. Örnek: 90, 1:30, 2s", "Invalid time. Example: 90, 1:30, 2h");
                custom.Focus();
                custom.SelectAll();
                Invalidate();
                return;
            }
            settings.Custom = custom.Text.Trim();
            settings.Save();
            StartFor(d, -1);
        }

        internal void CancelTimer()
        {
            running = false;
            activePreset = -1;
            tick.Stop();
            CloseWarning();
            Refresh(true);
        }

        internal void Extend(int minutes)
        {
            if (!running) return;
            TimeSpan add = TimeSpan.FromMinutes(minutes);
            endUtc += add;
            total += add;
            warned = false;
            CloseWarning();
            Refresh(true);
        }

        internal void OnTick()
        {
            if (!running) { tick.Stop(); return; }
            TimeSpan rem = Remaining();
            if (rem <= TimeSpan.Zero)
            {
                // Süre bilgisayar uykudayken dolduysa uyanınca kapatma: sabah sürprizi olmasın.
                if (rem < TimeSpan.FromMinutes(-2))
                {
                    CancelTimer();
                    Balloon(Lang.T("Zamanlayıcı durduruldu", "Timer stopped"), Lang.T("Süre bilgisayar uykudayken doldu, işlem yapılmadı.", "Time ran out while the PC was asleep, so nothing was done."));
                    return;
                }
                Finish();
                return;
            }
            if (settings.Warn && !warned && rem.TotalSeconds <= 60)
            {
                warned = true;
                ShowWarning();
            }
            if (warning != null) warning.SetRemaining(rem);
            Refresh(false);
        }

        void Finish()
        {
            int action = settings.Action;
            running = false;
            activePreset = -1;
            tick.Stop();
            CloseWarning();
            Refresh(true);
            Power.Run(action);
        }

        // Ekranda görünen saniye değiştiyse çiz; boşuna çizim yok.
        void Refresh(bool force)
        {
            TimeSpan rem = Remaining();
            string t = running ? Ui.Fmt(rem) : "";
            if (!force && t == lastText) return;
            lastText = t;
            UpdateTrayText(rem);
            if (Visible) Invalidate();
        }

        void ShowWarning()
        {
            CloseWarning();
            warning = new WarningForm(s, settings.Action);
            warning.ExtendClicked += delegate { BeginInvoke(new MethodInvoker(delegate { Extend(15); })); };
            warning.CancelClicked += delegate { BeginInvoke(new MethodInvoker(CancelTimer)); };
            warning.SetRemaining(Remaining());
            warning.Show();
        }

        void CloseWarning()
        {
            if (warning == null) return;
            WarningForm w = warning;
            warning = null;
            w.Close();
            w.Dispose();
        }

        // ---------- Tepsi ----------

        void UpdateTrayText(TimeSpan rem)
        {
            string t = running
                ? "SleepTimer · " + Power.Line(settings.Action, Ui.Fmt(rem))
                : "SleepTimer · " + Lang.T("beklemede", "idle");
            if (t.Length > 63) t = t.Substring(0, 63);
            tray.Text = t;
        }

        void BuildTrayMenu()
        {
            trayMenu.Items.Clear();
            AddMenu(Lang.T("Göster", "Show"), ShowFromTray);
            trayMenu.Items.Add(new ToolStripSeparator());
            foreach (int m in Presets)
            {
                int mm = m;
                AddMenu(m + Lang.T(" dk", " min"), delegate { StartFor(TimeSpan.FromMinutes(mm), mm); });
            }
            if (running)
            {
                trayMenu.Items.Add(new ToolStripSeparator());
                AddMenu(Lang.T("+15 dk", "+15 min"), delegate { Extend(15); });
                AddMenu(Lang.T("İptal", "Cancel"), CancelTimer);
            }
            trayMenu.Items.Add(new ToolStripSeparator());
            AddMenu(running ? Lang.T("Çıkış (zamanlayıcı iptal)", "Exit (cancels the timer)") : Lang.T("Çıkış", "Exit"), ExitApp);
        }

        void AddMenu(string text, Action onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.ForeColor = Ui.Fg;
            item.Click += delegate { onClick(); };
            trayMenu.Items.Add(item);
        }

        void Balloon(string title, string text)
        {
            if (SuppressBalloons) return;
            tray.ShowBalloonTip(4000, title, text, ToolTipIcon.None);
        }

        void HideToTray()
        {
            Hide();
            if (running) tick.Interval = 1000;
            if (!settings.TrayHintShown)
            {
                settings.TrayHintShown = true;
                settings.Save();
                Balloon(Lang.T("SleepTimer arka planda", "SleepTimer is in the background"), Lang.T("Geri sayım sürüyor. Pencere için saatin yanındaki simgeye tıkla.", "The countdown keeps running. Click the icon next to the clock to open it."));
            }
        }

        internal void ShowFromTray()
        {
            Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
            if (running) tick.Interval = 250;
            Refresh(true);
        }

        internal void ExitApp()
        {
            exiting = true;
            Close();
        }

        // Açılışta özel kutusu odakta olmasın; rakam yazılırsa kutuya yönlendir.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ActiveControl = null;
            Invalidate();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!custom.Focused && keyData >= Keys.D0 && keyData <= Keys.D9 || !custom.Focused && keyData >= Keys.NumPad0 && keyData <= Keys.NumPad9)
            {
                custom.Focus();
                custom.Text = ((char)('0' + (keyData >= Keys.NumPad0 ? keyData - Keys.NumPad0 : keyData - Keys.D0))).ToString();
                custom.SelectionStart = custom.Text.Length;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && Visible) HideToTray();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!exiting && running && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            tick.Stop();
            CloseWarning();
            tray.Visible = false;
            tray.Dispose();
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (Program.ShowMessage != 0 && m.Msg == Program.ShowMessage)
            {
                ShowFromTray();
                return;
            }
            base.WndProc(ref m);
        }

        // ---------- Çizim ----------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Ui.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            hits.Clear();
            TimeSpan rem = Remaining();

            Ui.Text(g, "SleepTimer", fSmall, Ui.Muted, R(14, 2, 200, 28), TextFormatFlags.Left);
            WindowButton(g, "min", R(LW - 72, 0, 36, 30));
            WindowButton(g, "close", R(LW - 36, 0, 36, 30));

            Rectangle main = MainBtnRect();
            if (running)
            {
                Ui.Text(g, Ui.Fmt(rem), fBig, Ui.Fg, R(12, 34, 140, 44), TextFormatFlags.Left);
                Rectangle plus = PlusBtnRect();
                DrawButton(g, "plus", "+15", plus, false);
                DrawButton(g, "cancel", Lang.T("İptal", "Cancel"), main, false);
                Bar(g, S(156), plus.X - S(14), rem);
            }
            else
            {
                if (error != null)
                    Ui.Text(g, error, fSmall, Ui.Danger, new Rectangle(S(14), S(34), main.X - S(28), S(44)), TextFormatFlags.Left);
                else
                {
                    Ui.Text(g, Lang.T("Dakika seç", "Pick minutes"), fHint, Ui.Muted, R(14, 34, 140, 44), TextFormatFlags.Left);
                    Bar(g, S(156), main.X - S(14), TimeSpan.Zero);
                }
                DrawButton(g, "go", Lang.T("Başlat", "Start"), main, true);
            }

            for (int i = 0; i < Presets.Length; i++)
            {
                string id = "p" + Presets[i];
                Rectangle r = ChipRect(i);
                bool act = running && activePreset == Presets[i];
                Color c = act ? (hover == id ? Ui.AccentDarkHover : Ui.AccentDark) : (hover == id ? Ui.ChipHover : Ui.Chip);
                Ui.Fill(g, c, r, S(6));
                Ui.Text(g, Presets[i].ToString(), fChip, Ui.Fg, r, TextFormatFlags.HorizontalCenter);
                Hit(id, r);
            }

            Rectangle cr = CustomRect();
            Color cbg = running && activePreset < 0 ? Ui.AccentDark : (hover == "custom" ? Ui.ChipHover : Ui.Chip);
            if (custom.BackColor != cbg) custom.BackColor = cbg;
            Ui.Fill(g, cbg, cr, S(6));
            if (error != null) Ui.Stroke(g, Ui.Danger, cr, S(6), Math.Max(1, S(1.5f)));
            else if (custom.Focused) Ui.Stroke(g, Ui.Accent, cr, S(6), Math.Max(1, S(1)));
            Hit("custom", cr);

            int x = S(10);
            x = Pill(g, "action", Lang.T("Eylem: ", "Action: ") + Power.Name(settings.Action), x) + S(6);
            Pill(g, "warn", settings.Warn ? Lang.T("Uyarı: 1 dk önce", "Warning: 1 min before") : Lang.T("Uyarı: kapalı", "Warning: off"), x);
            if (running)
                Ui.Text(g, Lang.T("Bitiş ", "Ends ") + endUtc.ToLocalTime().ToString("HH:mm"), fSmall, Ui.Muted,
                    R(LW - 14 - 110, 130, 110, 26), TextFormatFlags.Right);
        }

        void Hit(string id, Rectangle r) { hits.Add(new KeyValuePair<string, Rectangle>(id, r)); }

        void WindowButton(Graphics g, string id, Rectangle r)
        {
            if (hover == id)
                using (var b = new SolidBrush(id == "close" ? Ui.DangerDark : Ui.Chip)) g.FillRectangle(b, r);
            int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2, k = S(5);
            using (var pen = new Pen(hover == id ? Ui.Fg : Ui.Muted, Math.Max(1f, s)))
            {
                if (id == "min") g.DrawLine(pen, cx - k, cy, cx + k, cy);
                else { g.DrawLine(pen, cx - k, cy - k, cx + k, cy + k); g.DrawLine(pen, cx - k, cy + k, cx + k, cy - k); }
            }
            Hit(id, r);
        }

        void DrawButton(Graphics g, string id, string text, Rectangle r, bool accent)
        {
            Color c;
            if (accent) c = hover == id ? Ui.AccentDarkHover : Ui.AccentDark;
            else if (hover == id) c = id == "cancel" ? Ui.DangerDark : Ui.ChipHover;
            else c = Ui.Chip;
            Ui.Fill(g, c, r, S(6));
            Ui.Text(g, text, fChip, Ui.Fg, r, TextFormatFlags.HorizontalCenter);
            Hit(id, r);
        }

        void Bar(Graphics g, int x1, int x2, TimeSpan rem)
        {
            if (x2 - x1 < S(20)) return;
            var track = new Rectangle(x1, S(54), x2 - x1, S(4));
            Ui.Fill(g, Ui.Chip, track, S(2));
            if (!running || total.TotalSeconds <= 0) return;
            double frac = Math.Max(0, Math.Min(1, rem.TotalSeconds / total.TotalSeconds));
            int w = (int)(track.Width * frac);
            if (w > 0) Ui.Fill(g, Ui.Accent, new Rectangle(track.X, track.Y, Math.Max(w, track.Height), track.Height), S(2));
        }

        int Pill(Graphics g, string id, string text, int x)
        {
            int w = TextRenderer.MeasureText(g, text, fSmall, Size.Empty, TextFormatFlags.NoPadding).Width + S(18);
            var r = new Rectangle(x, S(130), w, S(26));
            if (hover == id) Ui.Fill(g, Ui.Chip, r, S(6));
            else Ui.Stroke(g, Ui.Chip, r, S(6), 1);
            Ui.Text(g, text, fSmall, hover == id ? Ui.Fg : Ui.Muted, r, TextFormatFlags.HorizontalCenter);
            Hit(id, r);
            return r.Right;
        }

        // ---------- Fare / klavye ----------

        string HitAt(Point p)
        {
            for (int i = hits.Count - 1; i >= 0; i--)
                if (hits[i].Value.Contains(p)) return hits[i].Key;
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            string h = HitAt(e.Location);
            if (h == hover) return;
            hover = h;
            Cursor = h == null ? Cursors.Default : (h == "custom" ? Cursors.IBeam : Cursors.Hand);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hover != null) { hover = null; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            string h = HitAt(e.Location);
            if (h == null)
            {
                // Boş yerden tutup pencereyi taşı.
                Native.ReleaseCapture();
                Native.SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
                return;
            }
            pressed = h;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            string h = HitAt(e.Location), p = pressed;
            pressed = null;
            if (h != null && h == p) OnHit(h);
        }

        void OnHit(string id)
        {
            switch (id)
            {
                case "min": HideToTray(); break;
                case "close": if (running) HideToTray(); else ExitApp(); break;
                case "go": StartCustom(); break;
                case "cancel": CancelTimer(); break;
                case "plus": Extend(15); break;
                case "custom": custom.Focus(); custom.SelectAll(); break;
                case "action":
                    settings.Action = (settings.Action + 1) % 3;
                    settings.Save();
                    Refresh(true);
                    break;
                case "warn":
                    settings.Warn = !settings.Warn;
                    settings.Save();
                    if (!settings.Warn) CloseWarning();
                    Invalidate();
                    break;
                default:
                    if (id.StartsWith("p"))
                    {
                        int m = int.Parse(id.Substring(1));
                        ActiveControl = null;
                        StartFor(TimeSpan.FromMinutes(m), m);
                    }
                    break;
            }
        }

        void Custom_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                StartCustom();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                ActiveControl = null;
                Invalidate();
            }
        }
    }
}
