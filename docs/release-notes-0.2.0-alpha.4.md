QuotaOwl is a Windows system tray monitor for Codex and Claude subscription usage. This preview reduces Claude polling and handles rate limits explicitly.

## Download

[Download Windows ZIP](https://github.com/flymm004/quotaowl/releases/download/v0.2.0-alpha.4/QuotaOwl-0.2.0-alpha.4-win-x64.zip)

Extract the entire ZIP and run **QuotaOwl.exe**. Keep **QuotaOwl.exe.config** beside it. Requires Windows 10/11 x64 and .NET Framework 4.8; no installer or administrator privileges needed. Exit the previous QuotaOwl version from its tray menu before launching the update.

## What's changed

- **Claude: at most one usage query every 10 minutes**, including manual refresh. Codex continues to refresh independently every 2 minutes.
- **Automatic cooldown after HTTP 429:** wait 10, 20, 40, then 60 minutes after consecutive rate-limit responses. A longer server `Retry-After` takes priority. Successful reads restore the normal interval.
- **Clear retry time:** the Claude section shows when the next rate-limit retry is scheduled. Available readings stay visible as outdated. Hover over Refresh to see the next scheduled Claude check.
- **Expired sign-in is handled separately:** detect an expired local credential before making a request. After `claude auth login`, a changed credential file permits an immediate retry following a sign-in error.
- Updated error messages, refresh hints, and Info text in all eight interface languages.

Reducing requests cannot guarantee availability of Claude's internal usage endpoint. A failed quota query does not by itself mean your subscription quota is exhausted.

## See it in action

### One number in the tray

<img src="https://raw.githubusercontent.com/flymm004/quotaowl/v0.2.0-alpha.4/docs/screenshots/tray-badge-sample.png" width="32" alt="Blue tray badge showing 44 percent remaining">

**44 = the lowest remaining quota.** Blue means Codex, orange means Claude. Hover for all four values; click for both services' details.

### Both services in one panel

<img src="https://raw.githubusercontent.com/flymm004/quotaowl/v0.2.0-alpha.4/docs/screenshots/claude-cooldown-sample.png" width="352" alt="QuotaOwl showing Claude's outdated readings and a scheduled rate-limit retry while Codex remains available">

View 5-hour and weekly limits, reset times, and update status. In this example, Claude is rate limited: its previous reading is marked outdated and the next retry is scheduled for 15:10. Codex continues to update. Use the globe button to change language. UI images use sample data, not a real account.

**Interface languages:** English, Simplified Chinese, Traditional Chinese, Japanese, Korean, Spanish, German, and French. The app can follow the system language.

## Before running

Codex needs an existing ChatGPT subscription sign-in; Claude needs an official subscription sign-in through `claude auth login`. Either service can be monitored independently. Third-party/API-key usage is not supported.

Claude's usage endpoint is an internal client interface and may change. QuotaOwl does not renew Claude credentials or automatically read Claude reset grants; the panel links to the official Usage page. It makes no model requests and does not save tokens, raw responses, or quota history.

This is an unsigned community prerelease, unaffiliated with OpenAI or Anthropic.
