using System.IO;
using System.Text.Json;

namespace Dwg2Pdf;

/// <summary>把用户设置(引擎路径、输出目录)持久化到 %AppData%\Dwg2Pdf\settings.json。</summary>
public static class Settings
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dwg2Pdf");
    private static readonly string FilePath = Path.Combine(Dir, "settings.json");

    public static string? EnginePath { get; set; }
    public static string? OutputDir { get; set; }

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return;
            var data = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(FilePath));
            if (data != null)
            {
                EnginePath = data.EnginePath;
                OutputDir = data.OutputDir;
            }
        }
        catch
        {
            // 设置损坏时忽略, 使用默认值
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var data = new SettingsData { EnginePath = EnginePath, OutputDir = OutputDir };
            File.WriteAllText(FilePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // 无法写入时静默失败
        }
    }

    private sealed class SettingsData
    {
        public string? EnginePath { get; set; }
        public string? OutputDir { get; set; }
    }
}
