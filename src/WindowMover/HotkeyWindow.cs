using System.Runtime.InteropServices;

namespace WindowMover;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private const int HotkeyId = 0x4D57;
    private const int WmHotkey = 0x0312;
    private bool _registered;
    private bool _disposed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams
        {
            Caption = "WindowMover.HotkeyWindow",
            Parent = NativeMethods.MessageOnlyWindow
        });
    }

    public event EventHandler? Pressed;

    public Hotkey? Current { get; private set; }

    public void Register(Hotkey hotkey)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(hotkey);

        if (!hotkey.IsValid)
        {
            throw new ArgumentException(Localization.Text("InvalidHotkeyArgument"), nameof(hotkey));
        }

        Unregister();

        if (!NativeMethods.RegisterHotKey(Handle, HotkeyId, hotkey.NativeModifiers, (uint)hotkey.Key))
        {
            throw new HotkeyRegistrationException(hotkey);
        }

        Current = hotkey;
        _registered = true;
    }

    public void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(Handle, HotkeyId);
        _registered = false;
        Current = null;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey && message.WParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Unregister();
        DestroyHandle();
        _disposed = true;
    }
}
