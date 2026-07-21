namespace FanPulse.Core.Config;

public enum FanMode
{
    /// <summary>FanPulse karışmaz; BIOS eğrisi geçerlidir.</summary>
    Bios,

    /// <summary>Sabit yüzde: çipe bir kez yazılır, arka plan süreci gerekmez.</summary>
    Fixed,

    /// <summary>Sıcaklığa bağlı eğri: hafif headless servis gerektirir.</summary>
    Curve,
}

public sealed class CurvePoint
{
    public float Temp { get; set; }
    public float Percent { get; set; }
}

public sealed class FanProfile
{
    /// <summary>Kontrol sensörünün LHM kimliği (ör. /lpc/nct6687d/0/control/0).</summary>
    public string FanId { get; set; } = "";

    /// <summary>Kullanıcıya gösterilen ad (ör. "CPU Fan").</summary>
    public string Name { get; set; } = "";

    public FanMode Mode { get; set; } = FanMode.Bios;

    public float FixedPercent { get; set; } = 50f;

    /// <summary>Eğri modunda kaynak sıcaklık sensörünün kimliği.</summary>
    public string? SensorId { get; set; }

    /// <summary>Sıcaklığa göre artan sırada eğri noktaları.</summary>
    public List<CurvePoint> Curve { get; set; } = new();

    /// <summary>true ise bu fan için %0 (durdurma) yazılabilir.</summary>
    public bool AllowZero { get; set; }
}
