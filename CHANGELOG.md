# Changelog

Notable changes are recorded here for each release.

## Unreleased

- Clarify unavailable Claude reset credits with an official Usage page link instead of leaving the reset section absent.
- Rename the local preview to QuotaOwl with a multiresolution owl application icon.
- Read real Claude subscription usage through the existing local Claude Code OAuth sign-in.
- Show one tray number for the lowest remaining percentage across both providers' 5-hour and weekly limits, colored by its provider, with four-value hover details and a shared compact panel.
- Keep provider reads independent; transient failures preserve clearly marked last successful readings, while sign-out and unsupported provider changes clear old subscription data.
- Preserve eight languages, high-DPI text rendering, and the existing language preference.
- Prefer the newest versioned Codex desktop runtime over a stale unversioned desktop binary, including when the desktop root is on PATH.
- Show an unknown reset count separately from an explicit zero in all eight UI languages.

## 0.1.0-alpha.1

- Initial public preview of the Windows notification-area usage viewer.
- Added eight UI languages and high-DPI support.
- Reads account rate limits through the local Codex App Server.
