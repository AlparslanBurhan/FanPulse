using System.Globalization;
using System.Resources;
using System.Windows.Markup;

namespace FanPulse.App.Localization;

/// <summary>Resources/Strings*.resx üzerinden yerelleştirme.</summary>
public static class Loc
{
    private static readonly ResourceManager Rm =
        new("FanPulse.App.Resources.Strings", typeof(Loc).Assembly);

    public static string T(string key) =>
        Rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
}

/// <summary>XAML kullanımı: Text="{loc:Loc Temps}"</summary>
public sealed class LocExtension : MarkupExtension
{
    public string Key { get; set; }

    public LocExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key);
}
