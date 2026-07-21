using System.Text.Json.Serialization;

namespace FanPulse.Core.Config;

public sealed class AppConfig
{
    /// <summary>Arayüz dili: "en" (varsayılan) veya "tr".</summary>
    public string Language { get; set; } = "en";

    /// <summary>Windows açılışında ayarların otomatik uygulanması (Görev Zamanlayıcı görevi).</summary>
    public bool ApplyOnStartup { get; set; }

    private float _minSpeedFloor = 20f;

    /// <summary>
    /// Güvenlik tabanı: bu yüzdenin altına yazılmaz (fan başına AllowZero hariç).
    /// Kaynağında 0-100'e sınırlanır; aksi halde Math.Clamp(değer, taban, 100)
    /// çağrıları taban &gt; 100 durumunda fırlatırdı.
    /// </summary>
    public float MinSpeedFloor
    {
        get => _minSpeedFloor;
        set => _minSpeedFloor = Math.Clamp(value, 0f, 100f);
    }

    /// <summary>Headless serviste tepsi simgesi gösterilsin mi.</summary>
    public bool ShowTrayIcon { get; set; } = true;

    /// <summary>Son pencere boyutu; null ise varsayılan kullanılır.</summary>
    public double? WindowWidth { get; set; }
    public double? WindowHeight { get; set; }
    public bool WindowMaximized { get; set; }

    public List<FanProfile> Profiles { get; set; } = new();

    [JsonIgnore]
    public bool HasCurveProfiles => Profiles.Any(p => p.Mode == FanMode.Curve);

    public FanProfile? FindProfile(string fanId) =>
        Profiles.FirstOrDefault(p => p.FanId == fanId);
}
