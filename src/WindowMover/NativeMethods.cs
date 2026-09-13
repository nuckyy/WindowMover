using System.Runtime.InteropServices;
using System.Text;

namespace WindowMover;

internal static partial class NativeMethods
{
    internal static readonly nint MessageOnlyWindow = new(-3);
    internal static readonly nint InsertAfterTop = nint.Zero;

    internal const uint MonitorDefaultToNearest = 0x00000002;
    internal const uint SwpNoZOrder = 0x0004;
    internal const uint SwpNoActivate = 0x0010;
    internal const uint SwpNoOwnerZOrder = 0x0200;
    internal const int SwRestore = 9;
    internal const int SwMaximize = 3;
    internal const uint GaRoot = 2;
    internal const int DwmaCloaked = 14;

    internal delegate bool MonitorEnumProc(nint monitor, nint hdc, ref Rect monitorRect, nint data);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterHotKey(nint window, int id);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    internal static partial nint GetAncestor(nint window, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindow(nint window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(nint window);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint window, out Rect rect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowPlacement(nint window, ref WindowPlacement placement);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint window, int command);

    [LibraryImport("user32.dll")]
    internal static partial nint MonitorFromWindow(nint window, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumDisplayMonitors(
        nint hdc,
        nint clipRect,
        MonitorEnumProc callback,
        nint data);

    [LibraryImport("user32.dll")]
    internal static partial uint GetWindowThreadProcessId(nint window, out uint processId);

    [LibraryImport("user32.dll")]
    internal static partial uint GetDpiForWindow(nint window);

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    internal static extern int GetClassName(nint window, StringBuilder className, int maxCount);

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}

[StructLayout(LayoutKind.Sequential)]
internal struct Point
{
    internal int X;
    internal int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;

    internal readonly int Width => Right - Left;
    internal readonly int Height => Bottom - Top;
    internal readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    internal static Rect FromRectangle(Rectangle rectangle) => new()
    {
        Left = rectangle.Left,
        Top = rectangle.Top,
        Right = rectangle.Right,
        Bottom = rectangle.Bottom
    };
}

[StructLayout(LayoutKind.Sequential)]
internal struct MonitorInfo
{
    internal uint Size;
    internal Rect MonitorArea;
    internal Rect WorkArea;
    internal uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct WindowPlacement
{
    internal uint Length;
    internal uint Flags;
    internal uint ShowCommand;
    internal Point MinPosition;
    internal Point MaxPosition;
    internal Rect NormalPosition;
    internal Rect Device;
}
