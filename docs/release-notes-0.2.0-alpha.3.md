Codex Usage Monitor is now **QuotaOwl**, a Windows system tray monitor for **Codex and Claude subscription usage**.

## Download

[Download Windows ZIP / 下载 Windows 版本](https://github.com/flymm004/quotaowl/releases/download/v0.2.0-alpha.3/QuotaOwl-0.2.0-alpha.3-win-x64.zip)

Download **QuotaOwl-0.2.0-alpha.3-win-x64.zip** from Assets below, extract it, and run **QuotaOwl.exe**. Keep **QuotaOwl.exe.config** beside the EXE. Requires Windows 10/11 x64 and .NET Framework 4.8. No installer or administrator privileges needed.

中文下载：点击上方下载链接，或在下方 Assets 下载 **QuotaOwl-0.2.0-alpha.3-win-x64.zip**，全部解压后双击 **QuotaOwl.exe**。保留同目录配置文件，无需安装。不要下载 Source code 源码包。

## See it in action / 界面预览

### One number in the tray / 托盘只占一个位置

<img src="https://raw.githubusercontent.com/flymm004/quotaowl/v0.2.0-alpha.3/docs/screenshots/tray-badge-sample.png" width="32" alt="Blue tray badge showing 44 percent remaining">

**44 = the lowest remaining quota.** Blue means Codex, orange means Claude. Hover for all four values; click to open the panel below.

44 表示双方额度中最低的剩余百分比；蓝色为 Codex，橙色为 Claude。悬停看数值，点击开详情，无需来回切换终端或浏览器。

### Both services in one panel / 两个服务，一屏查看

<table>
<tr><th>English</th><th>简体中文</th></tr>
<tr>
<td><img src="https://raw.githubusercontent.com/flymm004/quotaowl/v0.2.0-alpha.3/docs/screenshots/usage-panel-en.png" width="320" alt="QuotaOwl English panel with both services and reset times"></td>
<td><img src="https://raw.githubusercontent.com/flymm004/quotaowl/v0.2.0-alpha.3/docs/screenshots/usage-panel-zh-cn.png" width="320" alt="QuotaOwl 中文面板，显示双方额度和重置时间"></td>
</tr>
</table>

View both services' 5-hour and weekly limits, reset times, and update status. Switch language with the globe button or refresh manually.

同时查看双方 5 小时和 7 天额度、重置时间及更新状态；点击地球按钮切换语言，点击刷新获取最新数值。

*UI images use sample data, not a real account. / 界面图片使用示例数据，不包含真实账户数据。*

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
