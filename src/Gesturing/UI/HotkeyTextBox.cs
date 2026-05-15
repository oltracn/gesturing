using System.Drawing;
using System.Runtime.InteropServices;
using Gesturing.Native;
using Gesturing.Services;
using static Gesturing.Native.NativeMethods;

namespace Gesturing.UI;

public class HotkeyTextBox : TextBox
{
    private string _key = string.Empty;
    private readonly List<string> _modifiers = new();

    private static readonly Color BorderColor = Color.FromArgb(42, 137, 190);
    private static readonly Color BorderFocused = Color.FromArgb(52, 157, 210);
    private static readonly Color BorderMuted = Color.FromArgb(200, 200, 204);
    private static readonly Color PlaceholderColor = Color.FromArgb(120, 120, 128);
    private static readonly Color TextColor = Color.FromArgb(28, 28, 32);

    private bool _isFocused;

    public string CapturedKey => _key;
    public IReadOnlyList<string> CapturedModifiers => _modifiers;
    public bool HasCapture => _key.Length > 0;

    public HotkeyTextBox()
    {
        ReadOnly = true;
        Cursor = Cursors.Default;
        BorderStyle = BorderStyle.None;
        ResetPlaceholder();
    }

    public void SetFromHotkey(string key, List<string> modifiers)
    {
        _key = key;
        _modifiers.Clear();
        _modifiers.AddRange(modifiers);
        UpdateDisplay();
    }

    public void ResetCapture()
    {
        _key = string.Empty;
        _modifiers.Clear();
        ResetPlaceholder();
    }

    private void ResetPlaceholder()
    {
        ForeColor = PlaceholderColor;
        Text = "点击此处按下快捷键";
    }

    private void UpdateDisplay()
    {
        ForeColor = TextColor;
        Text = _modifiers.Count == 0
            ? _key
            : string.Join(" + ", _modifiers) + " + " + _key;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        _isFocused = true;
        InvalidateNonClient();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        _isFocused = false;
        InvalidateNonClient();
        base.OnLostFocus(e);
    }

    protected override void WndProc(ref Message m)
    {
        switch ((uint)m.Msg)
        {
            case WM_KEYDOWN or WM_SYSKEYDOWN:
                HandleKeyDown(ref m);
                return;

            case WM_NCPAINT:
                if (BorderStyle != BorderStyle.None)
                    break;
                WmNcPaint(ref m);
                return;

            case WM_NCCALCSIZE:
                if (BorderStyle != BorderStyle.None)
                    break;
                WmNcCalcSize(ref m);
                return;

            case WM_NCHITTEST:
                if (BorderStyle != BorderStyle.None)
                    break;
                m.Result = (IntPtr)1; // HTCLIENT — treat border as client
                return;
        }

        base.WndProc(ref m);
    }

    private void HandleKeyDown(ref Message m)
    {
        var vkCode = (ushort)m.WParam.ToInt32();

        if (IsModifier(vkCode))
            return;

        _modifiers.Clear();
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0) _modifiers.Add("Ctrl");
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0) _modifiers.Add("Shift");
        if ((GetKeyState(VK_MENU) & 0x8000) != 0) _modifiers.Add("Alt");
        if (((GetKeyState(VK_LWIN) & 0x8000) != 0) || ((GetKeyState(VK_RWIN) & 0x8000) != 0))
            _modifiers.Add("Win");

        _key = Services.VirtualKeyMapper.ToKeyName(vkCode);

        if (_key.Length == 0 || _key.StartsWith("0x"))
        {
            _key = string.Empty;
            _modifiers.Clear();
            ResetPlaceholder();
            return;
        }

        UpdateDisplay();
    }

    private void WmNcCalcSize(ref Message m)
    {
        // Shrink client area by 1px on each side for our custom border
        var calc = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(m.LParam);
        calc.rect0.Left += 1;
        calc.rect0.Top += 1;
        calc.rect0.Right -= 1;
        calc.rect0.Bottom -= 1;
        Marshal.StructureToPtr(calc, m.LParam, false);
        m.Result = IntPtr.Zero;
    }

    private void WmNcPaint(ref Message m)
    {
        var hdc = GetWindowDC(m.HWnd);
        try
        {
            var rect = new RECT();
            GetWindowRect(m.HWnd, ref rect);
            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;

            using var g = Graphics.FromHdc(hdc);
            var color = _isFocused ? BorderFocused : (HasCapture ? BorderColor : BorderMuted);
            using var pen = new Pen(color, 1);
            g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
        }
        finally
        {
            ReleaseDC(m.HWnd, hdc);
        }
        m.Result = IntPtr.Zero;
    }

    private void InvalidateNonClient()
    {
        RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero,
            RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW);
    }

    private static bool IsModifier(ushort vk)
    {
        return vk is VK_CONTROL or VK_SHIFT or VK_MENU
            or VK_LCONTROL or VK_RCONTROL
            or VK_LSHIFT or VK_RSHIFT
            or VK_LMENU or VK_RMENU
            or VK_LWIN or VK_RWIN;
    }

    // ── Native interop for custom border ──

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        public RECT rect0, rect1, rect2;
        public IntPtr lppos;
    }

    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_NCPAINT = 0x0085;
    private const uint WM_NCCALCSIZE = 0x0083;
    private const uint WM_NCHITTEST = 0x0084;

    private const uint RDW_FRAME = 0x0400;
    private const uint RDW_INVALIDATE = 0x0001;
    private const uint RDW_UPDATENOW = 0x0100;
}
