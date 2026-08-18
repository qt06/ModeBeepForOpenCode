using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Automation;

namespace ModeBeep;

/// <summary>
/// Determines whether the foreground window is the opencode terminal and reads
/// the currently active agent ("plan"/"build") from the TUI badge line via
/// UI Automation.
/// </summary>
internal sealed partial class ModeDetector
{
    private readonly Config _config;

    public ModeDetector(Config config)
    {
        _config = config;
    }

    /// <summary>
    /// Returns the opencode terminal's foreground window handle, or null when
    /// opencode is not the focused window.
    /// </summary>
    public IntPtr? GetOpencodeForegroundWindow()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
        {
            return null;
        }

        string name;
        try
        {
            name = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName;
        }
        catch
        {
            return null;
        }

        if (!_config.ProcessNames.Any(p => p.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        if (_config.WindowTitleFilters.Count > 0)
        {
            var title = GetWindowText(hwnd);
            if (!_config.WindowTitleFilters.Any(f => title.Contains(f, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }
        }

        return hwnd;
    }

    /// <summary>
    /// Returns the current agent name (lowercased) if opencode is focused and
    /// the badge can be read, otherwise null.
    /// </summary>
    public string? ReadCurrentAgent()
    {
        var hwnd = GetOpencodeForegroundWindow();
        if (hwnd is null)
        {
            return null;
        }

        var screenText = ReadScreenText(hwnd.Value);
        if (string.IsNullOrEmpty(screenText))
        {
            return null;
        }

        return ParseBadge(screenText);
    }

    private static string? ReadScreenText(IntPtr hwnd)
    {
        AutomationElement root;
        try
        {
            root = AutomationElement.FromHandle(hwnd);
        }
        catch
        {
            return null;
        }

        if (root.TryGetCurrentPattern(TextPattern.Pattern, out var rootPattern))
        {
            var text = ((TextPattern)rootPattern).DocumentRange.GetText(-1);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);
        string? best = null;
        try
        {
            var elements = root.FindAll(TreeScope.Descendants, condition);
            foreach (AutomationElement element in elements)
            {
                if (!element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
                {
                    continue;
                }

                try
                {
                    var text = ((TextPattern)pattern).DocumentRange.GetText(-1);
                    if (!string.IsNullOrEmpty(text) && (best is null || text.Length > best.Length))
                    {
                        best = text;
                    }
                }
                catch
                {
                    // skip controls that fail to expose text
                }
            }
        }
        catch
        {
            // fall through
        }

        return best;
    }

    /// <summary>
    /// Returns the raw UIA screen text of the focused opencode window (for
    /// diagnostics only).
    /// </summary>
    public string? DebugScreenText()
    {
        var hwnd = GetOpencodeForegroundWindow();
        return hwnd is null ? null : ReadScreenText(hwnd.Value);
    }

    private string? ParseBadge(string screenText)
    {
        var lines = screenText.Split('\n');
        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (!line.Contains('\u25CF') && !line.Contains('\u00B7'))
            {
                continue;
            }

            foreach (var agent in _config.Agents)
            {
                var match = AgentAdjacentDotRegex(agent).Match(line);
                if (match.Success)
                {
                    return agent.ToLowerInvariant();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Matches a whitelisted agent name immediately adjacent to a badge dot
    /// (either "Build ●" or "● Build"), which only occurs on the badge line.
    /// </summary>
    private static System.Text.RegularExpressions.Regex AgentAdjacentDotRegex(string agent)
    {
        var escaped = System.Text.RegularExpressions.Regex.Escape(agent);
        return new System.Text.RegularExpressions.Regex(
            $@"(?:\u25CF|\u00B7)\s*\b{escaped}\b|\b{escaped}\b\s*(?:\u25CF|\u00B7)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private const string BadgeDot = "\u25CF";

    private static string GetWindowText(IntPtr hwnd)
    {
        var sb = new StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
}
