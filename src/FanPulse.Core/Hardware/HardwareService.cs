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
/// </summary>
public sealed class HardwareService : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor = new();
    private readonly object _sync = new();
    private bool _opened;
    private bool _controlWritten;

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

        var temps = new List<TempReading>();
        var fans = new List<FanReading>();

        lock (_sync)
        {
            foreach (var hardware in AllHardware())
            {
                var sensors = hardware.Sensors;
                foreach (var sensor in sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        temps.Add(new TempReading(
                            sensor.Identifier.ToString(),
                            sensor.Name,
                            hardware.Name,
                            sensor.Value));
                    }
                    else if (sensor.SensorType == SensorType.Control && sensor.Control is not null)
                    {
                        var rpm = sensors.FirstOrDefault(s =>
                            s.SensorType == SensorType.Fan && s.Index == sensor.Index);

                        fans.Add(new FanReading(
                            sensor.Identifier.ToString(),
                            sensor.Name,
                            rpm?.Value,
                            sensor.Value));
                    }
                }
            }
        }

        return new HardwareSnapshot(DateTime.Now, temps, fans);
    }

    /// <summary>Kontrol edilebilir fan başlıklarını (Control sensörlerini) listeler.</summary>
    public IReadOnlyList<FanChannel> GetFanChannels()
    {
        var channels = new List<FanChannel>();

        lock (_sync)
        {
            foreach (var hardware in AllHardware())
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType != SensorType.Control || sensor.Control is null)
                        continue;

                    var rpm = hardware.Sensors.FirstOrDefault(s =>
                        s.SensorType == SensorType.Fan && s.Index == sensor.Index);

                    channels.Add(new FanChannel(
                        sensor.Identifier.ToString(),
                        sensor.Name,
                        hardware.Name,
                        rpm?.Identifier.ToString()));
                }
            }
        }

        return channels;
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

    private ISensor? FindSensor(string identifier)
    {
        foreach (var hardware in AllHardware())
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.Identifier.ToString() == identifier)
                    return sensor;
            }
        }

        return null;
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
