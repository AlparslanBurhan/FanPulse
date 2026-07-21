using FanPulse.App.Common;
using FanPulse.App.Localization;
using FanPulse.Core.Config;

namespace FanPulse.App.ViewModels;

public sealed class FanItemViewModel : ObservableObject
{
    /// <summary>Eğri moduna ilk geçişte varsayılan kaynak sensörü sağlar (MainViewModel atar).</summary>
    public static Func<string?>? DefaultSensorProvider { get; set; }

    private float? _rpm;
    private float? _controlPercent;
    private FanMode _mode = FanMode.Bios;
    private float _fixedPercent = 50f;
    private bool _allowZero;
    private string? _sensorId;

    public FanItemViewModel(string controlId, string name, string hardwareName)
    {
        ControlId = controlId;
        Name = name;
        HardwareName = hardwareName;
    }

    public string ControlId { get; }
    public string Name { get; }
    public string HardwareName { get; }

    public List<CurvePoint> CurvePoints { get; } = new();

    public float? Rpm
    {
        get => _rpm;
        set
        {
            if (Set(ref _rpm, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public float? ControlPercent
    {
        get => _controlPercent;
        set
        {
            if (Set(ref _controlPercent, value))
                OnPropertyChanged(nameof(StatusText));
        }
    }

    public string StatusText
    {
        get
        {
            var rpm = Rpm is null ? "—" : $"{Rpm:F0} RPM";
            var pct = ControlPercent is null ? "" : $"  ·  %{ControlPercent:F0}";
            return rpm + pct;
        }
    }

    public FanMode Mode
    {
        get => _mode;
        set
        {
            if (!Set(ref _mode, value))
                return;

            if (value == FanMode.Curve)
                EnsureCurveDefaults();

            OnPropertyChanged(nameof(IsBios));
            OnPropertyChanged(nameof(IsFixed));
            OnPropertyChanged(nameof(IsCurve));
            OnPropertyChanged(nameof(ModeBadge));
        }
    }

    public bool IsBios
    {
        get => Mode == FanMode.Bios;
        set { if (value) Mode = FanMode.Bios; }
    }

    public bool IsFixed
    {
        get => Mode == FanMode.Fixed;
        set { if (value) Mode = FanMode.Fixed; }
    }

    public bool IsCurve
    {
        get => Mode == FanMode.Curve;
        set { if (value) Mode = FanMode.Curve; }
    }

    public string ModeBadge => Mode switch
    {
        FanMode.Fixed => Loc.T("ModeFixed"),
        FanMode.Curve => Loc.T("ModeCurve"),
        _ => Loc.T("ModeBios"),
    };

    public float FixedPercent
    {
        get => _fixedPercent;
        set
        {
            if (Set(ref _fixedPercent, (float)Math.Round(value)))
                OnPropertyChanged(nameof(FixedPercentText));
        }
    }

    public string FixedPercentText => $"%{FixedPercent:F0}";

    public bool AllowZero
    {
        get => _allowZero;
        set => Set(ref _allowZero, value);
    }

    public string? SensorId
    {
        get => _sensorId;
        set => Set(ref _sensorId, value);
    }

    public void LoadProfile(FanProfile profile)
    {
        _mode = profile.Mode;
        _fixedPercent = profile.FixedPercent;
        _allowZero = profile.AllowZero;
        _sensorId = profile.SensorId;
        CurvePoints.Clear();
        // Derin kopya: VM düzenleme tamponu ile Config'in aktif eğrisi aynı
        // CurvePoint nesnelerini paylaşmamalı — aksi halde sürükleme, kaydetmeden
        // çalışan motoru etkiler (tek buton sözleşmesi ihlali + veri yarışı).
        CurvePoints.AddRange(profile.Curve.OrderBy(p => p.Temp).Select(Clone));

        if (_mode == FanMode.Curve)
            EnsureCurveDefaults();
    }

    public FanProfile ToProfile() => new()
    {
        FanId = ControlId,
        Name = Name,
        Mode = Mode,
        FixedPercent = FixedPercent,
        AllowZero = AllowZero,
        SensorId = SensorId,
        Curve = CurvePoints.OrderBy(p => p.Temp).Select(Clone).ToList(),
    };

    private static CurvePoint Clone(CurvePoint p) => new() { Temp = p.Temp, Percent = p.Percent };

    private void EnsureCurveDefaults()
    {
        if (CurvePoints.Count < 2)
        {
            CurvePoints.Clear();
            CurvePoints.Add(new CurvePoint { Temp = 40, Percent = 30 });
            CurvePoints.Add(new CurvePoint { Temp = 60, Percent = 50 });
            CurvePoints.Add(new CurvePoint { Temp = 80, Percent = 100 });
        }

        SensorId ??= DefaultSensorProvider?.Invoke();
    }
}
