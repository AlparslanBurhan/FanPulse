# FanPulse

🇬🇧 English | [🇹🇷 Türkçe](#-türkçe)

**A featherweight fan control app for MSI motherboards — set it, close it, forget it.**

FanPulse talks directly to the Nuvoton NCT6687D Super I/O chip on modern MSI boards (X870 / B850 / Z890 and friends) through [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor). No MSI Center, no telemetry, no permanent background services.

## Features

- **Set & forget** — fixed fan speeds are written straight to the chip. Close the app and *nothing* stays running; the hardware keeps your values until reboot.
- **Temperature curves** — draggable multi-point curve editor per fan. When a curve is active, a tiny tray-icon service (~30 MB RAM) keeps it running.
- **Live dashboard** — real-time temperatures (CPU, GPU, VRM, chipset…), a rolling 60-second chart, and per-fan RPM readouts refreshed every 2 seconds.
- **Apply at startup** — one toggle creates a Task Scheduler entry that re-applies your settings after reboot (silently, in ~1 second) and exits.
- **GPU fans too** — NVIDIA fan control channels are exposed alongside motherboard headers.
- **Safety floor** — a configurable minimum speed (default 20%); 0% must be explicitly allowed per fan.
- **Bilingual UI** — Turkish (default) and English.
- Single portable exe, dark themed, no installation.

## Requirements

| Requirement | Why |
|---|---|
| Windows 10/11 x64 | WPF app |
| [PawnIO driver](https://pawnio.eu/) | Signed, HVCI-compatible kernel driver used for chip access (the safe successor to WinRing0) |
| Administrator rights | Ring-0 hardware access requires elevation |
| A supported MSI board (NCT6687D) | e.g. PRO X870-P WIFI, MAG X870 TOMAHAWK, B850/Z890 series |

## Usage

1. Install PawnIO (one time).
2. Run `FanPulse.exe`, approve the UAC prompt.
3. Pick a fan → choose **BIOS / Fixed / Curve** → adjust → **Save & Apply**.
4. Close the window. Fixed speeds persist with zero background processes; curves keep a tray-icon micro service alive.
5. Optional: enable **Apply at Windows startup** in Settings to survive reboots.

Command line: `FanPulse.exe --startup` (apply config and exit / start tray service), `FanPulse.exe --stop` (stop the tray service).

## Building

```
dotnet publish src/FanPulse.App/FanPulse.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## How "settings survive app exit" works

LibreHardwareMonitor normally restores BIOS fan defaults when it shuts down (`Computer.Close()`). FanPulse deliberately skips that restore step after writing fan controls — the chip keeps running with the last written values until the next reboot, and the startup task re-applies them after that.

---

# 🇹🇷 Türkçe

**MSI anakartlar için tüy siklet fan kontrol uygulaması — ayarla, kapat, unut.**

FanPulse, modern MSI kartlardaki (X870 / B850 / Z890 vb.) Nuvoton NCT6687D Super I/O çipiyle [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) üzerinden doğrudan konuşur. MSI Center yok, telemetri yok, kalıcı arka plan servisi yok.

## Özellikler

- **Ayarla ve unut** — sabit fan hızları doğrudan çipe yazılır. Uygulamayı kapat; arkada *hiçbir şey* çalışmaz, donanım değerlerini yeniden başlatmaya kadar korur.
- **Sıcaklık eğrileri** — fan başına sürüklenebilir çok noktalı eğri editörü. Eğri aktifken ~30 MB RAM kullanan minik bir tepsi servisi çalışır.
- **Canlı panel** — anlık sıcaklıklar (CPU, GPU, VRM, chipset…), kayan 60 saniyelik grafik ve 2 saniyede bir güncellenen fan RPM değerleri.
- **Açılışta uygula** — tek bir anahtar, yeniden başlatma sonrası ayarları ~1 saniyede sessizce uygulayıp kapanan bir Görev Zamanlayıcı kaydı oluşturur.
- **GPU fanları da dahil** — NVIDIA fan kanalları anakart başlıklarının yanında listelenir.
- **Güvenlik tabanı** — ayarlanabilir alt hız sınırı (varsayılan %20); %0 için fan başına açık izin gerekir.
- **İki dilli arayüz** — Türkçe (varsayılan) ve İngilizce.
- Tek taşınabilir exe, koyu tema, kurulum yok.

## Gereksinimler

| Gereksinim | Neden |
|---|---|
| Windows 10/11 x64 | WPF uygulaması |
| [PawnIO sürücüsü](https://pawnio.eu/) | Çip erişimi için imzalı, HVCI uyumlu çekirdek sürücüsü (WinRing0'ın güvenli halefi) |
| Yönetici yetkisi | Ring-0 donanım erişimi yükseltme ister |
| Desteklenen MSI kart (NCT6687D) | örn. PRO X870-P WIFI, MAG X870 TOMAHAWK, B850/Z890 serisi |

## Kullanım

1. PawnIO'yu kur (tek seferlik).
2. `FanPulse.exe`'yi çalıştır, UAC onayı ver.
3. Fan seç → **BIOS / Sabit / Eğri** modunu seç → ayarla → **Kaydet ve Uygula**.
4. Pencereyi kapat. Sabit hızlar sıfır arka plan süreciyle kalıcıdır; eğriler tepsi simgeli mikro servisi çalışır tutar.
5. İsteğe bağlı: yeniden başlatmalara dayanması için Ayarlar'dan **Windows açılışında uygula**'yı aç.

Komut satırı: `FanPulse.exe --startup` (ayarları uygula ve çık / tepsi servisini başlat), `FanPulse.exe --stop` (tepsi servisini durdur).

## "Ayarlar uygulama kapanınca nasıl kalıyor?"

LibreHardwareMonitor normalde kapanırken (`Computer.Close()`) BIOS fan varsayılanlarını geri yükler. FanPulse, fan kontrolü yazdığı oturumlarda bu geri yükleme adımını bilinçli olarak atlar — çip, bir sonraki yeniden başlatmaya kadar son yazılan değerlerle çalışmaya devam eder; sonrasını da açılış görevi halleder.

## Lisans

MIT — bkz. [LICENSE](LICENSE).
