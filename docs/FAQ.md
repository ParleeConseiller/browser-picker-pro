# FAQ — Browser Picker Pro

*Created by Parlee Conseiller, Inc.*

---

**Q: Does Browser Picker Pro require administrator privileges?**

No. All registry writes go to `HKCU` (current user). No UAC prompt is ever needed.

---

**Q: Why do I still need to manually set it in Default Apps?**

Windows 11 requires the final default-browser step to be performed by the user in Settings to prevent malware from silently hijacking browser associations. Browser Picker Pro prepares all the necessary registry entries; you just need to approve the association once.

---

**Q: Will it work with browsers I install in the future?**

Yes. Click **Refresh** in the Browsers section and any newly installed browser will be detected automatically.

---

**Q: My browser is listed but not appearing in the picker.**

Check that it is **enabled** in the Browsers section (the toggle must be ON).

---

**Q: Can I use it without any rules?**

Yes. The picker will always appear for every link, letting you choose manually every time.

---

**Q: Does it send my URLs anywhere?**

Only if security checks are enabled and you've configured API keys. In that case, the URL is sent to the configured providers (VirusTotal, Google Safe Browsing). Otherwise, nothing leaves your machine.

---

**Q: What happens if a security provider is unavailable?**

Browser Picker Pro never blocks the picker due to provider failures. If a provider is down or errors, its result is marked "Unknown" and the picker appears normally.

---

**Q: Where are my settings stored?**

`%AppData%\BrowserPickerPro\` — readable JSON files. You can edit them manually or use Import/Export.

---

**Q: How do I uninstall it?**

1. Go to **Windows Setup** → click **Unregister**
2. Delete the application folder
3. Optionally: delete `%AppData%\BrowserPickerPro\`
