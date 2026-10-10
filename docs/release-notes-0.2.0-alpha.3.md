# QuotaOwl 0.2.0-alpha.3 🦉

Codex Usage Monitor is becoming **QuotaOwl**, a Windows system tray monitor for **Codex and Claude subscription usage**.

## Download

Download **QuotaOwl-0.2.0-alpha.3-win-x64.zip** from Assets below, extract it, and run **QuotaOwl.exe**. Keep **QuotaOwl.exe.config** beside the EXE. Requires Windows 10/11 x64 and .NET Framework 4.8. No installer or administrator privileges needed.

**中文：**在下方 Assets 下载 **QuotaOwl-0.2.0-alpha.3-win-x64.zip**，全部解压后双击 **QuotaOwl.exe**。保留同目录配置文件，无需安装。不要下载 Source code 源码包。

## What's new

- One tray number for the lowest remaining percentage across Codex and Claude's 5-hour and weekly quota windows.
- Blue for Codex, orange for Claude. Hover for all four values; click to open the shared panel.
- Real Claude subscription usage through an existing Claude Code sign-in.
- Codex reset-credit reading and improved discovery of the current local Codex runtime.
- Explicit outdated-data markers on transient failures, without inventing zero values.
- Owl branding, a multiresolution icon, eight languages, and native high-DPI rendering.

## Before running

Exit an older monitor from its tray menu first. Codex needs an existing ChatGPT subscription sign-in; Claude needs an official subscription sign-in through `claude auth login`. Third-party/API-key usage is not supported. Either service can be monitored independently.

## Preview limitations

Claude's usage endpoint is an internal client interface and may change. QuotaOwl does not renew expired Claude credentials or automatically read Claude reset grants; the panel links to the official Usage page. Only overall Claude quota windows are shown. This is a prerelease, unsigned community utility, unaffiliated with OpenAI or Anthropic.

The app makes no model requests and does not save tokens, raw responses, or quota history.
