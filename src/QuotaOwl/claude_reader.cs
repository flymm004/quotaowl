using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace QuotaOwl
{
    internal static class ClaudeReader
    {
        public static UsageResult Read()
        {
            string root = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
            if (String.IsNullOrWhiteSpace(root)) root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude");
            Dictionary<string, object> settings = LoadJson(Path.Combine(root, "settings.json"), false);
            Dictionary<string, object> configuredEnv = Object(settings, "env");
            ValidateProvider(configuredEnv, Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL"),
                !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN")) ||
                !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")));
            Dictionary<string, object> credentials = LoadJson(Path.Combine(root, ".credentials.json"), true);
            Dictionary<string, object> oauth = Object(credentials, "claudeAiOauth");
            string token = StringValue(oauth, "accessToken");
            if (String.IsNullOrWhiteSpace(token)) throw new UsageReadException("errorClaudeLogin");

            // The Claude client uses this internal read-only endpoint. Do not follow redirects
            // with credentials or send tokens to a custom provider configured by CC Switch.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.anthropic.com/api/oauth/usage");
            request.Method = "GET";
            request.AllowAutoRedirect = false;
            request.Timeout = 15000;
            request.ReadWriteTimeout = 15000;
            request.UserAgent = "QuotaOwl/0.2.0";
            request.Accept = "application/json";
            request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
            request.Headers["anthropic-beta"] = "oauth-2025-04-20";
            try {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream())) {
                    if (response.StatusCode != HttpStatusCode.OK) throw new UsageReadException("errorClaude");
                    UsageResult result = ParseUsage(reader.ReadToEnd());
                    string plan = StringValue(oauth, "subscriptionType");
                    result.PlanType = String.IsNullOrWhiteSpace(plan) ? "Claude.ai" : plan;
                    return result;
                }
            } catch (WebException ex) {
                using (HttpWebResponse response = ex.Response as HttpWebResponse) {
                    if (response != null) {
                        int status = (int)response.StatusCode;
                        if (status == 401) throw new UsageReadException("errorClaudeLogin");
                        if (status == 403) throw new UsageReadException("errorClaudeSubscription");
                        if (status == 429) throw new UsageReadException("errorClaudeBusy");
                    }
                }
                throw new UsageReadException("errorClaude");
            }
        }

        internal static void ValidateProvider(Dictionary<string, object> settingsEnv, string processBaseUrl, bool processHasApiCredentials)
        {
            string configured = StringValue(settingsEnv, "ANTHROPIC_BASE_URL");
            foreach (string baseUrl in new[] { configured, processBaseUrl }) {
                if (String.IsNullOrWhiteSpace(baseUrl)) continue;
                Uri uri;
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uri) ||
                    uri.Scheme != Uri.UriSchemeHttps || !String.Equals(uri.Host, "api.anthropic.com", StringComparison.OrdinalIgnoreCase))
                    throw new UsageReadException("errorClaudeProvider");
            }
            if (processHasApiCredentials || !String.IsNullOrWhiteSpace(StringValue(settingsEnv, "ANTHROPIC_AUTH_TOKEN")) ||
                !String.IsNullOrWhiteSpace(StringValue(settingsEnv, "ANTHROPIC_API_KEY"))) throw new UsageReadException("errorClaudeProvider");
        }

        internal static UsageResult ParseUsage(string json)
        {
            Dictionary<string, object> data;
            try { data = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>; }
            catch (ArgumentException) { throw new UsageReadException("errorClaude"); }
            UsageResult result = new UsageResult();
            AddWindow(result, Object(data, "five_hour"), "primary", 300);
            AddWindow(result, Object(data, "seven_day"), "secondary", 10080);
            if (result.Rows.Count == 0) throw new UsageReadException("errorClaudeSubscription");
            return result;
        }
        private static void AddWindow(UsageResult result, Dictionary<string, object> window, string period, int minutes)
        {
            object usage;
            double used;
            if (window == null || !window.TryGetValue("utilization", out usage) || usage == null ||
                !Double.TryParse(Convert.ToString(usage, CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture, out used) ||
                Double.IsNaN(used) || Double.IsInfinity(used) || used < 0 || used > 100) return;
            DateTimeOffset reset;
            long resetsAt = DateTimeOffset.TryParse(StringValue(window, "resets_at"), CultureInfo.InvariantCulture, DateTimeStyles.None, out reset) ? reset.ToUnixTimeSeconds() : 0;
            result.Rows.Add(new UsageRow { LimitId = "claude", Period = period, WindowMinutes = minutes,
                UsedPercent = (int)Math.Ceiling(used), RemainingPercent = (int)Math.Floor(100 - used), ResetsAt = resetsAt });
        }
        private static Dictionary<string, object> LoadJson(string path, bool required)
        {
            try {
                if (!File.Exists(path)) { if (required) throw new UsageReadException("errorClaudeLogin"); return null; }
                return new JavaScriptSerializer().DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
            } catch (IOException) { throw new UsageReadException("errorClaude"); }
            catch (UnauthorizedAccessException) { throw new UsageReadException("errorClaude"); }
            catch (ArgumentException) { throw new UsageReadException("errorClaude"); }
        }
        private static Dictionary<string, object> Object(Dictionary<string, object> data, string key)
        { object value; return data != null && data.TryGetValue(key, out value) ? value as Dictionary<string, object> : null; }
        private static string StringValue(Dictionary<string, object> data, string key)
        { object value; return data != null && data.TryGetValue(key, out value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null; }
    }
}
