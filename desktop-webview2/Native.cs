using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace PetbirdsLite
{
    internal static class Native
    {
        private const int ExStyle = -20, WindowStyle = -16;
        private const long Child = 0x40000000L, Popup = 0x80000000L, NoActivate = 0x08000000L;
        private const long ToolWindow = 0x80L, Transparent = 0x20L, Layered = 0x80000L, Topmost = 0x8L;
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLong(IntPtr window, int index, IntPtr value);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetParent(IntPtr window, IntPtr parent);
        [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
        private delegate bool EnumWindowCallback(IntPtr window, IntPtr data);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowCallback callback, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
        [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsChild(IntPtr parent, IntPtr child);
        [DllImport("user32.dll")] private static extern int MapWindowPoints(IntPtr from, IntPtr to, ref NativePoint point, uint count);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int count);

        public static IntPtr Handle(Window window) => new WindowInteropHelper(window).Handle;
        public static double Scale(Window window) => Math.Max(96, GetDpiForWindow(Handle(window))) / 96.0;
        public static bool EscapePressed => (GetAsyncKeyState(0x1B) & 0x8000) != 0;
        public static bool LeftButtonPressed => (GetAsyncKeyState(0x01) & 0x8000) != 0;

        private static IntPtr DesktopHost()
        {
            var shell = GetShellWindow();
            if (shell != IntPtr.Zero && FindWindowEx(shell, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero) return shell;
            var host = IntPtr.Zero;
            EnumWindows((window, data) =>
            {
                if (FindWindowEx(window, IntPtr.Zero, "SHELLDLL_DefView", null) == IntPtr.Zero) return true;
                host = window;
                return false;
            }, IntPtr.Zero);
            return host;
        }

        public static void Configure(Window window)
        {
            var handle = Handle(window);
            SetWindowLong(handle, ExStyle, new IntPtr(GetWindowLong(handle, ExStyle).ToInt64() | NoActivate | ToolWindow));
        }

        public static void AttachToDesktop(Window window)
        {
            var host = DesktopHost();
            if (host == IntPtr.Zero || !IsWindow(host)) throw new InvalidOperationException("找不到 Windows 桌面层。");
            var handle = Handle(window);
            var style = GetWindowLong(handle, WindowStyle).ToInt64() & 0xffffffffL;
            SetWindowLong(handle, WindowStyle, new IntPtr((style | Child) & ~Popup));
            SetParent(handle, host);
            if (GetAncestor(handle, 1) != host) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法将工具挂到桌面层。");
            if (!SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0010 | 0x0001 | 0x0002 | 0x0020)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public static bool IsDesktopChild(Window window)
        {
            var handle = Handle(window);
            var host = DesktopHost();
            return IsWindow(handle) && host != IntPtr.Zero && GetAncestor(handle, 1) == host && (GetWindowLong(handle, WindowStyle).ToInt64() & Child) != 0;
        }

        public static void ClickThrough(Window window, bool enabled)
        {
            var handle = Handle(window);
            var style = GetWindowLong(handle, ExStyle).ToInt64();
            SetWindowLong(handle, ExStyle, new IntPtr(enabled ? style | Transparent : style & ~Transparent));
        }

        public static void Move(Window window, int screenX, int screenY, int width, int height)
        {
            var point = new NativePoint { X = screenX, Y = screenY };
            if (IsDesktopChild(window)) MapWindowPoints(IntPtr.Zero, GetAncestor(Handle(window), 1), ref point, 1);
            if (!SetWindowPos(Handle(window), IntPtr.Zero, point.X, point.Y, width, height, 0x0010)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        public static void Raise(Window window) => SetWindowPos(Handle(window), IntPtr.Zero, 0, 0, 0, 0, 0x0010 | 0x0001 | 0x0002);
        public static void ShowWithoutActivating(Window window) => ShowWindow(Handle(window), 4);

        // Used only by the diagnostic mode, never to control widget visibility.
        public static bool DesktopFocused()
        {
            var root = GetAncestor(GetForegroundWindow(), 2);
            return root != IntPtr.Zero && (root == DesktopHost() || root == GetShellWindow());
        }

        public static System.Drawing.Rectangle Bounds(Window window)
        {
            GetWindowRect(Handle(window), out var rect);
            return System.Drawing.Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public static bool ToolbarReceivesInput(Window window)
        {
            var handle = Handle(window);
            GetWindowRect(handle, out var rect);
            var point = new NativePoint { X = (rect.Left + rect.Right) / 2, Y = rect.Top + Math.Min((rect.Bottom - rect.Top) / 2, (int)(62 * Scale(window))) };
            var hit = WindowFromPoint(point);
            return hit == handle || IsChild(handle, hit);
        }

        public static object Inspect(Window window)
        {
            var handle = Handle(window);
            var style = GetWindowLong(handle, ExStyle).ToInt64();
            var parent = GetAncestor(handle, 1);
            var name = new StringBuilder(256);
            GetClassName(parent, name, name.Capacity);
            GetWindowRect(handle, out var rect);
            return new { attachedToDesktop = IsDesktopChild(window), parentClass = name.ToString(), noActivate = (style & NoActivate) != 0, toolWindow = (style & ToolWindow) != 0, layered = (style & Layered) != 0, clickThrough = (style & Transparent) != 0, topmost = (style & Topmost) != 0, bounds = new { rect.Left, rect.Top, rect.Right, rect.Bottom } };
        }
    }
}
