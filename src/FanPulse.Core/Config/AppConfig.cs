namespace FanPulse.Core.Config;

public sealed class AppConfig
{
    /// <summary>Arayüz dili: "tr" veya "en".</summary>
    public string Language { get; set; } = "tr";

    /// <summary>Windows açılışında ayarların otomatik uygulanması (Görev Zamanlayıcı görevi).</summary>
    public bool ApplyOnStartup { get; set; }

    /// <summary>Güvenlik tabanı: bu yüzdenin altına yazılmaz (fan başına AllowZero hariç).</summary>
    public float MinSpeedFloor { get; set; } = 20f;

    /// <summary>Headless serviste tepsi simgesi gösterilsin mi.</summary>
    public bool ShowTrayIcon { get; set; } = true;

    public List<FanProfile> Profiles { get; set; } = new();

    public bool HasCurveProfiles => Profiles.Any(p => p.Mode == FanMode.Curve);

    public FanProfile? FindProfile(string fanId) =>
        Profiles.FirstOrDefault(p => p.FanId == fanId);
}
