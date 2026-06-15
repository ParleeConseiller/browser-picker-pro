# User Guide — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## Getting Started

After installing Browser Picker Pro, run it once to open the Configuration Dashboard. Navigate to **Windows Setup** and follow the three steps to register it as your default browser.

---

## The Browser Picker

When Windows opens a web link (from Outlook, Slack, a PDF, etc.), Browser Picker Pro appears in the center of your screen showing all your installed browsers as glass cards with animated entrance effects.

### Selecting a Browser

| Action | Result |
|---|---|
| Click a browser card | Opens the URL in that browser |
| Press **Enter** | Opens in the currently focused card |
| Press **Escape** | Cancels — does not open the URL |

### Hover Effects

Hovering over a card shows a premium 3D effect: the card scales up, tilts in the direction of your cursor, lifts slightly, and displays a specular light reflection that follows your pointer. Moving the mouse away triggers an elastic spring-back animation.

### Drag to Reorder

Drag any browser card to a new position to change the order. The new order is saved automatically and persists across sessions.

### Remember for this domain

Check **"Remember for this domain"** before clicking a browser to automatically create a domain rule that will use that browser next time a URL from the same domain is opened — no picker shown.

---

## Rules Engine

Rules let you automatically route URLs without showing the picker.

### Rule Types

| Type | Pattern Example | Matches |
|---|---|---|
| **Domain** | `youtube.com` | youtube.com and all subdomains |
| **Wildcard** | `*.google.com*` | Any subdomain and path on google.com |
| **Regex** | `https://.*\.bank\.com` | Complex patterns |
| **Exact** | `https://github.com` | Only this exact URL |

### Priority

Lower number = higher priority. Rules are evaluated from lowest to highest number and the first match wins.

---

## Browser Profiles

If you use Chrome with multiple profiles (Personal, Work, etc.), Browser Picker Pro detects them automatically. When editing a rule, you can target a specific profile, not just a browser.

---

## Security

Security checking is optional and disabled by default. When enabled:

1. Before the picker appears, Browser Picker Pro queries the enabled security providers
2. A banner shows the result: **Safe** ✓, **Suspicious** ⚠, or **Dangerous** ✕
3. If **Block dangerous URLs** is enabled, dangerous URLs will not open at all

See [API_INTEGRATIONS.md](API_INTEGRATIONS.md) for how to get free API keys.

---

## Appearance

Navigate to **Appearance** to change:

**Color Theme**
- System (follows Windows), Light, or Dark

**Window Effects**
- **Mica Effect**: Windows 11 frosted glass background on the dashboard
- **Smooth Animations**: Hover and transition animations throughout the app

**Browser Picker** (toggles)
- Show browser version below the browser name
- Show recent usage indicators (how many times you've used each browser)

**Picker Window Style** (all settings apply next time the picker opens):
- *Window mode*: **Modal** (floating centered window with shadow) or **Fullscreen** overlay
- *Card layout*: **Wrap Grid** (cards wrap to multiple rows) or **Single Row** (horizontal scroll)
- *Background*: **Dark gradient** (default) or **Custom file** — pick any image (JPG, PNG, WebP), animated GIF, or video (MP4, WebM); videos loop automatically
- *Dark tint*: 0–90% opacity black overlay applied on top of the background
- *Card size*: 140–260 px — controls how large each browser card appears

---

## Import / Export

Use **Import/Export** to:
- **Export**: Save all your settings, browsers, and rules to a `.json` file
- **Import**: Restore from a previously exported file
- **Open App Data Folder**: Directly browse your configuration files

Configuration files are stored in `%AppData%\BrowserPickerPro\`.

---

## Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md).
