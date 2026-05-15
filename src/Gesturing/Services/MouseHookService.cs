using System.Runtime.InteropServices;
using System.Threading.Channels;
using Gesturing.Models;
using Gesturing.Native;
using static Gesturing.Native.NativeMethods;

namespace Gesturing.Services;

public class MouseHookService : IDisposable
{
    private readonly Channel<HookEvent> _channel;
    private readonly HashSet<string> _targetProcesses;
    private Thread? _hookThread;
    private nint _hookHandle;
    private HookProc? _hookProc;
    private volatile bool _isEnabled;
    private volatile bool _inGesture;
    private volatile bool _disposed;

    public ChannelReader<HookEvent> Reader => _channel.Reader;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public MouseHookService(HashSet<string> targetProcesses)
    {
        _targetProcesses = targetProcesses;
        _channel = Channel.CreateBounded<HookEvent>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _isEnabled = true;
    }

    public void Start()
    {
        _hookThread = new Thread(HookThreadProc)
        {
            IsBackground = true,
            Name = "MouseHook"
        };
        _hookThread.Start();
    }

    public void Stop()
    {
        if (_hookHandle != nint.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = nint.Zero;
        }

        PostThreadMessage((uint)_hookThread!.ManagedThreadId, WM_QUIT, nint.Zero, nint.Zero);
        _hookThread = null;
        _channel.Writer.TryComplete();
    }

    public void UpdateTargetProcesses(HashSet<string> processes)
    {
        _targetProcesses.Clear();
        foreach (var p in processes)
        {
            _targetProcesses.Add(p);
        }
    }

    private void HookThreadProc()
    {
        var hMod = GetModuleHandle(null);
        _hookProc = HookCallback;
        _hookHandle = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, hMod, 0);

        while (GetMessage(out var msg, nint.Zero, 0, 0))
        {
            TranslateMessage(in msg);
            DispatchMessage(in msg);
        }

        if (_hookHandle != nint.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = nint.Zero;
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (!_isEnabled)
        {
            _inGesture = false;
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var msg = (uint)wParam;

        if (msg == WM_RBUTTONDOWN)
        {
            _inGesture = false;

            if (!ProcessHelper.IsForegroundProcessInList(_targetProcesses))
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            _inGesture = true;

            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var evt = new HookEvent(HookEventType.Down, hookStruct.pt.x, hookStruct.pt.y, hookStruct.time);
            _channel.Writer.TryWrite(evt);

            return 1; // suppress
        }

        if (msg == WM_RBUTTONUP || msg == WM_RBUTTONDBLCLK)
        {
            if (!_inGesture)
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            _inGesture = false;

            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var evt = new HookEvent(HookEventType.Up, hookStruct.pt.x, hookStruct.pt.y, hookStruct.time);
            _channel.Writer.TryWrite(evt);

            return 1; // suppress
        }

        if (msg == WM_MOUSEMOVE)
        {
            if (!_inGesture)
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            var evt = new HookEvent(HookEventType.Move, hookStruct.pt.x, hookStruct.pt.y, hookStruct.time);
            _channel.Writer.TryWrite(evt);

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
