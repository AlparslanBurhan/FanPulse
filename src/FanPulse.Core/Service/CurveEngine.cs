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

    /// <summary>
    /// Profil değişiminde histerezis/hata durumunu sıfırlar; sonraki tick koşulsuz yazar.
    /// Yalnızca Tick ile aynı seriden (RefreshAsync kritik bölgesi) çağrılmalıdır.
    /// </summary>
    public void Reset()
    {
        _lastAppliedTemp.Clear();
        _failedReads.Clear();
    }

    /// <summary>
    /// Doğrusal interpolasyon. Eğrinin sıcaklığa göre SIRALI olması sözleşmedir:
    /// editör ve ConfigStore.Load bu değişmezi kurar; burada yeniden sıralanmaz.
    /// </summary>
    public static float Evaluate(IReadOnlyList<CurvePoint> curve, float temp)
    {
        if (curve.Count == 0)
            return 100f;
        if (temp <= curve[0].Temp)
            return curve[0].Percent;
        if (temp >= curve[^1].Temp)
            return curve[^1].Percent;

        for (var i = 1; i < curve.Count; i++)
        {
            if (temp > curve[i].Temp)
                continue;

            var a = curve[i - 1];
            var b = curve[i];
            return a.Percent + (temp - a.Temp) / (b.Temp - a.Temp) * (b.Percent - a.Percent);
        }

        return curve[^1].Percent;
    }

    /// <summary>Eğri profillerini bir kez değerlendirip gerekli yazmaları yapar.</summary>
    /// <param name="refreshHardware">GUI zaten bu tick içinde Update çağırdıysa false geçilir.</param>
    public void Tick(AppConfig config, bool refreshHardware = true)
    {
        if (refreshHardware)
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
