using System.Runtime.InteropServices;

namespace ModeBeep;

internal static class Win32
{
    private const string U = "user32.dll";
    private const string S = "shell32.dll";

    [DllImport(U, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern short RegisterClassW(ref WNDCLASSW lpWndClass);

    [DllImport(U, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr CreateWindowExW(
        uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport(U, SetLastError = true)]
    internal static extern int DestroyWindow(IntPtr hWnd);

    [DllImport(U)]
    internal static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport(U)]
    internal static extern void PostQuitMessage(int nExitCode);

    [DllImport(U)]
    internal static extern int GetMessageW(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport(U)]
    internal static extern int TranslateMessage(ref MSG lpMsg);

    [DllImport(U)]
    internal static extern IntPtr DispatchMessageW(ref MSG lpMsg);

    [DllImport(U, SetLastError = true)]
    internal static extern IntPtr CreatePopupMenu();

    [DllImport(U, SetLastError = true)]
    internal static extern int DestroyMenu(IntPtr hMenu);

    [DllImport(U, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int AppendMenuW(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport(U, SetLastError = true)]
    internal static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport(U)]
    internal static extern int GetCursorPos(out POINT lpPoint);

    [DllImport(U)]
    internal static extern int SetForegroundWindow(IntPtr hWnd);

    [DllImport(U)]
    internal static extern IntPtr PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport(U, CharSet = CharSet.Unicode)]
    internal static extern IntPtr LoadImageW(IntPtr hInst, IntPtr name, uint type, int cx, int cy, uint fuLoad);

    [DllImport(U, SetLastError = true)]
    internal static extern int DestroyIcon(IntPtr hIcon);

    [DllImport(S, CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    internal static extern IntPtr GetModuleHandleW(string? lpModuleName);

    [DllImport(U)]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport(U)]
    internal static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

    [DllImport(U)]
    internal static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

    internal const uint IMAGE_ICON = 1;
    internal const uint LR_DEFAULTCOLOR = 0;
    internal const int IDI_APPLICATION = 32512;

    internal const uint WS_EX_TOOLWINDOW = 0x00000080;
    internal const uint WS_EX_NOACTIVATE = 0x08000000;
    internal const uint WS_OVERLAPPED = 0x00000000;

    internal const uint NIM_ADD = 0;
    internal const uint NIM_DELETE = 2;
    internal const uint NIM_SETVERSION = 4;
    internal const uint NIF_MESSAGE = 0x00000001;
    internal const uint NIF_ICON = 0x00000002;
    internal const uint NIF_TIP = 0x00000004;
    internal const uint NOTIFYICON_VERSION_4 = 4;

    internal const uint WM_DESTROY = 0x0002;
    internal const uint WM_COMMAND = 0x0111;
    internal const uint WM_TIMER = 0x0113;
    internal const uint WM_NULL = 0x0000;
    internal const uint WM_CONTEXTMENU = 0x007B;
    internal const uint WM_LBUTTONDOWN = 0x0201;
    internal const uint WM_RBUTTONDOWN = 0x0204;
    internal const uint TRAY_CALLBACK = 0x8001; // WM_APP + 1

    internal const uint MF_STRING = 0x00000000;
    internal const uint TPM_LEFTALIGN = 0x0000;
    internal const uint TPM_BOTTOMALIGN = 0x0020;
    internal const uint TPM_LEFTBUTTON = 0x0000;

    internal const uint ID_TEST = 1001;
    internal const uint ID_EXIT = 1002;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct WNDCLASSW
{
    public uint style;
    public IntPtr lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public IntPtr hInstance;
    public IntPtr hIcon;
    public IntPtr hCursor;
    public IntPtr hbrBackground;
    public string? lpszMenuName;
    public string lpszClassName;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int x, y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    public IntPtr hwnd;
    public uint message;
    public IntPtr wParam;
    public IntPtr lParam;
    public uint time;
    public POINT pt;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct NOTIFYICONDATAW
{
    public uint cbSize;
    public IntPtr hWnd;
    public uint uID;
    public uint uFlags;
    public uint uCallbackMessage;
    public IntPtr hIcon;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szTip;
    public uint dwState;
    public uint dwStateMask;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string szInfo;
    public uint uVersionOrTimeout;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string szInfoTitle;
    public uint dwInfoFlags;
    public Guid guidItem;
}

internal delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

internal static class Program
{
    private static readonly IntPtr ProbeTimerId = (IntPtr)1;
    private static readonly IntPtr BaselineTimerId = (IntPtr)2;

    private static readonly string WindowClass = "ModeBeepWin";
    private static readonly string TipText = "ModeBeep - opencode 模式切换提示音";
    private static readonly string MenuTextTest = "测试声音";
    private static readonly string MenuTextExit = "退出";

    private static Config _config = null!;
    private static ModeDetector _detector = null!;
    private static AppSound _sound = null!;
    private static KeyboardHook _hook = null!;

    private static IntPtr _hwnd;
    private static IntPtr _hIcon;
    private static IntPtr _hMenu;
    private static WndProcDelegate? _wndProcCallback;

    private static string? _lastAgent;
    private static bool _busy;
    private static bool _menuOpen;

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "--probe")
        {
            RunProbe();
            return;
        }

        if (args.Length > 0 && args[0] is "--test-sound")
        {
            var sound = new AppSound(Config.Load());
            foreach (var agent in Config.Load().Agents)
            {
                sound.Play(agent);
                Thread.Sleep(900);
            }

            return;
        }

        using var mutex = new Mutex(true, @"Local\ModeBeepSingleInstance", out var createdNew);
        if (!createdNew)
        {
            return;
        }

        try
        {
            _config = Config.Load();
            _detector = new ModeDetector(_config);
            _sound = new AppSound(_config);
            _hook = new KeyboardHook(OnTabPressed);

            _wndProcCallback = WndProc;
        var hInst = Win32.GetModuleHandleW(null);

        var wc = new WNDCLASSW
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcCallback),
            hInstance = hInst,
            lpszClassName = WindowClass,
        };
        var rc = Win32.RegisterClassW(ref wc);
        DLog($"RegisterClassW = {rc}, lastError={Marshal.GetLastWin32Error()}");

        _hwnd = Win32.CreateWindowExW(
            Win32.WS_EX_TOOLWINDOW | Win32.WS_EX_NOACTIVATE,
            WindowClass, "ModeBeep",
            Win32.WS_OVERLAPPED,
            0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero, hInst, IntPtr.Zero);
        DLog($"CreateWindowExW = {_hwnd}, lastError={Marshal.GetLastWin32Error()}");

        if (_hwnd == IntPtr.Zero)
        {
            return;
        }

        _hook.Install();
        SetupTrayIcon();
        SetupMenu();
        Win32.SetTimer(_hwnd, BaselineTimerId, 1500, IntPtr.Zero);

            var msg = new MSG();
            while (Win32.GetMessageW(out msg, IntPtr.Zero, 0, 0) > 0)
            {
                Win32.TranslateMessage(ref msg);
                Win32.DispatchMessageW(ref msg);
            }
        }
        catch (Exception ex)
        {
            DLog($"startup error: {ex}");
            return;
        }

        DLog("message loop exited");
        Cleanup();
        DLog("process exiting");
    }

    internal static void DLog(string message)
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

    private static void SetupTrayIcon()
    {
        _hIcon = Win32.LoadImageW(IntPtr.Zero, (IntPtr)Win32.IDI_APPLICATION,
            Win32.IMAGE_ICON, 16, 16, Win32.LR_DEFAULTCOLOR);

        var nid = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = 1,
            uFlags = Win32.NIF_MESSAGE | Win32.NIF_ICON | Win32.NIF_TIP,
            uCallbackMessage = Win32.TRAY_CALLBACK,
            hIcon = _hIcon,
            szTip = TipText,
            szInfo = "",
            szInfoTitle = "",
        };

        DLog($"NID size={nid.cbSize}");
        var addResult = Win32.Shell_NotifyIconW(Win32.NIM_ADD, ref nid);
        DLog($"NIM_ADD result={addResult} lastError={Marshal.GetLastWin32Error()}");

        var ver = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hwnd,
            uID = 1,
            uVersionOrTimeout = Win32.NOTIFYICON_VERSION_4,
        };
        var verResult = Win32.Shell_NotifyIconW(Win32.NIM_SETVERSION, ref ver);
        DLog($"NIM_SETVERSION result={verResult} lastError={Marshal.GetLastWin32Error()}");
    }

