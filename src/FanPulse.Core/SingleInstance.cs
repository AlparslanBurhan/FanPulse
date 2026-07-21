namespace FanPulse.Core;

/// <summary>
/// Tek süreç garantisi (named mutex) ve çalışan headless servise durdurma
/// sinyali gönderme (named event).
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Global\FanPulse";
    private const string StopEventName = @"Global\FanPulse.Stop";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _stopEvent;

    private SingleInstance(Mutex mutex, EventWaitHandle stopEvent)
    {
        _mutex = mutex;
        _stopEvent = stopEvent;
    }

    /// <summary>Servis durdurma isteği geldiğinde işaretlenen bekleme tanıtıcısı.</summary>
    public WaitHandle StopRequested => _stopEvent;

    /// <summary>Kilidi almayı dener; başka bir FanPulse örneği çalışıyorsa null döner.</summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            return null;
        }

        var stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset, StopEventName);
        return new SingleInstance(mutex, stopEvent);
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

    public void Dispose()
    {
        _stopEvent.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
