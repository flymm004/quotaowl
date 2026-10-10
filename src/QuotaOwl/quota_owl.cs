using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace QuotaOwl
{
    internal sealed class ProviderState
    {
        public readonly string Name;
        public readonly Color Accent;
        public UsageResult Result;
        public UsageReadException Error;
        public DateTime UpdatedAt;
        public bool Reading;
        public bool WasLow;
        public ProviderState(string name, Color accent) { Name = name; Accent = accent; }
    }

    internal sealed class UsageApplicationContext : ApplicationContext
    {
        private readonly ProviderState[] providers = {
            new ProviderState("Codex", Palette.Blue), new ProviderState("Claude", Palette.Orange)
        };
        private readonly UsageWindow window;
        private readonly System.Windows.Forms.Timer timer;
        private readonly List<ToolStripMenuItem> languageChoices = new List<ToolStripMenuItem>();
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem summaryItem, showItem, refreshItem, languageItem, exitItem;
        private readonly NotifyIcon tray;
        private Icon trayIcon;

        public UsageApplicationContext()
        {
            window = new UsageWindow(providers, RefreshUsage, ChangeLanguage);
            menu = new ContextMenuStrip { BackColor = Palette.Panel, ForeColor = Palette.Text, Renderer = new DarkMenuRenderer() };
            summaryItem = new ToolStripMenuItem { Enabled = false };
            showItem = new ToolStripMenuItem("", null, delegate { ShowWindow(); });
            refreshItem = new ToolStripMenuItem("", null, delegate { RefreshUsage(); });
            languageItem = new ToolStripMenuItem();
            foreach (string code in UiText.LanguageCodes) AddLanguage(code);
            languageItem.DropDownItems.Add(new ToolStripSeparator());
            AddLanguage("system");
            exitItem = new ToolStripMenuItem("", null, delegate { ExitApplication(); });
            menu.Items.AddRange(new ToolStripItem[] { summaryItem, new ToolStripSeparator(), showItem, refreshItem, languageItem, new ToolStripSeparator(), exitItem });
            tray = new NotifyIcon { ContextMenuStrip = menu };
            UpdateTray();
            tray.Visible = true;
            tray.MouseClick += delegate(object sender, MouseEventArgs e) {
                if (e.Button == MouseButtons.Left) { if (window.Visible) window.Hide(); else ShowWindow(); }
            };
            ApplyLanguage();
            timer = new System.Windows.Forms.Timer { Interval = 120000 };
            timer.Tick += delegate { RefreshUsage(); };
            timer.Start();
            ShowWindow();
            RefreshUsage();
        }

        private void AddLanguage(string code)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(UiText.LanguageName(code)) { Tag = code };
            item.Click += delegate { ChangeLanguage((string)item.Tag); };
            languageChoices.Add(item);
            languageItem.DropDownItems.Add(item);
        }

        private void ApplyLanguage()
        {
            menu.Font = new Font(UiText.FontName, 9F);
            showItem.Text = UiText.Get("menuShow");
            refreshItem.Text = UiText.Get("menuRefresh");
            languageItem.Text = UiText.Get("menuLanguage");
            exitItem.Text = UiText.Get("menuExit");
            foreach (ToolStripMenuItem item in languageChoices) {
                if ((string)item.Tag == "system") item.Text = UiText.Get("systemLanguage");
                item.Checked = (string)item.Tag == UiText.Preference;
            }
            UpdateTray();
            window.ApplyLanguage();
        }

        private void ChangeLanguage(string code) { UiText.Select(code); ApplyLanguage(); }

        private void ShowWindow()
        {
            // Show first so PerMonitorV2 calculates the final size on the target screen.
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            window.Location = new Point(area.Right - window.Width - 8, area.Bottom - window.Height - 8);
            window.Show();
            window.Location = new Point(Math.Max(area.Left, area.Right - window.Width - 8), Math.Max(area.Top, area.Bottom - window.Height - 8));
            window.BringToFront();
            window.Activate();
        }

        private void RefreshUsage()
        {
            foreach (ProviderState state in providers) RefreshProvider(state);
        }

        private void RefreshProvider(ProviderState state)
        {
            if (state.Reading) return;
            state.Reading = true;
            window.Render();
            ThreadPool.QueueUserWorkItem(delegate {
                UsageResult result = null;
                UsageReadException error = null;
                try { result = state.Name == "Codex" ? CodexReader.Read() : ClaudeReader.Read(); }
                catch (UsageReadException ex) { error = ex; }
                catch (Exception) { error = new UsageReadException(state.Name == "Codex" ? "errorGeneric" : "errorClaude"); }
                try {
                    if (window.IsDisposed || !window.IsHandleCreated) return;
                    window.BeginInvoke((MethodInvoker)delegate {
                        if (window.IsDisposed) return;
                        // Retain a last successful reading on transient failures, but never
                        // carry subscription data into a signed-out or third-party account.
                        if (result != null || !CanKeepLastReading(error)) state.Result = result;
                        state.Error = error;
                        state.Reading = false;
                        if (result != null) state.UpdatedAt = DateTime.Now;
                        UpdateTray();
                        bool low = result != null && result.Rows.Exists(delegate(UsageRow row) { return row.RemainingPercent < 20; });
                        if (low && !state.WasLow) {
                            tray.BalloonTipTitle = "QuotaOwl · " + state.Name;
                            tray.BalloonTipText = UiText.Get("lowBody");
                            tray.BalloonTipIcon = ToolTipIcon.Warning;
                            tray.ShowBalloonTip(4500);
                        }
                        if (result != null || state.Result == null) state.WasLow = low;
                        window.Render();
                    });
                } catch (InvalidOperationException) { }
            });
        }

        private static bool CanKeepLastReading(UsageReadException error)
        {
            return error != null && (error.Key == "errorTimeout" || error.Key == "errorCliStart" ||
                error.Key == "errorServerStart" || error.Key == "errorGeneric" ||
                error.Key == "errorClaudeBusy" || error.Key == "errorClaude");
        }

        private void UpdateTray()
        {
            ProviderState lowestProvider = null;
            UsageRow lowest = null;
            bool incomplete = false;
            foreach (ProviderState state in providers) {
                incomplete |= state.Error != null;
                if (state.Result == null) continue;
                foreach (UsageRow row in state.Result.Rows) {
                    if (row.LimitId != state.Name.ToLowerInvariant() || row.RemainingPercent < 0 || row.RemainingPercent > 100) continue;
                    // Keep ties stable; prefer a fresh reading when an equal cached value exists.
                    if (lowest == null || row.RemainingPercent < lowest.RemainingPercent ||
                        (row.RemainingPercent == lowest.RemainingPercent && lowestProvider.Error != null && state.Error == null)) {
                        lowest = row;
                        lowestProvider = state;
                    }
                }
            }
            Icon previous = trayIcon;
            trayIcon = TrayIconFactory.Create(lowest == null ? (int?)null : lowest.RemainingPercent,
                incomplete, lowestProvider == null ? Palette.Muted : lowestProvider.Accent, "—");
            tray.Icon = trayIcon;
            if (previous != null) previous.Dispose();
            summaryItem.Text = lowest == null ? OwlText.Get("trayUnknown") :
                OwlText.Get("trayLowest") + " · " + lowestProvider.Name + " · " +
                UiText.WindowLabel(lowest.WindowMinutes) + " " + lowest.RemainingPercent + "%" +
                (lowestProvider.Error == null ? "" : " · " + OwlText.Get("stale"));
            // NotifyIcon.Text is limited to 63 characters on .NET Framework. Compact
            // duration units keep all four values visible, including 100% and stale marks.
            string text = "QuotaOwl";
            foreach (ProviderState state in providers) {
                text += "\n" + state.Name;
                if (state.Error != null) text += state.Result == null ? "!" : "(" + OwlText.Get("staleShort") + ")";
                int count = 0;
                if (state.Result != null) foreach (UsageRow row in state.Result.Rows) {
                    if (row.LimitId != state.Name.ToLowerInvariant() || row.RemainingPercent < 0 || row.RemainingPercent > 100) continue;
                    if (count++ == 2) break;
                    string duration = row.WindowMinutes % 1440 == 0 ? (row.WindowMinutes / 1440) + "d" :
                        row.WindowMinutes % 60 == 0 ? (row.WindowMinutes / 60) + "h" : row.WindowMinutes + "m";
                    text += " " + duration + row.RemainingPercent + "%" + (row == lowest ? "*" : "");
                }
                if (count == 0) text += " —";
            }
            tray.Text = text.Length > 63 ? text.Substring(0, 62) + "…" : text;
        }

        private void ExitApplication()
        {
            timer.Stop(); timer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            if (trayIcon != null) trayIcon.Dispose();
            menu.Dispose();
            window.CloseForExit();
            ExitThread();
        }
    }

    internal sealed class UsageWindow : Form
    {
        private readonly ProviderState[] providers;
        private readonly ProviderPanel[] sections;
        private readonly Button refreshButton, infoButton, languageButton;
        private readonly Label subtitle, footer;
        private readonly ContextMenuStrip languageMenu;
        private readonly ToolTip tips = new ToolTip();
        private bool menuOpen, allowClose;

        public UsageWindow(ProviderState[] states, Action refresh, Action<string> changeLanguage)
        {
            SuspendLayout();
            providers = states;
            Text = "QuotaOwl";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Palette.Panel;
            ForeColor = Palette.Text;
            ClientSize = new Size(352, 468);
            Font = new Font(UiText.FontName, 9F);
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (ArgumentException) { }
            Panel owl = new Panel();
            owl.SetBounds(20, 17, 32, 32);
            owl.Paint += delegate(object sender, PaintEventArgs e) { OwlMark.Draw(e.Graphics, owl.ClientRectangle); };
            Controls.Add(owl);
            Controls.Add(LabelAt("QuotaOwl", 63, 15, 193, 25, 13F, Palette.Text, true));
            subtitle = LabelAt("", 63, 40, 194, 18, 7.5F, Palette.Dim, false);
            Controls.Add(subtitle);
            languageButton = ButtonAt("", 273, 19, 26, 26);
            languageButton.Paint += delegate(object sender, PaintEventArgs e) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float s = languageButton.Width * .55F, x = (languageButton.Width - s) / 2, y = (languageButton.Height - s) / 2;
                using (Pen pen = new Pen(Palette.Muted, Math.Max(1, DeviceDpi / 96F))) {
                    e.Graphics.DrawEllipse(pen, x, y, s, s);
                    e.Graphics.DrawEllipse(pen, x + s * .28F, y, s * .44F, s);
                    e.Graphics.DrawLine(pen, x, y + s / 2, x + s, y + s / 2);
                }
            };
            Controls.Add(languageButton);
            Button close = ButtonAt("×", 306, 18, 26, 26);
            close.Font = new Font("Segoe UI", 14F);
            close.Click += delegate { Hide(); };
            Controls.Add(close);
            tips.SetToolTip(close, UiText.Get("closeTooltip"));

            languageMenu = new ContextMenuStrip { BackColor = Palette.Panel, ForeColor = Palette.Text, Renderer = new DarkMenuRenderer() };
            foreach (string code in UiText.LanguageCodes) AddLanguage(code, changeLanguage);
            languageMenu.Items.Add(new ToolStripSeparator());
            AddLanguage("system", changeLanguage);
            languageMenu.Closed += delegate { menuOpen = false; };
            languageButton.Click += delegate {
                ApplyMenuLanguage(); menuOpen = true;
                languageMenu.Show(languageButton, new Point(0, languageButton.Height));
            };

            sections = new ProviderPanel[states.Length];
            for (int i = 0; i < states.Length; i++) {
                sections[i] = new ProviderPanel(states[i]);
                sections[i].SetBounds(20, i == 0 ? 74 : 258, 312, 164);
                Controls.Add(sections[i]);
            }
            footer = LabelAt("", 20, 434, 130, 22, 7F, Palette.Dim, false);
            Controls.Add(footer);
            infoButton = ButtonAt("", 163, 428, 68, 28);
            infoButton.Click += delegate {
                menuOpen = true;
                try { MessageBox.Show(this, OwlText.Get("about"), "QuotaOwl", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                finally { menuOpen = false; }
            };
            Controls.Add(infoButton);
            refreshButton = ButtonAt("", 240, 428, 92, 28);
            refreshButton.ForeColor = Palette.Text;
            refreshButton.BackColor = Palette.Button;
            refreshButton.FlatAppearance.BorderSize = 1;
            refreshButton.FlatAppearance.BorderColor = Palette.Line;
            refreshButton.Click += delegate { refresh(); };
            Controls.Add(refreshButton);
            Deactivate += delegate { if (Visible && !menuOpen) Hide(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (!allowClose && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            };
            SizeChanged += delegate { UpdateRegion(); };
            DpiChanged += delegate { BeginInvoke((MethodInvoker)delegate { Render(); }); };
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ResumeLayout(false);
            ApplyLanguage();
            UpdateRegion();
        }

        private void AddLanguage(string code, Action<string> changeLanguage)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(UiText.LanguageName(code)) { Tag = code };
            item.Click += delegate { changeLanguage((string)item.Tag); };
            languageMenu.Items.Add(item);
        }
        private void ApplyMenuLanguage()
        {
            foreach (ToolStripItem candidate in languageMenu.Items) {
                ToolStripMenuItem item = candidate as ToolStripMenuItem;
                if (item == null) continue;
                if ((string)item.Tag == "system") item.Text = UiText.Get("systemLanguage");
                item.Checked = (string)item.Tag == UiText.Preference;
            }
        }
        public void ApplyLanguage()
        {
            languageMenu.Font = new Font(UiText.FontName, 9F);
            subtitle.Text = OwlText.Get("subtitle");
            footer.Text = OwlText.Get("trayLowest");
            tips.SetToolTip(footer, OwlText.Get("trayHint"));
            subtitle.Font = new Font(UiText.FontName, 7.5F);
            footer.Font = new Font(UiText.FontName, 7F);
            infoButton.Text = UiText.Get("info");
            refreshButton.Text = UiText.Get("refresh");
            infoButton.Font = new Font(UiText.FontName, 8F);
            refreshButton.Font = new Font(UiText.FontName, 8F);
            tips.SetToolTip(languageButton, UiText.Get("languageTooltip"));
            tips.SetToolTip(refreshButton, UiText.Get("refreshTooltip"));
            ApplyMenuLanguage();
            Render();
        }
        public void Render()
        {
            bool reading = false;
            for (int i = 0; i < providers.Length; i++) { sections[i].Render(); reading |= providers[i].Reading; }
            refreshButton.Enabled = !reading;
            refreshButton.Text = UiText.Get("refresh");
        }
        public void CloseForExit() { allowClose = true; Close(); }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { tips.Dispose(); languageMenu.Dispose(); }
            base.Dispose(disposing);
        }
        private void UpdateRegion()
        {
            if (Width < 32 || Height < 32) return;
            using (GraphicsPath path = new GraphicsPath()) {
                int d = (int)(24 * Width / 352F);
                path.AddArc(0, 0, d, d, 180, 90);
                path.AddArc(Width - d - 1, 0, d, d, 270, 90);
                path.AddArc(Width - d - 1, Height - d - 1, d, d, 0, 90);
                path.AddArc(0, Height - d - 1, d, d, 90, 90); path.CloseFigure();
                Region previous = Region; Region = new Region(path); if (previous != null) previous.Dispose();
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            float scale = ClientSize.Width / 352F;
            using (Pen pen = new Pen(Palette.Line)) {
                e.Graphics.DrawLine(pen, 20 * scale, 244 * scale, 332 * scale, 244 * scale);
            }
        }
        internal static Label LabelAt(string text, int x, int y, int w, int h, float size, Color color, bool bold)
        {
            Label label = new Label { Text = text, ForeColor = color, BackColor = Palette.Panel, AutoSize = false,
                Font = new Font(UiText.FontName, size, bold ? FontStyle.Bold : FontStyle.Regular), TextAlign = ContentAlignment.MiddleLeft };
            label.SetBounds(x, y, w, h); return label;
        }
        private static Button ButtonAt(string text, int x, int y, int w, int h)
        {
            Button button = new Button { Text = text, FlatStyle = FlatStyle.Flat, ForeColor = Palette.Muted,
                BackColor = Palette.Panel, Cursor = Cursors.Hand, Font = new Font(UiText.FontName, 8F) };
            button.FlatAppearance.BorderSize = 0;
            button.SetBounds(x, y, w, h); return button;
        }
    }

    internal sealed class ProviderPanel : Panel
    {
        private readonly ProviderState state;
        public ProviderPanel(ProviderState value) { state = value; BackColor = Palette.Panel; DoubleBuffered = true; }
        public void Render()
        {
            SuspendLayout();
            while (Controls.Count > 0) { Control c = Controls[0]; Controls.Remove(c); c.Dispose(); }
            float scale = Width / 312F;
            Func<int, int> px = delegate(int n) { return (int)Math.Round(n * scale); };
            Add(state.Name, 0, 0, 68, 23, 10F, state.Accent, true, scale);
            string plan = state.Result == null ? "" : state.Result.PlanType;
            Add(String.IsNullOrEmpty(plan) ? "" : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(plan), 70, 1, 73, 21, 7.5F, Palette.Muted, false, scale);
            string status = state.Reading ? UiText.Get("updating") : state.Error != null ?
                state.Result == null ? UiText.Get("readFailed") : OwlText.Get("stale") + " · " + state.UpdatedAt.ToString("HH:mm") :
                state.Result == null ? UiText.Get("reading") : UiText.Format("updatedAt", state.UpdatedAt.ToString("HH:mm"));
            Label timestamp = Add(status, 143, 1, 169, 21, 7F, state.Error != null ? Palette.Error : Palette.Dim, false, scale);
            timestamp.TextAlign = ContentAlignment.MiddleRight;
            if (state.Result != null) {
                int y = 30;
                foreach (UsageRow row in state.Result.Rows) {
                    if (row.LimitId != state.Name.ToLowerInvariant()) continue;
                    if (y > 90) break;
                    Label title = Add(UiText.WindowLabel(row.WindowMinutes), 0, y, 225, 22, 9F, Palette.Text, true, scale);
                    Label percent = Add(row.RemainingPercent + "%", 245, y, 67, 22, 9F, row.RemainingPercent < 20 ? Palette.Amber : state.Accent, true, scale);
                    percent.TextAlign = ContentAlignment.MiddleRight;
                    ProgressTrack progress = new ProgressTrack(row.RemainingPercent, row.RemainingPercent < 20, state.Accent);
                    progress.SetBounds(0, px(y + 26), px(312), Math.Max(3, px(4))); Controls.Add(progress);
                    Add(row.ResetsAt > 0 ? UiText.Format("resetAt", UiText.ResetTime(row.ResetsAt)) : OwlText.Get("resetUnknown"),
                        0, y + 35, 312, 18, 7.5F, Palette.Muted, false, scale);
                    y += 57;
                }
                if (state.Name == "Codex") Add(state.Result.AvailableResetCount.HasValue ?
                    state.Result.AvailableResetCount.Value == 0 ? UiText.Get("noResets") : UiText.ResetCredits(state.Result.AvailableResetCount.Value) :
                    UiText.Get("resetCountUnknown"), 0, 143, 312, 17, 7F, Palette.Dim, false, scale);
                else {
                    // Claude's OAuth usage surface does not expose this account's reset grants.
                    // A missing entitlement is unknown, never an explicit zero.
                    LinkLabel resets = new LinkLabel { Text = OwlText.Get("resetWeb"),
                        LinkColor = state.Accent, ActiveLinkColor = Palette.Text,
                        VisitedLinkColor = state.Accent, BackColor = Palette.Panel,
                        Font = new Font(UiText.FontName, 7F), LinkBehavior = LinkBehavior.HoverUnderline,
                        TextAlign = ContentAlignment.MiddleLeft };
                    resets.SetBounds(0, px(143), px(312), px(17));
                    resets.LinkClicked += delegate {
                        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                            "https://claude.ai/settings/usage") { UseShellExecute = true }); }
                        catch (System.ComponentModel.Win32Exception) {
                            MessageBox.Show(OwlText.Get("browserError"), "QuotaOwl", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    };
                    Controls.Add(resets);
                }
            } else {
                string message = state.Error == null ? UiText.Get("loadingUsage") :
                    state.Error.Key.StartsWith("errorClaude", StringComparison.Ordinal) ? OwlText.Get(state.Error.Key) : UiText.Error(state.Error);
                Add(message, 0, 36, 312, 84, 8F, state.Error == null ? Palette.Muted : Palette.Error, false, scale);
            }
            ResumeLayout(false);
        }
        private Label Add(string text, int x, int y, int w, int h, float font, Color color, bool bold, float scale)
        {
            Label label = UsageWindow.LabelAt(text, (int)Math.Round(x * scale), (int)Math.Round(y * scale),
                (int)Math.Round(w * scale), (int)Math.Round(h * scale), font, color, bold);
            Controls.Add(label); return label;
        }
    }

    internal static class OwlMark
    {
        public static void Draw(Graphics graphics, Rectangle bounds)
        {
            GraphicsState saved = graphics.Save();
            graphics.TranslateTransform(bounds.X, bounds.Y);
            graphics.ScaleTransform(bounds.Width / 32F, bounds.Height / 32F);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Brush body = new SolidBrush(Color.FromArgb(43, 54, 67))) {
                graphics.FillEllipse(body, 3, 6, 26, 25);
                graphics.FillPolygon(body, new PointF[] { new PointF(3, 13), new PointF(3, 2), new PointF(14, 8) });
                graphics.FillPolygon(body, new PointF[] { new PointF(18, 8), new PointF(29, 2), new PointF(29, 13) });
            }
            using (Brush blue = new SolidBrush(Palette.Blue)) graphics.FillEllipse(blue, 5, 9, 11, 13);
            using (Brush orange = new SolidBrush(Palette.Orange)) graphics.FillEllipse(orange, 16, 9, 11, 13);
            using (Brush eye = new SolidBrush(Palette.Panel)) { graphics.FillEllipse(eye, 9, 12, 4, 6); graphics.FillEllipse(eye, 19, 12, 4, 6); }
            using (Brush beak = new SolidBrush(Palette.Text)) graphics.FillPolygon(beak, new PointF[] { new PointF(14, 22), new PointF(18, 22), new PointF(16, 26) });
            graphics.Restore(saved);
        }
    }
}
