using FanPulse.Core;
using FanPulse.Core.Config;
using FanPulse.Core.Hardware;
using FanPulse.Core.Service;

namespace FanPulse.App;

/// <summary>
/// GUI açılmadan çalışan açılış akışı. Sabit hızlar bir kez yazılır;
/// eğri profili varsa süreç headless servise dönüşür.
/// (Aşama 5: tepsi simgesi bu akışa eklenecek.)
/// </summary>
internal static class CliFlows
{
    public static int RunStartup()
    {
        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
            return 2; // Zaten bir FanPulse örneği çalışıyor.

        var config = ConfigStore.Load();
        if (config.Profiles.Count == 0)
            return 0;

        using var hardware = new HardwareService();
        hardware.Open();

        var controller = new FanController(hardware) { MinSpeedFloor = config.MinSpeedFloor };

        foreach (var profile in config.Profiles.Where(p => p.Mode == FanMode.Fixed))
            controller.ApplyFixed(profile);

        if (!config.HasCurveProfiles)
            return 0; // Yaz ve çık: arka planda hiçbir şey kalmaz.

        var engine = new CurveEngine(hardware, controller);
        engine.Tick(config);

        while (!instance.StopRequested.WaitOne(TimeSpan.FromSeconds(2)))
            engine.Tick(config);

        return 0;
    }
}
