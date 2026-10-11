# Changelog

Notable changes are recorded here for each release.

## 0.2.0-alpha.4 — 2026-10-11

- Check Claude usage at most once every 10 minutes, with the same interval for automatic and manual refresh. Codex retains its independent 2-minute timer.
- Back off after HTTP 429 for 10, 20, 40, then 60 minutes, respecting a longer server `Retry-After`. Show the next retry time and retain clearly marked outdated readings.
- Detect expired Claude credentials before sending a request. Allow immediate retry after the credential file changes following a sign-in error.
- Update refresh hints, sign-in errors, and Info text in all eight UI languages.

## 0.2.0-alpha.3 — 2026-10-10

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
