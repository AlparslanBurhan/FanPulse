using FanPulse.Core;
using FanPulse.Core.Config;
using FanPulse.Core.Hardware;

namespace FanPulse.App;

internal static class CliFlows
{
    /// <summary>
    /// Açılış hızlı yolu: WPF hiç yüklenmeden sabit hızları çipe yazar ve çıkar.
    /// Arka planda hiçbir süreç kalmaz; çip son değerlerle çalışmaya devam eder.
    /// </summary>
    public static int ApplyFixedAndExit(AppConfig config)
    {
        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
            return 2; // Zaten bir FanPulse örneği çalışıyor; ona karışma.

        if (!config.Profiles.Any(p => p.Mode == FanMode.Fixed))
            return 0;

        using var hardware = new HardwareService();
        hardware.Open();

        new FanController(hardware).ApplyAllFixed(config);
        return 0;
    }
}
