using FanPulse.Core.Config;
using FanPulse.Core.Hardware;

namespace FanPulse.Core.Service;

/// <summary>
/// Eğri modundaki fanlar için sıcaklık → yüzde hesabı ve uygulaması.
/// Zamanlayıcı sahipliği çağırandadır; her tick'te <see cref="Tick"/> çağrılır.
/// </summary>
public sealed class CurveEngine
{
    private readonly HardwareService _hardware;
    private readonly FanController _controller;
    private readonly Dictionary<string, float> _lastAppliedTemp = new();
    private readonly Dictionary<string, int> _failedReads = new();

    /// <summary>Sıcaklık bu kadar değişmeden yeni değer yazılmaz (gereksiz salınımı önler).</summary>
    public float HysteresisCelsius { get; set; } = 2f;

    /// <summary>Sensör art arda okunamazsa uygulanacak güvenli hız.</summary>
    public float FailsafePercent { get; set; } = 60f;

    public int FailsafeAfterMissedReads { get; set; } = 3;

    public CurveEngine(HardwareService hardware, FanController controller)
    {
        _hardware = hardware;
        _controller = controller;
    }

    /// <summary>Sıralı eğri üzerinde doğrusal interpolasyon.</summary>
    public static float Evaluate(IReadOnlyList<CurvePoint> curve, float temp)
    {
        if (curve.Count == 0)
            return 100f;

        var sorted = curve.OrderBy(p => p.Temp).ToList();

        if (temp <= sorted[0].Temp)
            return sorted[0].Percent;
        if (temp >= sorted[^1].Temp)
            return sorted[^1].Percent;

        for (var i = 0; i < sorted.Count - 1; i++)
        {
            var a = sorted[i];
            var b = sorted[i + 1];
            if (temp >= a.Temp && temp <= b.Temp)
            {
                var t = (temp - a.Temp) / (b.Temp - a.Temp);
                return a.Percent + t * (b.Percent - a.Percent);
            }
        }

        return sorted[^1].Percent;
    }

    /// <summary>Eğri profillerini bir kez değerlendirip gerekli yazmaları yapar.</summary>
    public void Tick(AppConfig config)
    {
        _hardware.Update();

        foreach (var profile in config.Profiles.Where(p => p.Mode == FanMode.Curve))
        {
            var temp = profile.SensorId is null ? null : _hardware.ReadSensorValue(profile.SensorId);

            if (temp is null)
            {
                var misses = _failedReads.GetValueOrDefault(profile.FanId) + 1;
                _failedReads[profile.FanId] = misses;

                if (misses >= FailsafeAfterMissedReads)
                    _controller.SetPercent(profile.FanId, FailsafePercent, profile.AllowZero);

                continue;
            }

            _failedReads[profile.FanId] = 0;

            // Histerezis: son uygulanan sıcaklığa yeterince yakınsa dokunma.
            if (_lastAppliedTemp.TryGetValue(profile.FanId, out var last) &&
                Math.Abs(temp.Value - last) < HysteresisCelsius)
            {
                continue;
            }

            var percent = Evaluate(profile.Curve, temp.Value);
            if (_controller.SetPercent(profile.FanId, percent, profile.AllowZero))
                _lastAppliedTemp[profile.FanId] = temp.Value;
        }
    }
}
