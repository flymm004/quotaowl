# GitHub publication checklist

## Repository identity

- Product: **QuotaOwl**.
- Proposed repository name: **quotaowl**; reuse the existing repository and preserve its history.
- Current remote: `https://github.com/flymm004/codex-usage-monitor.git`.
- After an approved GitHub rename: `https://github.com/flymm004/quotaowl.git`.
- Description: **Windows system tray monitor for Codex and Claude subscription usage. One number shows your lowest remaining quota.**
- Suggested topics: `quotaowl`, `windows`, `system-tray`, `codex`, `claude`, `claude-code`, `usage-monitor`, `quota-monitor`, `ai-tools`, `csharp`, `winforms`.

README links currently use the existing public repository. Update them to the canonical `quotaowl` URL after the rename. GitHub redirects the old repository address; do not recreate a repository under the old name.

## Before publishing

1. Accept the local preview, including the one-number tray behavior and both account panels.
2. Rename the existing GitHub repository when authorized; update `origin`, README links, repository description, and topics.
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

The repository has been prepared for publication; this document is not evidence that a remote rename, push, or Release publication has occurred. The old mutex and language-preference path intentionally remain compatible with earlier versions.
