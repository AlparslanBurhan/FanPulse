<div align="center">

# 🌀 FanPulse

**MSI anakartlar için hafif fan kontrolü. Ayarla, kapat, unut.**

[![Lisans: MIT](https://img.shields.io/badge/Lisans-MIT-blue.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%20x64-0078D6)](#gereksinimler)

[English README](README.md)

</div>

---

MSI Center; fanlarını döndürmek için arka plan servisleri, telemetri ve yüzlerce megabayt yük taşır. FanPulse bunların hepsinin yerine, fan ayarlarını doğrudan anakarttaki Nuvoton NCT6687D çipine yazan **tek bir taşınabilir exe** koyar — sonra da aradan çekilir.

## Özellikler

- 🎯 **Ayarla ve unut** — sabit hızlar doğrudan Super I/O çipine yazılır. Uygulamayı kapat: arkada sıfır süreç kalır, donanım değerlerini yeniden başlatmaya kadar korur.
- 📈 **Sıcaklık eğrileri** — fan başına sürüklenebilir çok noktalı eğri editörü. Yalnızca eğri aktifken ~30 MB RAM'lik minik bir tepsi servisi arkada kalır.
- 📊 **Canlı panel** — CPU / GPU / VRM / chipset sıcaklıkları, kayan 60 saniyelik grafik ve fan başına RPM; 2 saniyede bir güncellenir.
- 🔁 **Yeniden başlatmaya dayanıklılık** — tek bir anahtar, oturum açılışında profilini ~1 saniyede sessizce uygulayıp kapanan bir Görev Zamanlayıcı kaydı oluşturur.
- 🎮 **GPU fanları dahil** — NVIDIA fan kanalları anakart başlıklarının hemen yanında listelenir.
- 🛡️ **Güvenlik tabanı** — ayarlanabilir alt hız sınırı (varsayılan %20); %0 için fan başına açık izin gerekir.
- 🌍 **İngilizce ve Türkçe arayüz** — Ayarlar'dan değiştirilebilir.

## İndir

Tek dosyalık güncel `FanPulse.exe`'yi **[Releases](https://github.com/AlparslanBurhan/FanPulse/releases)** sayfasından al — kurulum yok, runtime gerekmez.

## Gereksinimler

- Windows 10/11 x64
- [PawnIO sürücüsü](https://pawnio.eu/) — çip erişimi için kullanılan imzalı, HVCI uyumlu çekirdek sürücüsü (artık engellenen WinRing0'ın halefi)
- Yönetici yetkisi (ring-0 donanım erişimi)
- Nuvoton NCT6687D'li bir MSI kart — örn. PRO X870-P WIFI, MAG X870(E) TOMAHAWK, B850 / Z890 serisi

## Hızlı başlangıç

1. PawnIO'yu kur (tek seferlik).
2. `FanPulse.exe`'yi çalıştır, UAC onayı ver.
3. Fan seç → **BIOS / Sabit / Eğri** → ayarla → **Kaydet ve Uygula**.
4. Pencereyi kapat, hayatına devam et.

| Komut | Etki |
|---|---|
| `FanPulse.exe` | Arayüzü açar |
| `FanPulse.exe --startup` | Kayıtlı profili uygular ve çıkar (eğri varsa tepsi servisini başlatır) |
| `FanPulse.exe --stop` | Tepsi servisini durdurur |

## Uygulama kapanınca ayarlar nasıl kalıyor?

LibreHardwareMonitor normalde kapanırken (`Computer.Close()`) BIOS fan varsayılanlarını geri yükler. FanPulse, fan kontrolü yazdıktan sonra bu geri yükleme adımını bilinçli olarak atlar; çip son yazılan değerlerle çalışmaya devam eder. Yeniden başlatma çipi sıfırlar — isteğe bağlı açılış görevi tam olarak bunun içindir.

## Kaynaktan derleme

```
dotnet publish src/FanPulse.App/FanPulse.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Teşekkürler

- [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor) — sensör erişimi ve NCT6687D desteği
- [PawnIO](https://pawnio.eu/) — güvenli çekirdek sürücüsü
- [ScottPlot](https://scottplot.net/) — grafikler ve eğri editörü tuvali

## Lisans

[MIT](LICENSE)
