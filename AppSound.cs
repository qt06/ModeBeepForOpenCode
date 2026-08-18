using System.Runtime.InteropServices;

namespace ModeBeep;

/// <summary>
/// Plays the configured WAV for an agent (via winmm PlaySound), falling back to
/// a system sound when no file is configured or the file is missing.
/// </summary>
internal sealed class AppSound
{
    private const uint SND_FILENAME = 0x00020000;
    private const uint SND_ALIAS = 0x00010000;
    private const uint SND_ASYNC = 0x0001;

    private readonly Config _config;
    private readonly Dictionary<string, string> _sounds = new();

    public AppSound(Config config)
    {
        _config = config;

        foreach (var (agent, path) in config.Sounds)
        {
            var full = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
            if (File.Exists(full))
            {
                _sounds[agent.ToLowerInvariant()] = full;
                Log($"loaded sound {agent} <- {full}");
            }
            else
            {
                Log($"sound file missing for {agent}: {full}");
            }
        }
    }

    public void Play(string agent)
    {
        agent = agent.ToLowerInvariant();
        Log($"switch to {agent}");

        if (_sounds.TryGetValue(agent, out var path))
        {
            PlaySound(path, IntPtr.Zero, SND_FILENAME | SND_ASYNC);
            return;
        }

        if (_config.FallbackSound)
        {
            Log($"no sound for {agent}, playing fallback");
            PlaySound("SystemExclamation", IntPtr.Zero, SND_ALIAS | SND_ASYNC);
        }
    }

    private static void Log(string message)
    {
        // try
        // {
        //     var path = Path.Combine(AppContext.BaseDirectory, "modebeep.log");
        //     File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
        // }
        // catch
        // {
        //     // ignore logging failures
        // }
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string? pszSound, IntPtr hmod, uint fdwSound);
}
