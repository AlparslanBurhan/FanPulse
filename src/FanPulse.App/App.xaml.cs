using System.Windows;
using FanPulse.App.Localization;
using FanPulse.App.Tray;
using FanPulse.App.ViewModels;
using FanPulse.Core;
using FanPulse.Core.Hardware;
using FanPulse.Core.Service;

namespace FanPulse.App;

public partial class App : Application
{
    private SingleInstance? _instance;
    private MainViewModel? _vm;
    private TrayService? _tray;
    private MainWindow? _window;
    private bool _noticeShown;
    private bool _exiting;

    /// <summary>--startup ile eğri profili varken: pencere olmadan tepsi servisi olarak başla.</summary>
    public bool StartTrayOnly { get; init; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _instance = AcquireTakingOver();
        if (_instance is null)
        {
            MessageBox.Show(Loc.T("AlreadyRunning"), "FanPulse");
            Shutdown(1);
            return;
        }

        WatchStopSignal();

        var hardware = new HardwareService();
        var controller = new FanController(hardware);
        var engine = new CurveEngine(hardware, controller);
        _vm = new MainViewModel(hardware, controller, engine);
        _vm.TrayTooltipChanged += text => _tray?.UpdateTooltip(text);

        if (StartTrayOnly)
            EnsureTray();
        else
            ShowMainWindow();

        _ = _vm.InitializeAsync();
    }

    /// <summary>Kilidi al; çalışan headless servis varsa durdurup devral.</summary>
    private static SingleInstance? AcquireTakingOver()
    {
        var instance = SingleInstance.TryAcquire();
        if (instance is not null)
            return instance;

        SingleInstance.SignalStop();

        for (var i = 0; i < 25; i++)
        {
            Thread.Sleep(200);
            instance = SingleInstance.TryAcquire();
            if (instance is not null)
                return instance;
        }

        return null;
    }

    /// <summary>--stop sinyalini izler: pencere açıksa yok sayılır, servis modunda çıkılır.</summary>
    private void WatchStopSignal()
    {
        ThreadPool.RegisterWaitForSingleObject(
            _instance!.StopRequested,
            (_, _) => Dispatcher.Invoke(() =>
            {
                if (_window is { IsVisible: true })
                    _instance.ResetStop();
                else
                    ExitApp();
            }),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    public void ShowMainWindow()
    {
        if (_window is null)
        {
            _window = new MainWindow { DataContext = _vm };
            _window.Closed += OnWindowClosed;
        }

        _window.Show();
        _window.Activate();
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _window = null;

        if (_exiting)
            return;

        // Eğri profili varsa süreç arka planda yaşamaya devam eder (tepsi + motor).
        if (_vm!.Config.HasCurveProfiles)
        {
            EnsureTray();

            if (!_noticeShown)
            {
                _noticeShown = true;
                _tray?.ShowNotice("FanPulse", Loc.T("BackgroundNotice"));
            }
        }
        else
        {
            ExitApp(); // Sabit hızlar çipte kaldı; sürece gerek yok.
        }
    }

    private void EnsureTray()
    {
        if (_tray is not null || _vm is null || !_vm.Config.ShowTrayIcon)
            return;

        _tray = new TrayService(
            openGui: () => Dispatcher.Invoke(ShowMainWindow),
            exitApp: () => Dispatcher.Invoke(ExitApp),
            togglePause: paused =>
            {
                if (_vm is not null)
                    _vm.Paused = paused;
            });
    }

    public void ExitApp()
    {
        if (_exiting)
            return;

        _exiting = true;

        _tray?.Dispose();
        _tray = null;
        _window?.Close();
        _vm?.Dispose();
        _instance?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (!_exiting)
        {
            _tray?.Dispose();
            _vm?.Dispose();
            _instance?.Dispose();
        }

        base.OnExit(e);
    }
}
