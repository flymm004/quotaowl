using System;
using System.Globalization;

namespace QuotaOwl
{
    internal static class RefreshPolicyTests
    {
        private static int checks;
        private static void Check(bool condition, string name)
        {
            if (!condition) throw new Exception(name);
            checks++;
        }
        private static void Main()
        {
            // Simulated time and error responses only: no credentials or network calls.
            DateTime now = new DateTime(2026, 10, 11, 8, 0, 0, DateTimeKind.Utc);
            ProviderState claude = new ProviderState("Claude", Palette.Orange);
            Check(claude.CanRead(now, 1), "Initial Claude read");
            claude.NextReadUtc = now.AddMinutes(10);
            claude.CredentialRevision = 1;
            Check(!claude.CanRead(now.AddMinutes(9), 1), "Manual refresh obeys 10-minute interval");
            Check(claude.CanRead(now.AddMinutes(10), 1), "Read at deadline");
            claude.Reading = true;
            Check(!claude.CanRead(now.AddHours(1), 1), "No overlapping requests");
            claude.Reading = false;
            claude.Error = new UsageReadException("errorClaudeLogin");
            Check(!claude.CanRead(now, 1), "Unchanged invalid credential waits");
            Check(claude.CanRead(now, 2), "New sign-in can retry immediately");
            claude.Error = new UsageReadException("errorClaudeBusy");
            Check(!claude.CanRead(now, 2), "New credential does not bypass 429 cooldown");
            foreach (int minutes in new[] { 10, 20, 40, 60, 60 }) {
                claude.CompleteRead(claude.Error, now);
                Check(claude.NextReadUtc == now.AddMinutes(minutes), "Progressive 429 backoff " + minutes);
                Check(!claude.CanRead(claude.NextReadUtc.AddMilliseconds(-1), 1), "Cooldown stays closed");
                Check(claude.CanRead(claude.NextReadUtc, 1), "Cooldown opens at deadline");
            }
            claude.CompleteRead(new UsageReadException("errorClaudeBusy", retryAfter: TimeSpan.FromHours(2)), now);
            Check(claude.NextReadUtc == now.AddHours(2), "Long Retry-After honored");
            claude.CompleteRead(null, now);
            claude.CompleteRead(new UsageReadException("errorClaudeBusy", retryAfter: TimeSpan.FromSeconds(1)), now);
            Check(claude.NextReadUtc == now.AddMinutes(10), "Success resets backoff; short header cannot reduce minimum");
            ProviderState codex = new ProviderState("Codex", Palette.Blue) { NextReadUtc = now.AddHours(2) };
            Check(codex.CanRead(now, 1), "Claude cooldown does not block Codex");
            DateTimeOffset instant = new DateTimeOffset(now);
            Check(ClaudeReader.CredentialExpired(instant.AddMinutes(-1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture), instant), "Expired credential detected locally");
            Check(ClaudeReader.CredentialExpired(instant.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture), instant), "Exact expiry is expired");
            Check(!ClaudeReader.CredentialExpired(instant.AddMinutes(1).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture), instant), "Valid credential passes preflight");
            Check(!ClaudeReader.CredentialExpired(null, instant), "Missing expiry is not assumed expired");
            Check(ClaudeReader.ParseRetryAfter("120", instant) == TimeSpan.FromMinutes(2), "Retry-After seconds");
            Check(ClaudeReader.ParseRetryAfter(now.AddHours(1).ToString("R", CultureInfo.InvariantCulture), instant) == TimeSpan.FromHours(1), "Retry-After HTTP date");
            foreach (string header in new[] { null, "", "0", "-1", "invalid", "99999999999999999999999", now.AddHours(-1).ToString("R", CultureInfo.InvariantCulture) })
                Check(!ClaudeReader.ParseRetryAfter(header, instant).HasValue, "Invalid/zero/past Retry-After falls back");
            Console.WriteLine("Passed " + checks + " refresh policy checks (no network).");
        }
    }
}
