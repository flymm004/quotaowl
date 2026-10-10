# GitHub publication checklist

## Repository identity

- Product: **QuotaOwl**.
- Repository name: **quotaowl**; renamed from the existing repository with its history preserved.
- Current remote: `https://github.com/flymm004/quotaowl.git`.
- Previous name: `https://github.com/flymm004/codex-usage-monitor.git`.
- Description: **Windows system tray monitor for Codex and Claude subscription usage. One number shows your lowest remaining quota.**
- Topics: `quotaowl`, `windows`, `system-tray`, `codex`, `codex-cli`, `claude`, `claude-code`, `usage-monitor`, `quota-monitor`, `ai-tools`, `csharp`, `winforms`, `dotnet-framework`, `high-dpi`.

README links and the local remote use the canonical `quotaowl` URL. The description and topics have been updated on GitHub. GitHub redirects the old repository address; do not recreate a repository under the old name.

## Release procedure

1. Prepare an accepted local preview and versioned release notes.
2. Keep `origin`, README links, repository description, and topics consistent with QuotaOwl.
3. Update the version, changelog, and direct download links in both READMEs.
4. Build `./build.ps1 -Version 0.2.0-alpha.3` and review `git status`. Generated binaries stay outside Git.
5. Commit and push the branch, then tag `v0.2.0-alpha.3` to produce a **draft prerelease** through GitHub Actions.
6. Confirm the ZIP/config/checksum attachments, use [the release notes](release-notes-0.2.0-alpha.3.md), and publish the draft when authorized.
7. Confirm that the public download works and its SHA-256 matches the release checksum.

## Public assets

- Preferred user download: `QuotaOwl-0.2.0-alpha.3-win-x64.zip` (EXE, DPI config, README, license).
- ZIP SHA-256 checksum.
- Optional standalone EXE, its companion config, and checksum; the ZIP is simpler for beginners.
- Current UI images use sample data. Never upload local OAuth files, diagnostic responses, or real-account screenshots.

## Compatibility

The repository retains the history of Codex Usage Monitor. The old mutex and language-preference path intentionally remain compatible with earlier versions. Release assets for earlier versions keep their original filenames.
