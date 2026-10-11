# Contributing

Thanks for helping improve QuotaOwl.

## Before opening an issue

- Include your QuotaOwl version, Windows version, display scaling, and the relevant Codex CLI / Claude Code version.
- Describe the steps to reproduce the problem and the result you expected.
- Remove account emails, authentication data, and live quota values from logs and screenshots.
- Check whether the issue still occurs with the latest release.

## Changes

1. Keep changes focused and explain the user-visible effect.
2. For localization changes, include the locale and check that labels fit at 100% and 200% scaling.
3. Run `./test.ps1` for the offline refresh-policy checks, then build with `./build.ps1 -Version 0.2.0-alpha.4` on Windows.
4. Do not commit generated files from `artifacts/`, local app data, credentials, or screenshots of a real account.
5. Open a pull request with a short summary and the build result.

There are no external package dependencies. The CI workflow checks the refresh policy, then compiles and packages the app on Windows. Tests use simulated time and responses, without credentials or network calls.

Source is under `src/QuotaOwl/`; sample UI images and release notes are under `docs/`. See [Publishing](docs/publishing.md) for the repository description, topics, and release steps. The old single-instance mutex and language-preference location remain intentionally compatible with earlier versions.
