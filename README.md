<div align="center">

# 🌀 FanPulse

**Lightweight fan control for MSI motherboards. Set it, close it, forget it.**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%20x64-0078D6)](#requirements)

[Türkçe README](README.tr.md)

</div>

---

MSI Center ships background services, telemetry and hundreds of megabytes of runtime just to spin your fans. FanPulse replaces all of that with a **single portable executable** that writes your fan settings directly to the motherboard's Nuvoton NCT6687D chip — then gets out of the way.

## Features

- 🎯 **Set & forget** — fixed speeds are written straight to the Super I/O chip. Close the app: zero processes remain, and the hardware keeps your values until reboot.
- 📈 **Temperature curves** — a draggable multi-point curve editor per fan. Only when a curve is active does a tiny tray service (~30 MB RAM) stay behind to drive it.
- 📊 **Live dashboard** — CPU / GPU / VRM / chipset temperatures, a rolling 60-second chart and per-fan RPM, refreshed every 2 seconds.
- 🔁 **Reboot persistence** — one toggle registers a Task Scheduler entry that silently re-applies your profile at logon in about a second, then exits.
- 🎮 **GPU fans included** — NVIDIA fan channels appear right next to motherboard headers.
- 🛡️ **Safety floor** — configurable minimum speed (default 20%); 0% must be explicitly allowed per fan.
- 🌍 **English & Turkish UI** — switchable in Settings.

## Download

Grab the latest single-file `FanPulse.exe` from **[Releases](https://github.com/AlparslanBurhan/FanPulse/releases)** — no installation, no runtime required.

## Requirements

- Windows 10/11 x64
- [PawnIO driver](https://pawnio.eu/) — the signed, HVCI-compatible kernel driver used for chip access (successor to the now-blocked WinRing0)
- Administrator rights (ring-0 hardware access)
- An MSI board with the Nuvoton NCT6687D Super I/O — e.g. PRO X870-P WIFI, MAG X870(E) TOMAHAWK, B850 / Z890 series

## Quick start

1. Install PawnIO (one time).
2. Run `FanPulse.exe` and approve the UAC prompt.
3. Select a fan → choose **BIOS / Fixed / Curve** → adjust → **Save & Apply**.
4. Close the window and move on with your life.

| Command | Effect |
|---|---|
| `FanPulse.exe` | Open the UI |
| `FanPulse.exe --startup` | Apply saved profile and exit (or start the tray service if curves exist) |
| `FanPulse.exe --stop` | Stop the tray service |

## How does it persist after exit?

LibreHardwareMonitor normally restores BIOS fan defaults on shutdown (`Computer.Close()`). FanPulse intentionally skips that restore after writing fan controls, so the chip keeps running with the last written values. A reboot resets the chip — which is exactly what the optional startup task is for.

## Building from source

```
dotnet publish src/FanPulse.App/FanPulse.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Credits

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) — sensor access & NCT6687D support
- [PawnIO](https://pawnio.eu/) — safe kernel driver
- [ScottPlot](https://scottplot.net/) — charts and the curve editor canvas

## License

[MIT](LICENSE)
