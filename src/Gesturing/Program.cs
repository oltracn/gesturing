using System.Diagnostics;
using System.Runtime.InteropServices;
using Gesturing.Diagnostics;
using Gesturing.Models;
using Gesturing.Services;
using Gesturing.UI;

namespace Gesturing;

static partial class Program
{
    private const string MutexName = "Global\\Gesturing_SingleInstance";
    private const string ShowSettingsEventName = "Global\\Gesturing_ShowSettings";
    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _showSettingsEvent;
    private static TrayManager? _trayManager;
    private static MouseHookService? _hookService;
    private static GestureStateMachine? _gestureEngine;

    [STAThread]
    static void Main()
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            AllocConsole();
            ModuleValidator.RunAllTestsAsync().Wait();
            FreeConsole();
        }
#endif

        if (!EnsureSingleInstance())
        {
            return;
        }

        var config = ConfigManager.Load();

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Phase 6: Start gesture engine (before TrayManager so callback can reference it)
        var targetProcesses = new HashSet<string>(config.TargetProcesses, StringComparer.OrdinalIgnoreCase);
        _hookService = new MouseHookService(targetProcesses);
        _hookService.IsEnabled = config.IsEnabled;
        _gestureEngine = new GestureStateMachine(_hookService.Reader, config.Rules);

        _hookService.Start();
        _gestureEngine.Start();

        // config change callback: keep engine in sync
        Action onConfigChanged = () =>
        {
            _gestureEngine.Rules = config.Rules;
            _hookService.UpdateTargetProcesses(new HashSet<string>(
                config.TargetProcesses, StringComparer.OrdinalIgnoreCase));
        };

        _trayManager = new TrayManager(config, onConfigChanged);

        _trayManager.OnToggleEnable = enabled =>
        {
            _hookService.IsEnabled = enabled;
            config.IsEnabled = enabled;
            ConfigManager.Save(config);
        };

        StartListeningForShowSettings();

        if (config.Rules.Count == 0)
        {
            _trayManager.ShowSettings();
        }

        Application.Run();

        _gestureEngine.Dispose();
        _hookService.Dispose();
        _trayManager.Dispose();
    }

    private static bool EnsureSingleInstance()
    {
        _singleInstanceMutex = new Mutex(true, MutexName, out var createdNew);

        if (createdNew)
        {
            return true;
        }

        try
        {
            using var signal = EventWaitHandle.OpenExisting(ShowSettingsEventName);
            signal.Set();
        }
        catch
        {
            MessageBox.Show("Gesturing 已经在运行中。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        _singleInstanceMutex.Dispose();
        return false;
    }

    private static void StartListeningForShowSettings()
    {
        _showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);

        var thread = new Thread(() =>
        {
            while (true)
            {
                _showSettingsEvent.WaitOne();
                if (_trayManager != null)
                {
                    _trayManager.InvokeShowSettings();
                }
            }
        })
        {
            IsBackground = true
        };
        thread.Start();
    }

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();
}