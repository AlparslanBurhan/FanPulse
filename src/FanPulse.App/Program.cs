using System.Globalization;
using FanPulse.Core;
using FanPulse.Core.Config;

namespace FanPulse.App;

/// <summary>
/// Giriş noktası ve mod yönlendirme:
///   (argümansız)  → GUI
///   --startup     → sadece sabit hızlar varsa WPF yüklemeden yaz ve çık;
///                   eğri varsa tepsi simgeli headless servis
///   --stop        → çalışan headless servisi durdur
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--stop", StringComparer.OrdinalIgnoreCase))
            return SingleInstance.SignalStop() ? 0 : 1;

        var config = ConfigStore.Load();

        var culture = new CultureInfo(
            string.Equals(config.Language, "tr", StringComparison.OrdinalIgnoreCase) ? "tr" : "en");
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var startup = args.Contains("--startup", StringComparer.OrdinalIgnoreCase);

        if (startup && !config.HasCurveProfiles)
            return CliFlows.ApplyFixedAndExit(config);

        var app = new App { StartTrayOnly = startup, InitialConfig = config };
        app.InitializeComponent();
        return app.Run();
    }
}
