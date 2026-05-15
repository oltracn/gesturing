using Gesturing.Native;

namespace Gesturing.Services;

public static class ProcessHelper
{
    private static readonly Dictionary<nint, string> _windowProcessCache = new();
    private const uint MaxPathLength = 260;
    private const int MaxCacheSize = 100;

    public static string? GetForegroundProcessName()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        if (hWnd == nint.Zero)
        {
            return null;
        }

        if (_windowProcessCache.TryGetValue(hWnd, out var cachedName))
        {
            return cachedName;
        }

        var processName = GetProcessNameByWindow(hWnd);
        if (processName != null)
        {
            if (_windowProcessCache.Count >= MaxCacheSize)
            {
                _windowProcessCache.Clear();
            }
            _windowProcessCache[hWnd] = processName;
        }

        return processName;
    }

    public static bool IsForegroundProcessInList(HashSet<string> processNames)
    {
        var foregroundProcess = GetForegroundProcessName();
        return foregroundProcess != null && processNames.Contains(foregroundProcess);
    }

    public static void ClearCache()
    {
        _windowProcessCache.Clear();
    }

    private static string? GetProcessNameByWindow(nint hWnd)
    {
        _ = NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        var hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (hProcess == nint.Zero)
        {
            return null;
        }

        try
        {
            var buffer = new char[MaxPathLength];
            uint size = MaxPathLength;
            if (NativeMethods.QueryFullProcessImageName(hProcess, 0, buffer, ref size))
            {
                var fullPath = new string(buffer, 0, (int)size);
                return Path.GetFileName(fullPath);
            }

            return null;
        }
        finally
        {
            NativeMethods.CloseHandle(hProcess);
        }
    }
}