    private static void SetupMenu()
    {
        _hMenu = Win32.CreatePopupMenu();
        Win32.AppendMenuW(_hMenu, Win32.MF_STRING, Win32.ID_TEST, MenuTextTest);
        Win32.AppendMenuW(_hMenu, Win32.MF_STRING, Win32.ID_EXIT, MenuTextExit);
    }

    private static void ShowMenu()
    {
        if (_menuOpen)
        {
            return;
        }

        _menuOpen = true;
        try
        {
            Win32.GetCursorPos(out var pt);
            var prevFg = Win32.GetForegroundWindow();
            Win32.SetForegroundWindow(_hwnd);
            Win32.TrackPopupMenu(_hMenu,
                Win32.TPM_LEFTALIGN | Win32.TPM_BOTTOMALIGN | Win32.TPM_LEFTBUTTON,
                pt.x, pt.y, 0, _hwnd, IntPtr.Zero);
            Win32.PostMessageW(_hwnd, Win32.WM_NULL, IntPtr.Zero, IntPtr.Zero);
            if (prevFg != IntPtr.Zero && prevFg != _hwnd)
            {
                Win32.SetForegroundWindow(prevFg);
            }
        }
        finally
        {
            _menuOpen = false;
        }
    }

    private static void Cleanup()
    {
        if (_hwnd != IntPtr.Zero)
        {
            var nid = new NOTIFYICONDATAW
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
                hWnd = _hwnd,
                uID = 1,
            };
            Win32.Shell_NotifyIconW(Win32.NIM_DELETE, ref nid);
        }

