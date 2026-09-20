using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TradeBlotter.Framework.WinAppDriver;

// HWND attachment and WebDriver window switching do not remove occlusion by the
// hosted runner console. Only change z-order for the process that this adapter owns.
internal sealed class OwnedWindow : IDisposable
{
    private readonly IntPtr handle;
    private readonly uint processId;
    private readonly bool wasTopmost;

    public OwnedWindow(IntPtr handle, int processId)
    {
        this.handle = handle;
        this.processId = (uint)processId;
        GetWindowThreadProcessId(handle, out var actualProcess);
        if (actualProcess != this.processId) throw new InvalidOperationException("Window is not owned by the launched SUT.");
        wasTopmost = (GetWindowLong(handle, -20) & 0x00000008) != 0;
        if (!SetWindowPos(handle, new IntPtr(-1), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot raise the owned SUT window.");
    }

    public void Dispose()
    {
        GetWindowThreadProcessId(handle, out var actualProcess);
        if (actualProcess == processId && !wasTopmost)
            SetWindowPos(handle, new IntPtr(-2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr window, int index);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
