Codex Usage Monitor is now **QuotaOwl**, a Windows system tray monitor for **Codex and Claude subscription usage**.

## Download

[Download Windows ZIP / 下载 Windows 版本](https://github.com/flymm004/quotaowl/releases/download/v0.2.0-alpha.3/QuotaOwl-0.2.0-alpha.3-win-x64.zip)

Download **QuotaOwl-0.2.0-alpha.3-win-x64.zip** from Assets below, extract it, and run **QuotaOwl.exe**. Keep **QuotaOwl.exe.config** beside the EXE. Requires Windows 10/11 x64 and .NET Framework 4.8. No installer or administrator privileges needed.

## See it in action

### One number in the tray

<img src="https://raw.githubusercontent.com/flymm004/quotaowl/v0.2.0-alpha.3/docs/screenshots/tray-badge-sample.png" width="32" alt="Blue tray badge showing 44 percent remaining">

**44 = the lowest remaining quota.** Blue means Codex, orange means Claude. Hover for all four values; click to open the panel below.

### Both services in one panel

<img src="https://raw.githubusercontent.com/flymm004/quotaowl/v0.2.0-alpha.3/docs/screenshots/usage-panel-en.png" width="352" alt="QuotaOwl panel showing both services, quota windows, and reset times">

View both services' 5-hour and weekly limits, reset times, and update status. Switch language with the globe button or refresh manually.

*The UI image uses sample data, not a real account.*

## What's new

- One tray number for the lowest remaining percentage across Codex and Claude's 5-hour and weekly quota windows.
- Blue for Codex, orange for Claude. Hover for all four values; click to open the shared panel.
- Real Claude subscription usage through an existing Claude Code sign-in.
- Codex reset-credit reading and improved discovery of the current local Codex runtime.
- Explicit outdated-data markers on transient failures, without inventing zero values.
- Owl branding, a multiresolution icon, and native high-DPI rendering.
- Interface languages: English (default), Simplified Chinese, Traditional Chinese, Japanese, Korean, Spanish, German, and French.

## Before running

Exit an older monitor from its tray menu first. Codex needs an existing ChatGPT subscription sign-in; Claude needs an official subscription sign-in through `claude auth login`. Third-party/API-key usage is not supported. Either service can be monitored independently.

## Preview limitations

Claude's usage endpoint is an internal client interface and may change. QuotaOwl does not renew expired Claude credentials or automatically read Claude reset grants; the panel links to the official Usage page. Only overall Claude quota windows are shown. This is a prerelease, unsigned community utility, unaffiliated with OpenAI or Anthropic.

The app makes no model requests and does not save tokens, raw responses, or quota history.
