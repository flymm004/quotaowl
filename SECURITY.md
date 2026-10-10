# Security policy

This project is an early-stage Windows utility and does not have a supported release channel for urgent fixes yet.

Please report security issues through GitHub's private vulnerability reporting for this repository when it is enabled. Do not include authentication tokens, account emails, or other private data in a public issue. If private reporting is unavailable, contact the repository owner privately through their GitHub profile.

QuotaOwl starts the locally installed Codex CLI App Server and relies on its existing sign-in. For Claude, it reads the existing Claude Code OAuth credential from local disk into memory and sends it only to the fixed HTTPS Anthropic usage endpoint, with redirects disabled. Custom provider or API key configurations are rejected before sending credentials. Tokens and raw API responses are not logged or copied. The app does not send telemetry or make model requests.

Claude usage depends on an internal client interface that may change. Expired Claude credentials must be renewed through Claude Code. The preview does not implement an independent credential store or OAuth refresh flow.
