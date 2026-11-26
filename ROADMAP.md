<!--
  File: ROADMAP.md
  Description: Planned and delivered milestones of Browser Picker Pro.
  Author: Jose-Jorge HERNANDEZ
  Company: Parlee Conseiller, Inc.
  Date: 2026-06-12
  Last edit date: 2026-11-25
  Version: 1.1.0
-->
# Roadmap — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

## v1.0 — Initial Release ✅

- Browser Selection Mode with animated cards
- Configuration Dashboard (Browsers, Rules, Security, Appearance, Import/Export, Setup, About)
- Rules Engine: domain, wildcard, regex, exact
- Browser Detection: 13+ known browsers + registry fallback
- Multi-profile support (Chromium + Firefox)
- Security providers: VirusTotal, Google Safe Browsing
- Windows protocol/file association registration
- Dark/Light/System theme with Mica effect
- Import/Export JSON configuration

---

## v1.1 — Picker Redesign ✅

- [x] Glassmorphism picker window (transparent, custom background layers, glass tint)
- [x] Drag-to-reorder browser cards (native WPF DragDrop, persisted order)
- [x] Staggered entrance animations (cards slide in one by one)
- [x] Premium 3D card hover effect (scale, skew tilt, specular shine, ElasticEase spring-back)
- [x] Customizable picker: Modal vs Fullscreen, WrapGrid vs Single Row
- [x] Custom background support (image, GIF, MP4, WebM with loop)
- [x] Dark tint and card size sliders in Appearance settings
- [x] Modern pill scrollbar in picker (replaces ActiveX-style default)

---

## v2.0 — Instant Picker ✅ (2026-11-25)

- [x] System tray icon with quick actions (Settings, Refresh browsers, Start with Windows, Exit)
- [x] Resident app + NativeAOT launcher: click → picker in ~50–80 ms (target was <300 ms)
- [x] Opening animations (circle reveal, zoom & focus, spring rise, CRT power-on, random) with preview
- [x] Start with Windows
- [x] "Remember for this domain" actually saves a rule
- [x] Installer ships the launcher (WiX source updated; MSI build not yet automated)

## v2.1 — Polish

- [ ] Global hotkey to open picker manually
- [ ] Recent URLs history in picker
- [ ] Browser profiles shown as sub-cards in picker
- [ ] Opening animation for the configuration dashboard
- [ ] Build the MSI in the release workflow

## v1.3 — Power User Features

- [ ] Rule import from popular tools (BrowserSelector, etc.)
- [ ] Per-rule keyboard shortcuts
- [ ] "Open in all browsers" option
- [ ] URL preview / unfurl in picker
- [ ] Additional security providers: IPQualityScore, URLVoid, Norton SafeWeb
- [ ] Custom browser icon support

## v1.4 — Enterprise Features

- [ ] Group Policy / MDM configuration support
- [ ] Silent/headless mode with rules-only
- [ ] Centralized rule distribution via HTTP
- [ ] Audit log of all URL routing decisions

## v2.0 — Next Generation (Long Term)

- [ ] Windows App SDK migration (full WinUI 3)
- [ ] ARM64 support
- [ ] Plugin API for custom browser providers
- [ ] Browser extension for in-browser rule management

---

*Priorities may shift based on community feedback. Open an issue to request a feature.*
