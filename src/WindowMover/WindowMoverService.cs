using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowMover;

internal enum MoveStatus
{
    Success,
    NoForegroundWindow,
    OwnWindow,
    UnsupportedWindow,
    OneMonitor,
    AccessDenied,
    Failed
}

internal sealed record MoveResult(MoveStatus Status, string Message)
{
    internal bool IsSuccess => Status == MoveStatus.Success;
}

internal sealed class WindowMoverService
{
    private static readonly HashSet<string> ShellWindowClasses = new(StringComparer.Ordinal)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd"
    };

    public MoveResult MoveForegroundWindowToNextMonitor()
    {
        return MoveWindowToNextMonitor(NativeMethods.GetForegroundWindow());
    }

    internal MoveResult MoveWindowToNextMonitor(nint window)
    {
        if (window == nint.Zero || !NativeMethods.IsWindow(window) || !NativeMethods.IsWindowVisible(window))
        {
            return new MoveResult(MoveStatus.NoForegroundWindow, Localization.Text("NoForegroundWindow"));
        }

        window = NativeMethods.GetAncestor(window, NativeMethods.GaRoot);
        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return new MoveResult(MoveStatus.OwnWindow, Localization.Text("OwnWindow"));
        }

        if (IsUnsupportedWindow(window))
        {
            return new MoveResult(MoveStatus.UnsupportedWindow, Localization.Text("UnsupportedWindow"));
        }

        var monitors = GetMonitors();
        if (monitors.Count < 2)
        {
            return new MoveResult(MoveStatus.OneMonitor, Localization.Text("OneMonitor"));
        }

        var sourceHandle = NativeMethods.MonitorFromWindow(window, NativeMethods.MonitorDefaultToNearest);
        var sourceIndex = monitors.FindIndex(monitor => monitor.Handle == sourceHandle);
        if (sourceIndex < 0)
        {
            return new MoveResult(MoveStatus.Failed, Localization.Text("SourceMonitorNotFound"));
        }

        if (!NativeMethods.GetWindowRect(window, out var nativeBounds))
        {
            return FromLastWin32Error(Localization.Text("WindowLocationReadFailed"));
        }

        var placement = new WindowPlacement { Length = (uint)Marshal.SizeOf<WindowPlacement>() };
        if (!NativeMethods.GetWindowPlacement(window, ref placement))
        {
            return FromLastWin32Error(Localization.Text("WindowStateReadFailed"));
        }

        var source = monitors[sourceIndex];
        var destination = monitors[(sourceIndex + 1) % monitors.Count];
        var wasMaximized = placement.ShowCommand == NativeMethods.SwMaximize;
        var destinationBounds = wasMaximized
            ? destination.WorkArea
            : GeometryMapper.MapToMonitor(nativeBounds.ToRectangle(), source.WorkArea, destination.WorkArea);

        if (wasMaximized)
        {
            NativeMethods.ShowWindow(window, NativeMethods.SwRestore);
        }

        var moved = NativeMethods.SetWindowPos(
            window,
            NativeMethods.InsertAfterTop,
            destinationBounds.X,
            destinationBounds.Y,
            destinationBounds.Width,
            destinationBounds.Height,
            NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate | NativeMethods.SwpNoOwnerZOrder);

        if (!moved)
        {
            if (wasMaximized)
            {
                NativeMethods.ShowWindow(window, NativeMethods.SwMaximize);
            }

            return FromLastWin32Error(Localization.Text("WindowMoveFailed"));
        }

        if (wasMaximized)
        {
            NativeMethods.ShowWindow(window, NativeMethods.SwMaximize);
        }

        return new MoveResult(MoveStatus.Success, Localization.Text("MoveSuccess"));
    }

    private static bool IsUnsupportedWindow(nint window)
    {
        if (NativeMethods.DwmGetWindowAttribute(
                window,
                NativeMethods.DwmaCloaked,
                out var cloaked,
                sizeof(int)) == 0 && cloaked != 0)
        {
            return true;
        }

        var className = new StringBuilder(256);
        return NativeMethods.GetClassName(window, className, className.Capacity) > 0
            && ShellWindowClasses.Contains(className.ToString());
    }

    private static List<DisplayMonitor> GetMonitors()
    {
        var monitors = new List<DisplayMonitor>();
        Exception? callbackException = null;
        NativeMethods.MonitorEnumProc callback = (nint handle, nint hdc, ref Rect monitorRect, nint data) =>
        {
            try
            {
                var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
                if (NativeMethods.GetMonitorInfo(handle, ref info))
                {
                    monitors.Add(new DisplayMonitor(handle, info.MonitorArea.ToRectangle(), info.WorkArea.ToRectangle()));
                }

                return true;
            }
            catch (Exception exception)
            {
                callbackException = exception;
                return false;
            }
        };

        var enumerated = NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);
        if (callbackException is not null)
        {
            throw new InvalidOperationException(Localization.Text("MonitorInfoReadFailed"), callbackException);
        }

        if (!enumerated)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), Localization.Text("MonitorEnumerationFailed"));
        }

        return monitors
            .OrderBy(monitor => monitor.Bounds.Left)
            .ThenBy(monitor => monitor.Bounds.Top)
            .ToList();
    }

    private static MoveResult FromLastWin32Error(string fallbackMessage)
    {
        var error = Marshal.GetLastWin32Error();
        if (error == 5)
        {
            return new MoveResult(
                MoveStatus.AccessDenied,
                Localization.Text("AccessDenied"));
        }

        var details = error == 0 ? fallbackMessage : $"{fallbackMessage} ({new Win32Exception(error).Message})";
        return new MoveResult(MoveStatus.Failed, details);
    }

    private sealed record DisplayMonitor(nint Handle, Rectangle Bounds, Rectangle WorkArea);
}
