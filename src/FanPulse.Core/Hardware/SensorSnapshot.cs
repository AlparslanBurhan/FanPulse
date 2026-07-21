namespace FanPulse.Core.Hardware;

/// <summary>Tek bir sıcaklık sensörünün anlık okuması.</summary>
public sealed record TempReading(string Id, string Name, string Hardware, float? Celsius);

/// <summary>Bir fan kanalının anlık okuması (RPM + uygulanan kontrol yüzdesi).</summary>
public sealed record FanReading(string ControlId, string Name, float? Rpm, float? ControlPercent);

/// <summary>Tüm sensörlerin tek seferde alınmış görüntüsü.</summary>
public sealed record HardwareSnapshot(
    DateTime TakenAt,
    IReadOnlyList<TempReading> Temperatures,
    IReadOnlyList<FanReading> Fans);

/// <summary>Kontrol edilebilir bir fan başlığı.</summary>
public sealed record FanChannel(string ControlId, string Name, string HardwareName);
