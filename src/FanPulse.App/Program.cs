using FanPulse.Core;

namespace FanPulse.App;

/// <summary>
/// Giriş noktası ve mod yönlendirme:
///   (argümansız)  → GUI
///   --startup     → config'i uygula; eğri yoksa çık, varsa headless servis
///   --stop        → çalışan headless servisi durdur
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Contains("--stop", StringComparer.OrdinalIgnoreCase))
            return SingleInstance.SignalStop() ? 0 : 1;

        if (args.Contains("--startup", StringComparer.OrdinalIgnoreCase))
            return CliFlows.RunStartup();

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