        if (_hMenu != IntPtr.Zero)
        {
            Win32.DestroyMenu(_hMenu);
            _hMenu = IntPtr.Zero;
        }

        if (_hIcon != IntPtr.Zero)
        {
            Win32.DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }

        _hook.Dispose();

        if (_hwnd != IntPtr.Zero)
        {
            Win32.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case Win32.WM_DESTROY:
                DLog("WndProc: WM_DESTROY, PostQuitMessage");
                Win32.PostQuitMessage(0);
                return IntPtr.Zero;

            case Win32.WM_TIMER:
                if (wParam == ProbeTimerId)
                {
                    Win32.KillTimer(hWnd, ProbeTimerId);
                    _busy = false;
                    TrySwitchMode();
                }
                else if (wParam == BaselineTimerId)
                {
                    Win32.KillTimer(hWnd, BaselineTimerId);
                    _lastAgent = _detector.ReadCurrentAgent();
                }

                return IntPtr.Zero;

            case Win32.WM_COMMAND:
                var id = (uint)(int)wParam;
                DLog($"WndProc: WM_COMMAND id={id}");
                if (id == Win32.ID_TEST)
                {
                    foreach (var agent in _config.Agents)
                    {
                        _sound.Play(agent);
                        Thread.Sleep(700);
                    }
                }
                else if (id == Win32.ID_EXIT)
                {
                    Win32.PostMessageW(_hwnd, Win32.WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
                }

                return IntPtr.Zero;

            default:
                if (msg == Win32.TRAY_CALLBACK)
                {
                    var evt = (int)lParam & 0xFFFF;
                    if (evt == Win32.WM_LBUTTONDOWN || evt == Win32.WM_CONTEXTMENU)
                    {
                        DLog($"TRAY_CALLBACK evt=0x{evt:X} -> show menu");
                        ShowMenu();
                    }

                    return IntPtr.Zero;
                }

                break;
        }

        return Win32.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private static void OnTabPressed()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        Win32.KillTimer(_hwnd, ProbeTimerId);
        Win32.SetTimer(_hwnd, ProbeTimerId, (uint)Math.Max(100, _config.DelayMs), IntPtr.Zero);
    }

    private static void TrySwitchMode()
    {
        var agent = _detector.ReadCurrentAgent();
        if (agent is null)
        {
            DLog("TrySwitchMode: agent not detected (foreground not opencode, or no readable badge)");
            return;
        }

        if (string.Equals(agent, _lastAgent, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastAgent = agent;
        _sound.Play(agent);
    }

    /// <summary>
    /// Debug helper: prints the currently detected agent (or "(none)") and
    /// writes it to %TEMP%\modebeep-probe.txt.
    /// </summary>
    private static void RunProbe()
    {
        var probePath = Path.Combine(Path.GetTempPath(), "modebeep-probe.txt");
        var report = new System.Text.StringBuilder();

        void Flush()
        {
            try
            {
                File.WriteAllText(probePath, report.ToString());
            }
            catch
            {
                // ignore
            }
        }

        try
        {
            var config = Config.Load();
            var detector = new ModeDetector(config);

            report.AppendLine($"baseDir={AppContext.BaseDirectory}");
            report.AppendLine($"configPath={Path.Combine(AppContext.BaseDirectory, "config.json")} exists={File.Exists(Path.Combine(AppContext.BaseDirectory, "config.json"))}");
            report.AppendLine($"soundsCount={config.Sounds.Count}");
            Flush();

            var hwnd = detector.GetOpencodeForegroundWindow();
            report.AppendLine($"foregroundHwnd={hwnd?.ToString() ?? "null"}");
            report.AppendLine($"filters={string.Join(" | ", config.WindowTitleFilters)}");
            Flush();

            var agent = detector.ReadCurrentAgent();
            report.AppendLine($"agent={agent ?? "(none)"}");
            Flush();

            var raw = detector.DebugScreenText();
            report.AppendLine("--- raw screen text (first 3000) ---");
            report.AppendLine(raw is null ? "(null)" : raw.Length > 3000 ? raw.Substring(0, 3000) : raw);
            Flush();
        }
        catch (Exception ex)
        {
            report.AppendLine($"error: {ex}");
            Flush();
        }

        if (AttachConsole(0xFFFFFFFF))
        {
            try
            {
                Console.WriteLine(report.ToString());
            }
            finally
            {
                FreeConsole();
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();
}
