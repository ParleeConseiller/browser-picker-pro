# API Integrations — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## VirusTotal

### Setup
1. Create a free account at [virustotal.com](https://www.virustotal.com)
2. Go to your profile → API Key
3. Copy the key and paste it into the Security settings in Browser Picker Pro

### Free Tier Limits
- 4 requests per minute
- 500 requests per day

### What it checks
VirusTotal aggregates results from 70+ antivirus engines and security vendors. Browser Picker Pro queries the `/api/v3/urls/{id}` endpoint and considers a URL **Dangerous** if 3+ engines flag it, **Suspicious** if any engines flag it.

---

## Google Safe Browsing

### Setup
1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a project and enable the **Safe Browsing API**
3. Create an API key under Credentials
4. Paste the key into the Security settings

### Free Tier
- 10,000 requests per day (free)

### Threat Types Checked
- Malware
- Social Engineering (phishing)
- Unwanted Software
- Potentially Harmful Applications

---

## Adding a Custom Provider

Implement `ISecurityProvider` from `BrowserPickerPro.Core`:

```csharp
public class MyCustomProvider : SecurityProviderBase
{
    public override string ProviderName => "My Provider";
    public override bool IsConfigured => true;
    public override bool IsEnabled => true;

    public MyCustomProvider(ILogger logger, HttpClient httpClient)
        : base(logger, httpClient) { }

    protected override async Task<ProviderSecurityResult> CheckUrlCoreAsync(
        string url, CancellationToken cancellationToken)
    {
        // Call your API
        return new ProviderSecurityResult
        {
            ProviderName = ProviderName,
            Level = SecurityLevel.Safe,
            Score = 100,
            IsAvailable = true
        };
    }
}
```

Register in DI:
```csharp
services.AddSingleton<ISecurityProvider, MyCustomProvider>();
```

---

## Planned Providers

| Provider | Status |
|---|---|
| VirusTotal | ✅ Implemented |
| Google Safe Browsing | ✅ Implemented |
| IPQualityScore | 🔜 Planned v1.2 |
| URLVoid | 🔜 Planned v1.2 |
| Norton Safe Web | 🔜 Planned v1.2 |
| PhishTank | 🔜 Planned v1.2 |
| OpenPhish | 🔜 Planned v1.2 |
