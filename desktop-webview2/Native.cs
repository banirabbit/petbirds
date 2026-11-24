using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace PetbirdsLite
{
    internal static class Native
    {
        private const int ExStyle = -20;
        private const int NoActivate = 0x08000000;
        private const int ToolWindow = 0x00000080;
        private const int Transparent = 0x00000020;
        private const int Layered = 0x00080000;
        private const int TopmostStyle = 0x00000008;
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLong(IntPtr window, int index, IntPtr value);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
        private static readonly uint OwnProcessId = (uint)Process.GetCurrentProcess().Id;
        private static readonly IntPtr Top = IntPtr.Zero;
        private static readonly IntPtr NotTopmost = new IntPtr(-2);

        public static IntPtr Handle(Window window) => new WindowInteropHelper(window).Handle;
        public static double Scale(Window window) => GetDpiForWindow(Handle(window)) / 96.0;

        public static void Configure(Window window)
        {
            var handle = Handle(window);
            var value = GetWindowLong(handle, ExStyle).ToInt64();
            SetWindowLong(handle, ExStyle, new IntPtr(value | NoActivate | ToolWindow));
            SetWindowPos(handle, NotTopmost, 0, 0, 0, 0, 0x0010 | 0x0001 | 0x0002);
        }

        public static void ClickThrough(Window window, bool enabled)
        {
            var handle = Handle(window);
            var value = GetWindowLong(handle, ExStyle).ToInt64();
            value = enabled ? value | Transparent : value & ~Transparent;
            SetWindowLong(handle, ExStyle, new IntPtr(value));
        }

        public static bool DesktopFocused(bool previous)
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            GetWindowThreadProcessId(foreground, out var processId);
            if (processId == OwnProcessId) return previous;
            var name = new StringBuilder(256);
            GetClassName(foreground, name, name.Capacity);
            return name.ToString() == "Progman" || name.ToString() == "WorkerW";
        }

        public static bool EscapePressed => (GetAsyncKeyState(0x1B) & 0x8000) != 0;
        public static bool LeftButtonPressed => (GetAsyncKeyState(0x01) & 0x8000) != 0;

        public static void Move(Window window, int x, int y, int width, int height)
        {
            SetWindowPos(Handle(window), Top, x, y, width, height, 0x0010);
        }
        public static void Raise(Window window) => SetWindowPos(Handle(window), Top, 0, 0, 0, 0, 0x0010 | 0x0001 | 0x0002);
        public static void ShowWithoutActivating(Window window) => ShowWindow(Handle(window), 4);

        public static object Inspect(Window window)
        {
            var style = GetWindowLong(Handle(window), ExStyle).ToInt64();
            return new { noActivate = (style & NoActivate) != 0, toolWindow = (style & ToolWindow) != 0, layered = (style & Layered) != 0, clickThrough = (style & Transparent) != 0, topmost = (style & TopmostStyle) != 0 };
        }
    }
}
