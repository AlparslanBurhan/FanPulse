namespace FanPulse.Core;

/// <summary>
/// Tek süreç garantisi ve çalışan headless servise durdurma sinyali gönderme.
///
/// Kilit nesnesi olarak Mutex değil EventWaitHandle kullanılır: sahiplik
/// (thread-affinity) semantiğine ihtiyaç yok — tek gereken "createdNew" bilgisi
/// ve tanıtıcı ömrü. Böylece Dispose herhangi bir thread'den güvenle çağrılabilir
/// ve edinme işlemi arka plan thread'ine taşınabilir.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MarkerName = @"Global\FanPulse.Instance";
    private const string StopEventName = @"Global\FanPulse.Stop";

    private readonly EventWaitHandle _marker;
    private readonly EventWaitHandle _stopEvent;

    private SingleInstance(EventWaitHandle marker, EventWaitHandle stopEvent)
    {
        _marker = marker;
        _stopEvent = stopEvent;
    }

    /// <summary>Servis durdurma isteği geldiğinde işaretlenen bekleme tanıtıcısı.</summary>
    public WaitHandle StopRequested => _stopEvent;

    /// <summary>Kilidi almayı dener; başka bir FanPulse örneği çalışıyorsa null döner.</summary>
    public static SingleInstance? TryAcquire()
    {
        var marker = new EventWaitHandle(
            false, EventResetMode.ManualReset, MarkerName, out var createdNew);

        if (!createdNew)
        {
            marker.Dispose();
            return null;
        }

        var stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset, StopEventName);
        return new SingleInstance(marker, stopEvent);
    }

    /// <summary>Çalışan örneğe durdurma sinyali gönderir. Örnek yoksa false.</summary>
    public static bool SignalStop()
    {
        if (EventWaitHandle.TryOpenExisting(StopEventName, out var handle))
        {
            using (handle)
            {
                handle.Set();
                return true;
            }
        }

        return false;
    }

    /// <summary>Durdurma sinyali göz ardı edildiğinde (GUI açıkken) bayrağı sıfırlar.</summary>
    public void ResetStop() => _stopEvent.Reset();

    public void Dispose()
    {
        _stopEvent.Dispose();
        _marker.Dispose();
    }
}
