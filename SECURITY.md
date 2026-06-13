# Security Policy — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## Supported Versions

| Version | Supported |
|---------|-----------|
| 1.0.x   | ✅ Yes    |

## Reporting a Vulnerability

**Do not report security vulnerabilities via GitHub Issues.**

Please email: security@parleeconseiller.com

Include:
- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

We will respond within 48 hours and issue a patch within 14 days for critical issues.

## Security Considerations

### Protocol Registration
Browser Picker Pro registers itself in `HKCU` (user registry) only. No administrator privileges are required or requested. The registration can be fully undone via the Setup screen or `scripts/register-protocols.ps1 -Unregister`.

### API Keys
Security provider API keys (VirusTotal, Google Safe Browsing) are stored in `%AppData%\BrowserPickerPro\settings.json` — a user-writable directory. Keys are stored in plain text. Do not share your `settings.json` file.

### Network Requests
Browser Picker Pro only makes outbound HTTPS requests to:
- `https://www.virustotal.com` (if VirusTotal is enabled)
- `https://safebrowsing.googleapis.com` (if Google Safe Browsing is enabled)

No telemetry. No analytics. No crash reports sent anywhere.

### Launched Processes
The application launches browser processes using `Process.Start` with the URL as an argument. No shell execution (`UseShellExecute = false`). The URL is passed as a quoted argument.
