using System.Drawing;
using System.Runtime.InteropServices;

namespace Gesturing.UI;

public class StyledTextBox : TextBox
{
    private static readonly Color BorderNormal = Color.FromArgb(200, 200, 204);
    private static readonly Color BorderFocused = Color.FromArgb(42, 137, 190);

    private bool _isFocused;

    public StyledTextBox()
    {
        BorderStyle = BorderStyle.None;
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
            case WM_NCPAINT:
                WmNcPaint(ref m);
                return;
            case WM_NCCALCSIZE:
                WmNcCalcSize(ref m);
                return;
            case WM_NCHITTEST:
                m.Result = (IntPtr)1;
                return;
        }
        base.WndProc(ref m);
    }

    private void WmNcCalcSize(ref Message m)
    {
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
            var color = _isFocused ? BorderFocused : BorderNormal;
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

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS { public RECT rect0, rect1, rect2; public IntPtr lppos; }

    private const uint WM_NCPAINT = 0x0085;
    private const uint WM_NCCALCSIZE = 0x0083;
    private const uint WM_NCHITTEST = 0x0084;
    private const uint RDW_FRAME = 0x0400;
    private const uint RDW_INVALIDATE = 0x0001;
    private const uint RDW_UPDATENOW = 0x0100;
}