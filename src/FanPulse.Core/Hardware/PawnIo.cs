using Microsoft.Win32;

namespace FanPulse.Core.Hardware;

/// <summary>
/// PawnIO sürücüsü denetimi. LibreHardwareMonitor 0.9.6+ çekirdek erişimi için
/// imzalı PawnIO sürücüsünü kullanır; kurulu değilse anakart sensörlerine erişilemez.
/// Kullanıcı https://pawnio.eu adresinden kurmalıdır.
/// </summary>
public static class PawnIo
{
    public const string DownloadUrl = "https://pawnio.eu/";

    public static bool IsInstalled()
    {
        // Sürücü hizmeti kaydı: kurulumla birlikte oluşur.
        using var service = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\PawnIO");
        if (service is not null)
            return true;

        using var software = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\PawnIO");
        return software is not null;
    }
}
