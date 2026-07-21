using System.Text.Json;
using System.Text.Json.Serialization;

namespace FanPulse.Core.Config;

/// <summary>
/// fanpulse.json okuma/yazma. Taşınabilirlik için önce exe'nin yanı denenir;
/// klasör yazılabilir değilse %AppData%\FanPulse kullanılır.
/// </summary>
public static class ConfigStore
{
    public const string FileName = "fanpulse.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static string GetConfigPath()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (exeDir is not null)
        {
            var portablePath = Path.Combine(exeDir, FileName);
            if (File.Exists(portablePath) || IsWritable(exeDir))
                return portablePath;
        }

        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FanPulse");
        Directory.CreateDirectory(appData);
        return Path.Combine(appData, FileName);
    }

    public static AppConfig Load()
    {
        var path = GetConfigPath();
        if (!File.Exists(path))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch (JsonException)
        {
            // Bozuk config uygulamayı düşürmesin; yedeğini alıp temiz başla.
            File.Copy(path, path + ".bak", overwrite: true);
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        var path = GetConfigPath();
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static bool IsWritable(string directory)
    {
        try
        {
            var probe = Path.Combine(directory, ".fanpulse-write-test");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            return false;
        }
    }
}
