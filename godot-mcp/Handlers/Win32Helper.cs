#if TOOLS
using System.Runtime.InteropServices;

namespace GodotMCP.Handlers;

/// <summary>
/// Win32 interop for finding and interacting with the game window.
/// The Godot game runs as a separate child process, so all interaction
/// must go through OS-level APIs.
/// </summary>
public static class Win32Helper
{
    // --- Window enumeration ---

    public delegate bool EnumWindowsProc(System.IntPtr hWnd, System.IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, System.IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(System.IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(System.IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(System.IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(System.IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(System.IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern System.IntPtr GetForegroundWindow();

    // --- GDI for screenshots ---

    [DllImport("user32.dll")]
    public static extern System.IntPtr GetDC(System.IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(System.IntPtr hWnd, System.IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern System.IntPtr CreateCompatibleDC(System.IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern System.IntPtr CreateCompatibleBitmap(System.IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    public static extern System.IntPtr SelectObject(System.IntPtr hdc, System.IntPtr hgdiobj);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(System.IntPtr hwnd, System.IntPtr hdcBlt, uint nFlags);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(System.IntPtr hObject);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(System.IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern int GetDIBits(System.IntPtr hdc, System.IntPtr hbmp, uint uStartScan, uint cScanLines,
        byte[] lpvBits, ref BITMAPINFO lpbmi, uint uUsage);

    // --- Input injection ---

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern short VkKeyScan(char ch);

    // --- Structs ---

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public System.IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public System.IntPtr dwExtraInfo;
    }

    // --- Constants ---

    public const uint INPUT_KEYBOARD = 1;
    public const uint INPUT_MOUSE = 0;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    public const uint PW_RENDERFULLCONTENT = 2;

    // --- Key name to VK code mapping ---

    private static readonly System.Collections.Generic.Dictionary<string, ushort> KeyMap = new(System.StringComparer.OrdinalIgnoreCase)
    {
        // Letters
        {"A", 0x41}, {"B", 0x42}, {"C", 0x43}, {"D", 0x44}, {"E", 0x45},
        {"F", 0x46}, {"G", 0x47}, {"H", 0x48}, {"I", 0x49}, {"J", 0x4A},
        {"K", 0x4B}, {"L", 0x4C}, {"M", 0x4D}, {"N", 0x4E}, {"O", 0x4F},
        {"P", 0x50}, {"Q", 0x51}, {"R", 0x52}, {"S", 0x53}, {"T", 0x54},
        {"U", 0x55}, {"V", 0x56}, {"W", 0x57}, {"X", 0x58}, {"Y", 0x59}, {"Z", 0x5A},
        // Numbers
        {"0", 0x30}, {"1", 0x31}, {"2", 0x32}, {"3", 0x33}, {"4", 0x34},
        {"5", 0x35}, {"6", 0x36}, {"7", 0x37}, {"8", 0x38}, {"9", 0x39},
        // Special keys
        {"Space", 0x20}, {"Enter", 0x0D}, {"Return", 0x0D},
        {"Escape", 0x1B}, {"Esc", 0x1B},
        {"Tab", 0x09}, {"Backspace", 0x08}, {"Delete", 0x2E},
        {"Insert", 0x2D}, {"Home", 0x24}, {"End", 0x23},
        {"PageUp", 0x21}, {"PageDown", 0x22},
        // Arrow keys
        {"ArrowUp", 0x26}, {"ArrowDown", 0x28}, {"ArrowLeft", 0x25}, {"ArrowRight", 0x27},
        {"Up", 0x26}, {"Down", 0x28}, {"Left", 0x25}, {"Right", 0x27},
        // Modifiers
        {"Shift", 0x10}, {"Control", 0x11}, {"Ctrl", 0x11}, {"Alt", 0x12},
        // Function keys
        {"F1", 0x70}, {"F2", 0x71}, {"F3", 0x72}, {"F4", 0x73},
        {"F5", 0x74}, {"F6", 0x75}, {"F7", 0x76}, {"F8", 0x77},
        {"F9", 0x78}, {"F10", 0x79}, {"F11", 0x7A}, {"F12", 0x7B},
    };

    /// <summary>Map a key name string (e.g. "W", "Space", "Escape") to a Win32 VK code.</summary>
    public static ushort KeyNameToVk(string keyName)
    {
        if (KeyMap.TryGetValue(keyName, out var vk))
            return vk;
        // Single character: use its ASCII value (works for standard keys)
        if (keyName.Length == 1)
            return (ushort)char.ToUpper(keyName[0]);
        return 0;
    }

    /// <summary>
    /// Find the game window launched by the Godot editor.
    /// Searches for a visible window whose title contains the project name,
    /// excluding the editor's own windows.
    /// </summary>
    public static System.IntPtr FindGameWindow(string projectName, uint editorPid)
    {
        System.IntPtr found = System.IntPtr.Zero;
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == editorPid) return true;

            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, 256);
            var title = sb.ToString();
            if (title.Contains(projectName, System.StringComparison.OrdinalIgnoreCase))
            {
                found = hWnd;
                return false;
            }
            return true;
        }, System.IntPtr.Zero);
        return found;
    }
}
#endif
