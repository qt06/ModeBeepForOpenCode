using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModeBeep;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(Config))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}

internal sealed class Config
{
    /// <summary>Window title substrings that identify the opencode terminal.
    /// The title matches if it contains any of these (case-insensitive).</summary>
    public List<string> WindowTitleFilters { get; set; } = new() { "OC |", "Opencode" };

    /// <summary>Process names that host the opencode terminal window.</summary>
    public List<string> ProcessNames { get; set; } = new() { "WindowsTerminal" };

    /// <summary>Agent names considered as switchable modes.</summary>
    public List<string> Agents { get; set; } = new() { "plan", "build" };

    /// <summary>Per-agent sound files (WAV). Paths are relative to this file.</summary>
    public Dictionary<string, string> Sounds { get; set; } = new();

    /// <summary>Play a system sound when no file is configured for an agent.</summary>
    public bool FallbackSound { get; set; } = true;

    /// <summary>Delay after Tab before reading the mode, in milliseconds.</summary>
    public int DelayMs { get; set; } = 180;

    public static Config Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "config.json");
        if (!File.Exists(path))
        {
            return new Config();
        }

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(path), ConfigJsonContext.Default.Config) ?? new Config();
        }
        catch
        {
            return new Config();
        }
    }
}