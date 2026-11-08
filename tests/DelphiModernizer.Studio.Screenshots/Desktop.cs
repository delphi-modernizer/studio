using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DelphiModernizer.Studio.Screenshots;

/// <summary>
/// The Win32 calls the capture needs: window frames, the display mode, Windows' theme, high contrast and the pointer.
/// </summary>
internal static partial class Desktop
{
    private const int _extendedFrameBounds = 9;
    private const uint _noZOrder = 0x0004;
    private const uint _noActivate = 0x0010;
    private const int _currentSettings = -1;
    private const int _pelsWidthAndHeight = 0x00080000 | 0x00100000;
    private const uint _getHighContrast = 0x0042;
    private const uint _setHighContrast = 0x0043;
    private const int _highContrastOn = 0x0001;
    private const uint _sendChange = 0x0002;
    private const uint _mouseInput = 0;
    private const uint _moveAbsolute = 0x0001 | 0x8000;
    private const uint _leftDown = 0x0002;
    private const uint _leftUp = 0x0004;
    private const int _screenWidth = 0;
    private const int _screenHeight = 1;
    private const string _personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string _appsUseLightTheme = "AppsUseLightTheme";

    public static string DisplayMode
    {
        get {
            var mode = CurrentMode();

            return $"{mode.PelsWidth} x {mode.PelsHeight}";
        }
    }

    public static bool IsHighContrast
    {
        get {
            var contrast = new HighContrast { Size = Marshal.SizeOf<HighContrast>() };

            return SystemParametersInfo(_getHighContrast, (uint)contrast.Size, ref contrast, 0)
                && (contrast.Flags & _highContrastOn) != 0;
        }
    }

    /// <summary>Whether apps that follow Windows draw light, as Settings, Personalization, Colors chooses.</summary>
    /// <remarks>
    /// Null where the person never chose, which Windows reads as light. An app reads it as it starts.
    /// </remarks>
    public static bool? AppsUseLightTheme
    {
        get {
            using var key = Registry.CurrentUser.OpenSubKey(_personalize);

            return key?.GetValue(_appsUseLightTheme) is int value ? value != 0 : null;
        }
        set {
            using var key = Registry.CurrentUser.CreateSubKey(_personalize);

            if (value is { } light)
            {
                key.SetValue(_appsUseLightTheme, light ? 1 : 0, RegistryValueKind.DWord);
            }
            else
            {
                key.DeleteValue(_appsUseLightTheme, throwOnMissingValue: false);
            }
        }
    }

    /// <summary>The window as the person sees it: the DWM frame, without the invisible resize borders.</summary>
    public static Rectangle VisibleFrame(nint window)
    {
        int result = DwmGetWindowAttribute(window, _extendedFrameBounds, out var frame, Marshal.SizeOf<Rect>());
        Marshal.ThrowExceptionForHR(result);

        return frame.ToRectangle();
    }

    /// <summary>Moves and sizes the window so that its visible frame is exactly the size asked for.</summary>
    public static Rectangle SetVisibleFrame(nint window, Point origin, Size size)
    {
        if (!GetWindowRect(window, out var outer))
        {
            throw new InvalidOperationException("The window has no rectangle.");
        }

        var visible = VisibleFrame(window);
        var bounds = outer.ToRectangle();
        int left = visible.Left - bounds.Left;
        int top = visible.Top - bounds.Top;
        int width = size.Width + bounds.Width - visible.Width;
        int height = size.Height + bounds.Height - visible.Height;

        if (!SetWindowPos(window, 0, origin.X - left, origin.Y - top, width, height, _noZOrder | _noActivate))
        {
            throw new InvalidOperationException($"SetWindowPos failed with {Marshal.GetLastPInvokeError()}.");
        }

        return VisibleFrame(window);
    }

