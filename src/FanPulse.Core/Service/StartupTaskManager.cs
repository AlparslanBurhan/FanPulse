using Microsoft.Win32.TaskScheduler;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace FanPulse.Core.Service;

/// <summary>
/// "Windows açılışında uygula" özelliği: oturum açılışında en yüksek ayrıcalıkla
/// <c>FanPulse.exe --startup</c> çalıştıran Görev Zamanlayıcı görevini yönetir.
/// </summary>
public static class StartupTaskManager
{
    public const string TaskName = "FanPulse Startup";
    private const string StartupArgument = "--startup";

    public static bool IsInstalled()
    {
        using var ts = new TaskService();
        return ts.GetTask(TaskName) is not null;
    }

    public static string? InstalledExePath()
    {
        using var ts = new TaskService();
        using Task? task = ts.GetTask(TaskName);
        var action = task?.Definition.Actions.OfType<ExecAction>().FirstOrDefault();
        return action?.Path;
    }

    public static void Install(string exePath)
    {
        using var ts = new TaskService();
        var definition = ts.NewTask();

        definition.RegistrationInfo.Description =
            "FanPulse: kayıtlı fan ayarlarını Windows açılışında donanıma uygular.";
        definition.Principal.RunLevel = TaskRunLevel.Highest;

        var trigger = new LogonTrigger { Delay = TimeSpan.FromSeconds(10) };
        definition.Triggers.Add(trigger);
        definition.Actions.Add(new ExecAction(exePath, StartupArgument));

        definition.Settings.DisallowStartIfOnBatteries = false;
        definition.Settings.StopIfGoingOnBatteries = false;
        definition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
        definition.Settings.StartWhenAvailable = true;

        ts.RootFolder.RegisterTaskDefinition(TaskName, definition);
    }

    public static void Uninstall()
    {
        using var ts = new TaskService();
        if (ts.GetTask(TaskName) is not null)
            ts.RootFolder.DeleteTask(TaskName);
    }

    /// <summary>Exe taşınmışsa görevdeki yolu günceller. Görev yoksa dokunmaz.</summary>
    public static void EnsurePathCurrent(string exePath)
    {
        var installedPath = InstalledExePath();
        if (installedPath is not null &&
            !string.Equals(installedPath, exePath, StringComparison.OrdinalIgnoreCase))
        {
            Install(exePath);
        }
    }
}
