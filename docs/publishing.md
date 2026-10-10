# GitHub publication checklist

## Repository identity

- Product: **QuotaOwl**.
- Repository name: **quotaowl**; renamed from the existing repository with its history preserved.
- Current remote: `https://github.com/flymm004/quotaowl.git`.
- Previous name: `https://github.com/flymm004/codex-usage-monitor.git`.
- Description: **Windows system tray monitor for Codex and Claude subscription usage. One number shows your lowest remaining quota.**
- Topics: `quotaowl`, `windows`, `system-tray`, `codex`, `codex-cli`, `claude`, `claude-code`, `usage-monitor`, `quota-monitor`, `ai-tools`, `csharp`, `winforms`, `dotnet-framework`, `high-dpi`.

README links and the local remote use the canonical `quotaowl` URL. The description and topics have been updated on GitHub. GitHub redirects the old repository address; do not recreate a repository under the old name.

## Before publishing

1. Accept the local preview, including the one-number tray behavior and both account panels.
2. Repository rename, `origin`, README links, repository description, and topics are prepared. Review that the public description matches the upcoming release.
3. Build `./build.ps1 -Version 0.2.0-alpha.3` and review `git status`.
4. Remove the local-preview notice from both READMEs only when the QuotaOwl Release is publicly downloadable.
5. Commit source and documentation. Generated binaries stay outside Git.
6. Push the branch, then tag `v0.2.0-alpha.3` to produce a **draft prerelease** through GitHub Actions.
7. Use [the prepared release notes](release-notes-0.2.0-alpha.3.md), confirm the ZIP/config/checksum attachments, and publish the draft when authorized.

## Public assets

- Preferred user download: `QuotaOwl-0.2.0-alpha.3-win-x64.zip` (EXE, DPI config, README, license).
- ZIP SHA-256 checksum.
- Optional standalone EXE, its companion config, and checksum; the ZIP is simpler for beginners.
- Current UI images use sample data. Never upload local OAuth files, diagnostic responses, or real-account screenshots.

## Local state

The GitHub repository has been renamed to QuotaOwl and its metadata updated. The new application code and Release have not yet been published. The old mutex and language-preference path intentionally remain compatible with earlier versions.
