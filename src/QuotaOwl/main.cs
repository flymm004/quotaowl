using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Globalization;

[assembly: AssemblyTitle("QuotaOwl")]
[assembly: AssemblyDescription("Windows tray monitor for Codex and Claude subscription quotas")]
[assembly: AssemblyCompany("QuotaOwl contributors")]
[assembly: AssemblyProduct("QuotaOwl")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8", FrameworkDisplayName = ".NET Framework 4.8")]

namespace QuotaOwl
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool created;
            // Keep this identity stable so renamed versions still share one process instance.
            using (Mutex mutex = new Mutex(true, "Local\\CodexUsageBarMvp", out created))
            {
                if (!created) return;
                Application.Run(new UsageApplicationContext());
            }
        }
    }

    internal sealed class UsageResult
    {
        public string PlanType;
        public int? AvailableResetCount;
        public List<UsageRow> Rows = new List<UsageRow>();
    }

    internal sealed class UsageRow
    {
        public string LimitId;
        public string Period;
        public string Label;
        public int WindowMinutes;
        public int UsedPercent;
        public int RemainingPercent;
        public long ResetsAt;
    }

    internal sealed class UsageReadException : Exception
    {
        public readonly string Key;
        public readonly string Detail;
        public UsageReadException(string key, string detail = null) : base(key)
        {
            Key = key;
            Detail = detail;
        }
    }

    internal static class CodexReader
    {
        private const int TimeoutSeconds = 15;

        public static UsageResult Read()
        {
            Process process = null;
            Queue<string> lines = new Queue<string>();
            object queueLock = new object();
            AutoResetEvent lineArrived = new AutoResetEvent(false);
            try
            {
                string codexPath = FindCodexExecutable();
                if (String.IsNullOrEmpty(codexPath))
                    throw new UsageReadException("errorCliMissing");

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = codexPath;
                start.Arguments = "app-server --listen stdio://";
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.RedirectStandardInput = true;
                start.RedirectStandardOutput = true;
                start.RedirectStandardError = true;

                try { process = Process.Start(start); }
                catch (Win32Exception)
                {
                    throw new UsageReadException("errorCliStart");
                }
                if (process == null) throw new UsageReadException("errorServerStart");

                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (queueLock) lines.Enqueue(e.Data);
                    lineArrived.Set();
                };
                process.ErrorDataReceived += delegate { };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.StandardInput.AutoFlush = true;

                Request(process, lines, queueLock, lineArrived, "initialize", 0,
                    "\"params\":{\"clientInfo\":{\"name\":\"quota-owl\",\"title\":\"QuotaOwl\",\"version\":\"0.2.0\"}}");
                process.StandardInput.WriteLine("{\"method\":\"initialized\",\"params\":{}}");

                Dictionary<string, object> accountResult = Request(process, lines, queueLock, lineArrived,
                    "account/read", 1, "\"params\":{\"refreshToken\":false}");
                Dictionary<string, object> account = GetDictionary(accountResult, "account");
                if (account == null)
                    throw new UsageReadException("errorNoAccount");
                string accountType = GetString(account, "type");
                if (!String.Equals(accountType, "chatgpt", StringComparison.OrdinalIgnoreCase))
                    throw new UsageReadException("errorApiKey");

                Dictionary<string, object> limitsResult = Request(process, lines, queueLock, lineArrived,
                    "account/rateLimits/read", 2, null);
                UsageResult result = new UsageResult();
                result.PlanType = GetString(account, "planType");
                Dictionary<string, object> rateLimits = GetDictionary(limitsResult, "rateLimits");
                if (String.IsNullOrEmpty(result.PlanType) && rateLimits != null)
                    result.PlanType = GetString(rateLimits, "planType");
                if (String.IsNullOrEmpty(result.PlanType)) result.PlanType = "ChatGPT";

                Dictionary<string, object> buckets = GetDictionary(limitsResult, "rateLimitsByLimitId");
                if (buckets != null)
                {
                    foreach (KeyValuePair<string, object> item in buckets)
                        AddBucketRows(result.Rows, item.Key, GetDictionary(item.Value));
                }
                if (result.Rows.Count == 0 && rateLimits != null)
                    AddBucketRows(result.Rows, GetString(rateLimits, "limitId") ?? "codex", rateLimits);
                if (result.Rows.Count == 0)
                    throw new UsageReadException("errorNoLimits");

                Dictionary<string, object> resetCredits = GetDictionary(limitsResult, "rateLimitResetCredits");
                object count;
                if (resetCredits != null && resetCredits.TryGetValue("availableCount", out count))
                {
                    int parsed;
                    if (Int32.TryParse(Convert.ToString(count, CultureInfo.InvariantCulture), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out parsed)) result.AvailableResetCount = Math.Max(0, parsed);
                }
                return result;
            }
            finally
            {
                if (process != null)
                {
                    try { process.StandardInput.Close(); } catch { }
                    try
                    {
                        if (!process.WaitForExit(900)) process.Kill();
                    }
                    catch { }
                    process.Dispose();
                }
                lineArrived.Dispose();
            }
        }

        private static string FindCodexExecutable()
        {
            string installRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenAI", "Codex", "bin");
            string direct = Path.Combine(installRoot, "codex.exe");
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            string[] pathEntries = path.Split(Path.PathSeparator);
            for (int i = 0; i < pathEntries.Length; i++)
            {
                string entry = pathEntries[i].Trim().Trim('"');
                if (entry.Length == 0) continue;
                string candidate = Path.Combine(entry, "codex.exe");
                // Older desktop installs can leave a stale unversioned binary behind.
                // Resolve that location through the versioned desktop packages below.
                if (String.Equals(candidate, direct, StringComparison.OrdinalIgnoreCase)) continue;
                if (File.Exists(candidate)) return candidate;
            }

            if (!Directory.Exists(installRoot)) return null;

            string newest = null;
            DateTime newestWrite = DateTime.MinValue;
            string[] versionDirectories = Directory.GetDirectories(installRoot);
            for (int i = 0; i < versionDirectories.Length; i++)
            {
                string candidate = Path.Combine(versionDirectories[i], "codex.exe");
                if (!File.Exists(candidate)) continue;
                DateTime writeTime = File.GetLastWriteTimeUtc(candidate);
                if (writeTime > newestWrite)
                {
                    newest = candidate;
                    newestWrite = writeTime;
                }
            }
            return newest ?? (File.Exists(direct) ? direct : null);
        }

        private static Dictionary<string, object> Request(Process process, Queue<string> lines,
            object queueLock, AutoResetEvent lineArrived, string method, int requestId, string parameters)
        {
            string request = "{\"method\":\"" + method + "\",\"id\":" + requestId;
            if (!String.IsNullOrEmpty(parameters)) request += "," + parameters;
            request += "}";
            process.StandardInput.WriteLine(request);

            DateTime deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            while (DateTime.UtcNow < deadline)
            {
                string line = null;
                lock (queueLock)
                {
                    if (lines.Count > 0) line = lines.Dequeue();
                }
                if (line == null)
                {
                    lineArrived.WaitOne(100);
                    continue;
                }
                Dictionary<string, object> message;
                try { message = serializer.Deserialize<Dictionary<string, object>>(line); }
                catch { continue; }
                if (message == null) continue;
                object idValue;
                if (!message.TryGetValue("id", out idValue) ||
                    Convert.ToString(idValue, CultureInfo.InvariantCulture) != requestId.ToString(CultureInfo.InvariantCulture)) continue;
                object error;
                if (message.TryGetValue("error", out error))
                {
                    Dictionary<string, object> errorObject = GetDictionary(error);
                    string detail = errorObject == null ? null : GetString(errorObject, "message");
                    throw new UsageReadException("errorServer", detail);
                }
                return GetDictionary(message, "result") ?? new Dictionary<string, object>();
            }
            throw new UsageReadException("errorTimeout");
        }

        private static void AddBucketRows(List<UsageRow> rows, string key, Dictionary<string, object> bucket)
        {
            if (bucket == null) return;
            string limitId = GetString(bucket, "limitId") ?? key;
            string limitName = GetString(bucket, "limitName");
            AddWindowRow(rows, limitId, limitName, "primary", GetDictionary(bucket, "primary"));
            AddWindowRow(rows, limitId, limitName, "secondary", GetDictionary(bucket, "secondary"));
        }

        private static void AddWindowRow(List<UsageRow> rows, string limitId, string limitName,
            string period, Dictionary<string, object> value)
        {
            if (value == null) return;
            int used;
            int minutes;
            long resetsAt;
            if (!TryGetInt(value, "usedPercent", out used) || !TryGetInt(value, "windowDurationMins", out minutes) ||
                !TryGetLong(value, "resetsAt", out resetsAt) || minutes <= 0 || resetsAt <= 0) return;
            used = Math.Max(0, Math.Min(100, used));
            string label = String.IsNullOrEmpty(limitName) ? limitId.Replace('_', ' ') : limitName;
            UsageRow row = new UsageRow();
            row.LimitId = limitId;
            row.Period = period;
            row.Label = label;
            row.WindowMinutes = minutes;
            row.UsedPercent = used;
            row.RemainingPercent = 100 - used;
            row.ResetsAt = resetsAt;
            rows.Add(row);
        }

        private static bool TryGetInt(Dictionary<string, object> data, string key, out int value)
        {
            object raw;
            double parsed;
            if (data != null && data.TryGetValue(key, out raw) &&
                Double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out parsed))
            {
                value = (int)Math.Round(parsed, MidpointRounding.AwayFromZero);
                return true;
            }
            value = 0;
            return false;
        }

        private static bool TryGetLong(Dictionary<string, object> data, string key, out long value)
        {
            object raw;
            if (data != null && data.TryGetValue(key, out raw) &&
                Int64.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out value)) return true;
            value = 0;
            return false;
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : null;
        }

        private static Dictionary<string, object> GetDictionary(Dictionary<string, object> data, string key)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) ? GetDictionary(value) : null;
        }

        private static Dictionary<string, object> GetDictionary(object value)
        {
            return value as Dictionary<string, object>;
        }
    }

    internal sealed class ProgressTrack : Control
    {
        private readonly int percent;
        private readonly bool low;

        public ProgressTrack(int value, bool isLow) : this(value, isLow, Palette.Blue) { }
        private readonly Color accent;
        public ProgressTrack(int value, bool isLow, Color providerAccent)
        {
            percent = Math.Max(0, Math.Min(100, value));
            low = isLow; accent = providerAccent;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Palette.Panel;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(3, Height - 1));
            using (Brush background = new SolidBrush(Palette.Track))
                FillRounded(e.Graphics, background, track, 3);
            int fillWidth = (int)Math.Round(track.Width * (percent / 100.0));
            if (fillWidth > 0)
            {
                Rectangle fill = new Rectangle(0, 0, Math.Min(track.Width, Math.Max(5, fillWidth)), track.Height);
                using (Brush foreground = new SolidBrush(low ? Palette.Amber : accent))
                    FillRounded(e.Graphics, foreground, fill, 3);
            }
            base.OnPaint(e);
        }

        private static void FillRounded(Graphics graphics, Brush brush, Rectangle rect, int radius)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                int d = Math.Min(radius * 2, rect.Height);
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                graphics.FillPath(brush, path);
            }
        }
    }

    internal static class TrayIconFactory
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon Create(int? remainingPercent, bool error, Color providerAccent, string fallback)
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Color accent = providerAccent;
                using (Brush background = new SolidBrush(Color.FromArgb(25, 34, 44)))
                    graphics.FillRoundedRectangle(background, new Rectangle(1, 1, 30, 30), 8);
                using (Pen border = new Pen(providerAccent, 1.5F))
                    graphics.DrawRoundedRectangle(border, new Rectangle(1, 1, 30, 30), 8);
                string text = remainingPercent.HasValue ? remainingPercent.Value.ToString(CultureInfo.InvariantCulture) : fallback;
                float fontSize = text.Length >= 3 ? 13F : text == "C" ? 19F : 18F;
                using (Font font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (Brush foreground = new SolidBrush(accent))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    graphics.DrawString(text, font, foreground, new RectangleF(1, 1, 30, 30), format);
                }
                if (error || (remainingPercent.HasValue && remainingPercent.Value < 20)) {
                    using (Brush warning = new SolidBrush(Palette.Amber))
                        graphics.FillEllipse(warning, 25, 2, 5, 5);
                }
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); }
                finally { DestroyIcon(handle); }
            }
        }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius)) graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius)) graphics.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal static class Palette
    {
        public static readonly Color Panel = Color.FromArgb(20, 28, 35);
        public static readonly Color Button = Color.FromArgb(35, 45, 52);
        public static readonly Color ButtonText = Color.FromArgb(16, 35, 27);
        public static readonly Color Text = Color.FromArgb(239, 245, 245);
        public static readonly Color Muted = Color.FromArgb(169, 183, 189);
        public static readonly Color Dim = Color.FromArgb(122, 139, 148);
        public static readonly Color Line = Color.FromArgb(51, 63, 70);
        public static readonly Color Track = Color.FromArgb(47, 60, 66);
        public static readonly Color Blue = Color.FromArgb(137, 190, 255);
        public static readonly Color Orange = Color.FromArgb(239, 177, 137);
        public static readonly Color Green = Color.FromArgb(171, 233, 198);
        public static readonly Color GreenStrong = Color.FromArgb(113, 216, 158);
        public static readonly Color Amber = Color.FromArgb(239, 197, 125);
        public static readonly Color Error = Color.FromArgb(226, 140, 130);
    }

    internal sealed class DarkMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Palette.Panel; } }
        public override Color ImageMarginGradientBegin { get { return Palette.Panel; } }
        public override Color ImageMarginGradientMiddle { get { return Palette.Panel; } }
        public override Color ImageMarginGradientEnd { get { return Palette.Panel; } }
        public override Color MenuItemSelected { get { return Palette.Button; } }
        public override Color MenuItemSelectedGradientBegin { get { return Palette.Button; } }
        public override Color MenuItemSelectedGradientEnd { get { return Palette.Button; } }
        public override Color MenuItemPressedGradientBegin { get { return Palette.Button; } }
        public override Color MenuItemPressedGradientEnd { get { return Palette.Button; } }
        public override Color MenuItemBorder { get { return Palette.Line; } }
        public override Color MenuBorder { get { return Palette.Line; } }
        public override Color SeparatorDark { get { return Palette.Line; } }
        public override Color SeparatorLight { get { return Palette.Panel; } }
        public override Color CheckBackground { get { return Palette.Button; } }
        public override Color CheckSelectedBackground { get { return Palette.Button; } }
        public override Color CheckPressedBackground { get { return Palette.Button; } }
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColors()) { }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle box = e.ImageRectangle;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Palette.Blue, 2F))
            {
                e.Graphics.DrawLines(pen, new Point[] {
                    new Point(box.Left + box.Width * 2 / 10, box.Top + box.Height * 5 / 10),
                    new Point(box.Left + box.Width * 4 / 10, box.Top + box.Height * 7 / 10),
                    new Point(box.Left + box.Width * 8 / 10, box.Top + box.Height * 3 / 10)
                });
            }
        }
    }

}
