using System.Collections.ObjectModel;
using System.Windows.Threading;
using FanPulse.App.Common;
using FanPulse.App.Localization;
using FanPulse.Core.Config;
using FanPulse.Core.Hardware;
using FanPulse.Core.Service;

namespace FanPulse.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    public const int ChartCapacity = 30; // 2 sn aralıkla 60 saniye

    private readonly HardwareService _hardware;
    private readonly FanController _controller;
    private readonly CurveEngine _engine;
    private readonly DispatcherTimer _timer;
    private List<FanProfile> _orphanProfiles = new();
    private bool _refreshBusy;

    private FanItemViewModel? _selectedFan;
    private bool _isHardwareReady;
    private bool _isPawnIoMissing;
    private bool _paused;
    private string _statusMessage = "";

    private string? _chartCpuId;
    private string? _chartGpuId;
    private string? _chartSystemId;

    public MainViewModel(HardwareService hardware, FanController controller, CurveEngine engine, AppConfig config)
    {
        _hardware = hardware;
        _controller = controller;
        _engine = engine;

        Config = config;
        _controller.MinSpeedFloor = Config.MinSpeedFloor;

        FanItemViewModel.DefaultSensorProvider = () => _chartCpuId ?? Temps.FirstOrDefault()?.Id;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        _timer.Tick += async (_, _) => await RefreshAsync();

        SaveApplyCommand = new RelayCommand(SaveApply, () => IsHardwareReady);
    }

    /// <summary>Kaydedilmiş (diskteki) yapılandırma. Save ile güncellenir.</summary>
    public AppConfig Config { get; }

    public ObservableCollection<TempItemViewModel> Temps { get; } = new();
    public ObservableCollection<FanItemViewModel> Fans { get; } = new();

    public List<double> ChartCpu { get; } = new();
    public List<double> ChartGpu { get; } = new();
    public List<double> ChartSystem { get; } = new();

    public event Action? ChartUpdated;
    public event Action<string>? TrayTooltipChanged;

    public RelayCommand SaveApplyCommand { get; }

    public FanItemViewModel? SelectedFan
    {
        get => _selectedFan;
        set
        {
            if (Set(ref _selectedFan, value))
                OnPropertyChanged(nameof(HasSelectedFan));
        }
    }

    public bool HasSelectedFan => SelectedFan is not null;

    public bool IsHardwareReady
    {
        get => _isHardwareReady;
        private set => Set(ref _isHardwareReady, value);
    }

    public bool IsPawnIoMissing
    {
        get => _isPawnIoMissing;
        private set => Set(ref _isPawnIoMissing, value);
    }

    /// <summary>Tepsiden eğri kontrolünü duraklatma.</summary>
    public bool Paused
    {
        get => _paused;
        set => Set(ref _paused, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public async Task InitializeAsync()
    {
        IsPawnIoMissing = !PawnIo.IsInstalled();

        IReadOnlyList<FanChannel> channels = [];
        HardwareSnapshot? snapshot = null;

        await Task.Run(() =>
        {
            _hardware.Open();
            channels = _hardware.GetFanChannels();
            snapshot = _hardware.GetSnapshot(refresh: false);
        });

        foreach (var temp in snapshot!.Temperatures)
        {
            Temps.Add(new TempItemViewModel(temp.Id, temp.Name, temp.Hardware)
            {
                Celsius = temp.Celsius,
            });
        }

        PickChartSensors();

        var channelIds = new HashSet<string>(channels.Select(c => c.ControlId));
        _orphanProfiles = Config.Profiles.Where(p => !channelIds.Contains(p.FanId)).ToList();

        foreach (var channel in channels)
        {
            var vm = new FanItemViewModel(channel.ControlId, channel.Name, channel.HardwareName);
            var profile = Config.FindProfile(channel.ControlId);
            if (profile is not null)
                vm.LoadProfile(profile);

            var reading = snapshot.Fans.FirstOrDefault(f => f.ControlId == channel.ControlId);
            vm.Rpm = reading?.Rpm;
            vm.ControlPercent = reading?.ControlPercent;

            Fans.Add(vm);
        }

        IsHardwareReady = true;
        _timer.Start();

        // Exe taşındıysa açılış görevindeki yolu sessizce onar.
        if (Config.ApplyOnStartup && Environment.ProcessPath is { } exePath)
        {
            try
            {
                StartupTaskManager.EnsurePathCurrent(exePath);
            }
            catch
            {
                // Görev onarılamazsa kullanıcı Kaydet ile yeniden kurabilir.
            }
        }
    }

    private async Task RefreshAsync()
    {
        if (_refreshBusy)
            return;

        _refreshBusy = true;
        try
        {
            var runCurves = Config.HasCurveProfiles && !Paused;

            var snapshot = await Task.Run(() =>
            {
                var snap = _hardware.GetSnapshot();
                if (runCurves)
                    _engine.Tick(Config, refreshHardware: false);
                return snap;
            });

            foreach (var temp in Temps)
                temp.Celsius = snapshot.Temperatures.FirstOrDefault(t => t.Id == temp.Id)?.Celsius;

            foreach (var fan in Fans)
            {
                var reading = snapshot.Fans.FirstOrDefault(f => f.ControlId == fan.ControlId);
                fan.Rpm = reading?.Rpm;
                fan.ControlPercent = reading?.ControlPercent;
            }

            PushChart();
            TrayTooltipChanged?.Invoke(BuildTooltip());
        }
        catch
        {
            // Tek bir okuma hatası UI'ı düşürmesin; sonraki tick'te tekrar denenir.
        }
        finally
        {
            _refreshBusy = false;
        }
    }

    private void PushChart()
    {
        Append(ChartCpu, _chartCpuId);
        Append(ChartGpu, _chartGpuId);
        Append(ChartSystem, _chartSystemId);
        ChartUpdated?.Invoke();

        void Append(List<double> buffer, string? id)
        {
            var value = id is null ? null : Temps.FirstOrDefault(t => t.Id == id)?.Celsius;
            buffer.Add(value ?? double.NaN);
            if (buffer.Count > ChartCapacity)
                buffer.RemoveAt(0);
        }
    }

    private void PickChartSensors()
    {
        _chartCpuId =
            (Temps.FirstOrDefault(t => t.Id.Contains("cpu") && t.Name.Contains("Tctl"))
             ?? Temps.FirstOrDefault(t => t.Id.Contains("cpu"))
             ?? Temps.FirstOrDefault(t => t.Name.Contains("CPU")))?.Id;

        _chartGpuId =
            (Temps.FirstOrDefault(t => t.Id.Contains("gpu") && t.Name == "GPU Core")
             ?? Temps.FirstOrDefault(t => t.Id.Contains("gpu")))?.Id;

        _chartSystemId = Temps.FirstOrDefault(t => t.Name == "System")?.Id;
    }

    private string BuildTooltip()
    {
        var parts = new List<string>();

        var cpu = Temps.FirstOrDefault(t => t.Id == _chartCpuId)?.Celsius;
        if (cpu is not null)
            parts.Add($"CPU {cpu:F0}°C");

        var gpu = Temps.FirstOrDefault(t => t.Id == _chartGpuId)?.Celsius;
        if (gpu is not null)
            parts.Add($"GPU {gpu:F0}°C");

        var fan = Fans.FirstOrDefault(f => f.Rpm > 0);
        if (fan is not null)
            parts.Add($"{fan.Name} {fan.Rpm:F0} RPM");

        var text = "FanPulse — " + string.Join(" · ", parts);
        return text.Length <= 120 ? text : text[..120];
    }

    private void SaveApply()
    {
        try
        {
            var previouslyManaged = Config.Profiles
                .Where(p => p.Mode != FanMode.Bios)
                .Select(p => p.FanId)
                .ToHashSet();

            Config.Profiles = Fans
                .Where(f => f.Mode != FanMode.Bios)
                .Select(f => f.ToProfile())
                .Concat(_orphanProfiles)
                .ToList();

            _controller.MinSpeedFloor = Config.MinSpeedFloor;
            ConfigStore.Save(Config);

            // BIOS'a geri alınan fanları serbest bırak.
            foreach (var fan in Fans.Where(f => f.Mode == FanMode.Bios && previouslyManaged.Contains(f.ControlId)))
                _controller.ReleaseToBios(fan.ControlId);

            foreach (var profile in Config.Profiles.Where(p => p.Mode == FanMode.Fixed))
                _controller.ApplyFixed(profile);

            if (Config.HasCurveProfiles)
                _engine.Tick(Config);

            var exePath = Environment.ProcessPath;
            if (exePath is not null)
            {
                if (Config.ApplyOnStartup)
                    StartupTaskManager.Install(exePath);
                else
                    StartupTaskManager.Uninstall();
            }

            StatusMessage = $"{Loc.T("Saved")}  ({DateTime.Now:HH:mm:ss})";
        }
        catch (Exception e)
        {
            StatusMessage = e.Message;
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _hardware.Dispose(); // KeepSettingsOnExit=true: çip son değerlerle kalır
    }
}
