using FanPulse.Core.Hardware;

namespace FanPulse.Poc;

/// <summary>
/// Aşama 1 doğrulama aracı: sensör keşfi, fan yazma ve KALICILIK testi.
///
/// Test akışı:
///   1. `list`  → fanları ve sıcaklıkları gör
///   2. `set 2 40` → 2 numaralı fana %40 yaz, RPM değişimini `watch` ile izle
///   3. `exit-keep` → uygulama Close ÇAĞIRMADAN kapanır → hız KALMALI
///   4. Yeniden başlatma sonrası → BIOS eğrisine dönmeli
///   5. `exit-restore` → LHM Close ile kapanır → hız hemen BIOS'a dönmeli
/// </summary>
internal static class Program
{
    private static HardwareService? _hardware;
    private static FanController? _controller;
    private static IReadOnlyList<FanChannel> _fans = [];

    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== FanPulse PoC — Donanım Doğrulama Aracı ===\n");

        if (!PawnIo.IsInstalled())
        {
            Console.WriteLine("UYARI: PawnIO sürücüsü kurulu görünmüyor!");
            Console.WriteLine($"Anakart sensörlerine erişim için kurulum gerekli: {PawnIo.DownloadUrl}");
            Console.WriteLine("Yine de devam ediliyor (CPU/GPU sensörleri kısmen çalışabilir)...\n");
        }

        Console.WriteLine("Donanım başlatılıyor (birkaç saniye sürebilir)...");
        _hardware = new HardwareService();

        try
        {
            _hardware.Open();
        }
        catch (Exception e)
        {
            Console.WriteLine($"HATA: Donanım başlatılamadı: {e.Message}");
            Console.WriteLine("Uygulamayı Yönetici olarak çalıştırdığından emin ol.");
            Console.ReadKey();
            return 1;
        }

        _controller = new FanController(_hardware);
        _fans = _hardware.GetFanChannels();

        PrintAll();
        PrintHelp();

        while (true)
        {
            Console.Write("\npoc> ");
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(input))
                continue;

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case "list":
                    PrintAll();
                    break;

                case "set" when parts.Length == 3
                              && int.TryParse(parts[1], out var setIdx)
                              && float.TryParse(parts[2], out var pct):
                    DoSet(setIdx, pct);
                    break;

                case "release" when parts.Length == 2 && int.TryParse(parts[1], out var relIdx):
                    DoRelease(relIdx);
                    break;

                case "watch":
                    Watch();
                    break;

                case "exit-keep":
                    Console.WriteLine("Close ÇAĞRILMADAN çıkılıyor — ayarlar çipte kalmalı.");
                    _hardware.KeepSettingsOnExit = true;
                    _hardware.Dispose();
                    return 0;

                case "exit-restore":
                    Console.WriteLine("LHM Close ile çıkılıyor — fanlar BIOS kontrolüne dönmeli.");
                    _hardware.KeepSettingsOnExit = false;
                    _hardware.Dispose();
                    return 0;

                default:
                    PrintHelp();
                    break;
            }
        }
    }

    private static void PrintAll()
    {
        var snapshot = _hardware!.GetSnapshot();

        Console.WriteLine("\n--- SICAKLIKLAR ---");
        foreach (var t in snapshot.Temperatures.Where(t => t.Celsius is not null))
            Console.WriteLine($"  {t.Celsius,6:F1} °C  {t.Hardware} / {t.Name}   [{t.Id}]");

        Console.WriteLine("\n--- FAN KANALLARI (kontrol edilebilir) ---");
        if (_fans.Count == 0)
            Console.WriteLine("  Hiç kontrol sensörü bulunamadı! (PawnIO kurulu mu? Yönetici misin?)");

        for (var i = 0; i < _fans.Count; i++)
        {
            var fan = _fans[i];
            var reading = snapshot.Fans.FirstOrDefault(f => f.ControlId == fan.ControlId);
            Console.WriteLine(
                $"  [{i}] {fan.Name} ({fan.HardwareName})  " +
                $"RPM: {reading?.Rpm?.ToString("F0") ?? "?"}  Kontrol: %{reading?.ControlPercent?.ToString("F0") ?? "?"}  " +
                $"[{fan.ControlId}]");
        }
    }

    private static void DoSet(int index, float percent)
    {
        if (index < 0 || index >= _fans.Count)
        {
            Console.WriteLine("Geçersiz fan numarası.");
            return;
        }

        var clamped = _controller!.ClampPercent(percent, allowZero: false);
        if (Math.Abs(clamped - percent) > 0.01f)
            Console.WriteLine($"Güvenlik tabanı: %{percent:F0} → %{clamped:F0} olarak sınırlandı.");

        var ok = _controller.SetPercent(_fans[index].ControlId, percent);
        Console.WriteLine(ok
            ? $"{_fans[index].Name} → %{clamped:F0} yazıldı. RPM değişimini görmek için `watch` yaz."
            : "YAZILAMADI — kontrol sensörü bulunamadı.");
    }

    private static void DoRelease(int index)
    {
        if (index < 0 || index >= _fans.Count)
        {
            Console.WriteLine("Geçersiz fan numarası.");
            return;
        }

        var ok = _controller!.ReleaseToBios(_fans[index].ControlId);
        Console.WriteLine(ok ? $"{_fans[index].Name} BIOS kontrolüne bırakıldı." : "İşlem başarısız.");
    }

    private static void Watch()
    {
        Console.WriteLine("10 saniye boyunca 2 sn'de bir okunuyor (iptal: Ctrl+C değil, bekle)...");
        for (var i = 0; i < 5; i++)
        {
            Thread.Sleep(2000);
            var snapshot = _hardware!.GetSnapshot();
            var line = string.Join("  |  ", snapshot.Fans.Select(f =>
                $"{f.Name}: {f.Rpm?.ToString("F0") ?? "?"} RPM (%{f.ControlPercent?.ToString("F0") ?? "?"})"));
            Console.WriteLine($"  {line}");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""

            Komutlar:
              list              Sensörleri ve fanları yeniden listele
              set <no> <yüzde>  Fana sabit hız yaz  (ör: set 2 40)
              release <no>      Fanı BIOS kontrolüne geri bırak
              watch             10 sn boyunca RPM izle
              exit-keep         Close çağırmadan çık (KALICILIK TESTİ — ayar kalmalı)
              exit-restore      LHM Close ile çık (fanlar BIOS'a dönmeli)
            """);
    }
}
