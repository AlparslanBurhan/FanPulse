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

        var fixedProfiles = config.Profiles.Where(p => p.Mode == FanMode.Fixed).ToList();
        if (fixedProfiles.Count == 0)
            return 0;

        using var hardware = new HardwareService();
        hardware.Open();

        var controller = new FanController(hardware) { MinSpeedFloor = config.MinSpeedFloor };
        foreach (var profile in fixedProfiles)
            controller.ApplyFixed(profile);

        return 0;
    }
}
