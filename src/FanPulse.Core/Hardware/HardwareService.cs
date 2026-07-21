using LibreHardwareMonitor.Hardware;

namespace FanPulse.Core.Hardware;

/// <summary>
/// LibreHardwareMonitor üzerinden anakart (NCT6687D), CPU ve GPU sensörlerine erişim.
///
/// KALICILIK NOTU: LibreHardwareMonitor, <c>Computer.Close()</c> sırasında bu süreçte
/// değiştirilen fan kontrol register'larını BIOS varsayılanlarına GERİ YÜKLER.
/// "Ayarla ve kapat" davranışı için, kontrol yazılmış bir oturumda Close çağrılmaz
/// (bkz. <see cref="KeepSettingsOnExit"/>); süreç sonlanınca sürücü tanıtıcılarını
/// işletim sistemi serbest bırakır ve çip son yazılan değerlerle çalışmaya devam eder.
///
/// Sensör kümesi Open() sonrası değişmez; kimlik → sensör indeksi bir kez kurulur,
/// tüm aramalar O(1), kimlik stringleri süreç ömründe bir kez üretilir.
/// </summary>
public sealed class HardwareService : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor = new();
    private readonly object _sync = new();
    private bool _opened;
    private bool _controlWritten;

    private Dictionary<string, ISensor>? _sensorsById;
    private List<(string Id, ISensor Sensor, string Hardware)>? _tempSensors;
    private List<(string ControlId, ISensor Control, ISensor? Rpm, string Hardware)>? _fanPairs;

    /// <summary>true ise (varsayılan) kontrol yazılmış oturumlarda Dispose, Close çağırmaz.</summary>
    public bool KeepSettingsOnExit { get; set; } = true;

    public HardwareService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
        };
    }

    public void Open()
    {
        lock (_sync)
        {
            if (_opened)
                return;
            _computer.Open();
            _opened = true;
        }

        Update();
        BuildIndex();
    }

    /// <summary>Tüm donanım sensörlerini yeniden okur.</summary>
    public void Update()
    {
        lock (_sync)
        {
            _computer.Accept(_updateVisitor);
        }
    }

    public HardwareSnapshot GetSnapshot(bool refresh = true)
    {
        if (refresh)
            Update();

        lock (_sync)
        {
            if (_tempSensors is null || _fanPairs is null)
                return new HardwareSnapshot(DateTime.Now, [], []);

            var temps = new List<TempReading>(_tempSensors.Count);
            foreach (var (id, sensor, hardware) in _tempSensors)
                temps.Add(new TempReading(id, sensor.Name, hardware, sensor.Value));

            var fans = new List<FanReading>(_fanPairs.Count);
            foreach (var (controlId, control, rpm, _) in _fanPairs)
                fans.Add(new FanReading(controlId, control.Name, rpm?.Value, control.Value));

            return new HardwareSnapshot(DateTime.Now, temps, fans);
        }
    }

    /// <summary>Kontrol edilebilir fan başlıklarını (Control sensörlerini) listeler.</summary>
    public IReadOnlyList<FanChannel> GetFanChannels()
    {
        lock (_sync)
        {
            return _fanPairs is null
                ? []
                : _fanPairs
                    .Select(f => new FanChannel(f.ControlId, f.Control.Name, f.Hardware))
                    .ToList();
        }
    }

    /// <summary>Bir fan kontrolüne yüzde değeri yazar. Değer çağıran tarafça sınırlandırılmış olmalıdır.</summary>
    public bool TrySetFanPercent(string controlId, float percent)
    {
        lock (_sync)
        {
            var sensor = FindSensor(controlId);
            if (sensor?.Control is null)
                return false;

            sensor.Control.SetSoftware(percent);
            _controlWritten = true;
            return true;
        }
    }

    /// <summary>Fan kontrolünü BIOS/varsayılan moda geri bırakır.</summary>
    public bool TryReleaseFan(string controlId)
    {
        lock (_sync)
        {
            var sensor = FindSensor(controlId);
            if (sensor?.Control is null)
                return false;

            sensor.Control.SetDefault();
            return true;
        }
    }

    public float? ReadSensorValue(string sensorId)
    {
        lock (_sync)
        {
            return FindSensor(sensorId)?.Value;
        }
    }

    private ISensor? FindSensor(string identifier) =>
        _sensorsById?.GetValueOrDefault(identifier);

    /// <summary>Open() sonrası bir kez: kimlik sözlüğü, sıcaklık listesi ve fan çiftleri kurulur.</summary>
    private void BuildIndex()
    {
        lock (_sync)
        {
            var byId = new Dictionary<string, ISensor>();
            var temps = new List<(string, ISensor, string)>();
            var fanPairs = new List<(string, ISensor, ISensor?, string)>();

            foreach (var hardware in AllHardware())
            {
                foreach (var sensor in hardware.Sensors)
                {
                    var id = sensor.Identifier.ToString();
                    byId[id] = sensor;

                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        temps.Add((id, sensor, hardware.Name));
                    }
                    else if (sensor.SensorType == SensorType.Control && sensor.Control is not null)
                    {
                        var rpm = hardware.Sensors.FirstOrDefault(s =>
                            s.SensorType == SensorType.Fan && s.Index == sensor.Index);
                        fanPairs.Add((id, sensor, rpm, hardware.Name));
                    }
                }
            }

            _sensorsById = byId;
            _tempSensors = temps;
            _fanPairs = fanPairs;
        }
    }

    /// <summary>Ana donanımlar + alt donanımlar (anakart → Super I/O) düzleştirilmiş.</summary>
    private IEnumerable<IHardware> AllHardware()
    {
        foreach (var hardware in _computer.Hardware)
        {
            yield return hardware;
            foreach (var sub in hardware.SubHardware)
                yield return sub;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (!_opened)
                return;

            _opened = false;

            // Kontrol yazıldıysa ve ayarların kalması isteniyorsa Close atlanır:
            // LHM Close içinde register'ları eski haline döndürürdü.
            if (_controlWritten && KeepSettingsOnExit)
                return;

            _computer.Close();
        }
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
