using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PetbirdsLite
{
    internal static class Native
    {
        private const int ExStyle = -20;
        private const long NoActivate = 0x08000000L, ToolWindow = 0x80L, Transparent = 0x20L;
        private static readonly IntPtr Topmost = new IntPtr(-1);
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLong(IntPtr window, int index, IntPtr value);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
        [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);

        public static IntPtr Handle(Window window) => new WindowInteropHelper(window).Handle;
        public static double Scale(Window window) => Math.Max(96, GetDpiForWindow(Handle(window))) / 96.0;
        public static bool EscapePressed => (GetAsyncKeyState(0x1B) & 0x8000) != 0;

        public static void Configure(Window window)
        {
            var handle = Handle(window);
            SetWindowLong(handle, ExStyle, new IntPtr(GetWindowLong(handle, ExStyle).ToInt64() | NoActivate | ToolWindow));
        }
        public static void ClickThrough(Window window, bool enabled)
        {
            var handle = Handle(window);
            var style = GetWindowLong(handle, ExStyle).ToInt64();
            SetWindowLong(handle, ExStyle, new IntPtr(enabled ? style | Transparent : style & ~Transparent));
        }
        public static void Move(Window window, int x, int y, int width, int height)
        {
            if (!SetWindowPos(Handle(window), Topmost, x, y, width, height, 0x0010)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        public static void Raise(Window window) => SetWindowPos(Handle(window), Topmost, 0, 0, 0, 0, 0x0010 | 0x0001 | 0x0002);
        public static System.Drawing.Rectangle Bounds(Window window)
        {
            GetWindowRect(Handle(window), out var rect);
            return System.Drawing.Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
        public static bool ReceivesInput(Window window, int screenX, int screenY)
        {
            var handle = Handle(window);
            var hit = WindowFromPoint(new NativePoint { X = screenX, Y = screenY });
            return hit == handle || IsChild(handle, hit);
        }
        public static bool IsFloating(Window window)
        {
            var handle = Handle(window);
            return GetAncestor(handle, 2) == handle && (GetWindowLong(handle, -16).ToInt64() & 0x40000000L) == 0 && (GetWindowLong(handle, ExStyle).ToInt64() & 8) != 0;
        }
    }
}
