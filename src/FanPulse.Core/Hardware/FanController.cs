using FanPulse.Core.Config;

namespace FanPulse.Core.Hardware;

/// <summary>
/// Fan yazma işlemlerinin güvenlik politikası: taban hız sınırı (minSpeedFloor)
/// ve %0'a yalnızca fan başına açık izinle inme.
/// </summary>
public sealed class FanController
{
    private readonly HardwareService _hardware;

    public FanController(HardwareService hardware) => _hardware = hardware;

    /// <summary>Bu tabanın altına yazma yapılmaz (%0 izni hariç).</summary>
    public float MinSpeedFloor { get; set; } = 20f;

    public bool SetPercent(string fanId, float percent, bool allowZero = false)
    {
        var clamped = ClampPercent(percent, allowZero);
        return _hardware.TrySetFanPercent(fanId, clamped);
    }

    public bool ApplyFixed(FanProfile profile) =>
        SetPercent(profile.FanId, profile.FixedPercent, profile.AllowZero);

    public bool ReleaseToBios(string fanId) => _hardware.TryReleaseFan(fanId);

    public float ClampPercent(float percent, bool allowZero)
    {
        if (allowZero && percent <= 0f)
            return 0f;

        return Math.Clamp(percent, MinSpeedFloor, 100f);
    }
}
