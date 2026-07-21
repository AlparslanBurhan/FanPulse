using FanPulse.App.Common;

namespace FanPulse.App.ViewModels;

public sealed class TempItemViewModel : ObservableObject
{
    private float? _celsius;

    public TempItemViewModel(string id, string name, string hardware)
    {
        Id = id;
        Name = name;
        Hardware = hardware;
        FullName = $"{name}  ({hardware})";
    }

    public string Id { get; }
    public string Name { get; }
    public string Hardware { get; }

    /// <summary>Sensör seçim kutusunda gösterilen ad (bir kez kurulur).</summary>
    public string FullName { get; }

    public float? Celsius
    {
        get => _celsius;
        set
        {
            if (Set(ref _celsius, value))
                OnPropertyChanged(nameof(Display));
        }
    }

    public string Display => Celsius is null ? "—" : $"{Celsius:F1}°";
}