    /// <returns>Whether the primary display now shows at least the size asked for.</returns>
    public static bool EnsureDisplayMode(int width, int height)
    {
        var mode = CurrentMode();

        if (mode.PelsWidth >= width && mode.PelsHeight >= height)
        {
            return true;
        }

        mode.PelsWidth = width;
        mode.PelsHeight = height;
        mode.Fields = _pelsWidthAndHeight;

        return ChangeDisplaySettings(ref mode, 0) == 0;
    }

    /// <returns>Whether high contrast is in the state asked for once the call returns.</returns>
    public static bool SetHighContrast(bool on)
    {
        var contrast = new HighContrast { Size = Marshal.SizeOf<HighContrast>() };

        if (!SystemParametersInfo(_getHighContrast, (uint)contrast.Size, ref contrast, 0))
        {
            return false;
        }

        contrast.Flags = on ? contrast.Flags | _highContrastOn : contrast.Flags & ~_highContrastOn;
        SystemParametersInfo(_setHighContrast, (uint)contrast.Size, ref contrast, _sendChange);

        return IsHighContrast == on;
    }

    /// <summary>Moves the pointer as a mouse does, so the app sees pointer input and not only a new cursor position.</summary>
    /// <remarks>XAML takes its pointer-over states and tool tips from pointer input, which SetCursorPos does not send.</remarks>
    public static void MovePointer(Point to)
    {
        int width = GetSystemMetrics(_screenWidth);
        int height = GetSystemMetrics(_screenHeight);
        Send(new MouseInput {
            X = (int)Math.Round(to.X * 65535.0 / (width - 1)),
            Y = (int)Math.Round(to.Y * 65535.0 / (height - 1)),
            Flags = _moveAbsolute,
        });
    }

    /// <summary>Clicks the left button at a point, as a mouse does.</summary>
    /// <remarks>After pointer input XAML draws no focus ring where focus moves next; after keyboard input it does.</remarks>
    public static void Click(Point at)
    {
        MovePointer(at);
        Send(new MouseInput { Flags = _leftDown });
        Send(new MouseInput { Flags = _leftUp });
    }

    private static void Send(MouseInput mouse)
    {
        var input = new Input { Type = _mouseInput, Mouse = mouse };

        if (SendInput(1, ref input, Marshal.SizeOf<Input>()) != 1)
        {
            throw new InvalidOperationException($"SendInput failed with {Marshal.GetLastPInvokeError()}.");
        }
    }

    private static DevMode CurrentMode()
    {
        var mode = new DevMode { Size = (short)Marshal.SizeOf<DevMode>() };

        return EnumDisplaySettings(null, _currentSettings, ref mode)
            ? mode
            : throw new InvalidOperationException("The display mode cannot be read.");
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmGetWindowAttribute(nint window, int attribute, out Rect value, int size);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint window, out Rect rect);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "EnumDisplaySettingsW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplaySettings(string? device, int mode, ref DevMode devMode);

    [LibraryImport("user32.dll", EntryPoint = "ChangeDisplaySettingsW")]
    private static partial int ChangeDisplaySettings(ref DevMode devMode, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint parameter, ref HighContrast value, uint update);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint SendInput(uint count, ref Input input, int size);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly Rectangle ToRectangle()
        {
            return Rectangle.FromLTRB(Left, Top, Right, Bottom);
        }
    }

    /// <remarks>DEVMODEW is 220 bytes; only the fields a display-mode change reads are declared.</remarks>
    [StructLayout(LayoutKind.Explicit, Size = 220)]
    private struct DevMode
    {
        [FieldOffset(68)]
        public short Size;

        [FieldOffset(72)]
        public int Fields;

        [FieldOffset(172)]
        public int PelsWidth;

        [FieldOffset(176)]
        public int PelsHeight;
    }

    /// <remarks>INPUT with its MOUSEINPUT member, the largest of its union.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint Data;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HighContrast
    {
        public int Size;
        public int Flags;
        public nint DefaultScheme;
    }
}
