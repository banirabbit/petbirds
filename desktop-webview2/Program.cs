using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Forms = System.Windows.Forms;

namespace PetbirdsLite
{
    internal sealed class Program : Application
    {
        private WebWindow toolbar;
        private WebWindow overlay;
        private StateController controller;
        private Forms.NotifyIcon tray;
        private DispatcherTimer monitor;
        private Mutex singleton;
        private bool closing;
        private readonly string[] args;

        private Program(string[] args) { this.args = args; ShutdownMode = ShutdownMode.OnExplicitShutdown; }

        [STAThread]
        public static void Main(string[] args)
        {
            var app = new Program(args);
            app.DispatcherUnhandledException += (_, e) => { e.Handled = true; app.Fail(e.Exception); };
            app.Run();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try
            {
                var mutexName = IsSmokeTest ? @"Local\PetbirdsLite.Smoke." + System.Diagnostics.Process.GetCurrentProcess().Id : @"Local\PetbirdsLite";
                singleton = new Mutex(true, mutexName, out var first);
                if (!first) { Shutdown(); return; }
                await Start();
            }
            catch (WebView2RuntimeNotFoundException)
            {
                Fail(new InvalidOperationException("需要安装 Microsoft Edge WebView2 Evergreen Runtime。\n官方下载：https://developer.microsoft.com/microsoft-edge/webview2/"));
            }
            catch (Exception error) { Fail(error); }
        }

        private async Task Start()
        {
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PetbirdsLite", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, data);
            toolbar = new WebWindow("toolbar.html", "Petbirds 轻量版工具条", 88, 403);
            overlay = new WebWindow("overlay.html", "Petbirds 轻量版互动层", 800, 600);
            // Create the native handles without showing a fullscreen input surface.
            new WindowInteropHelper(toolbar).EnsureHandle();
            new WindowInteropHelper(overlay).EnsureHandle();
            Native.ClickThrough(overlay, true);
            toolbar.Opacity = 0;
            overlay.Opacity = 0;
            toolbar.Show();
            overlay.Show();
            controller = new StateController(ShowOverlay, HideOverlay, Broadcast, SetToolbarVisible);
            toolbar.Message += OnMessage;
            overlay.Message += OnMessage;
            toolbar.Failed += Fail;
            overlay.Failed += Fail;
            await Task.WhenAll(toolbar.Initialize(environment), overlay.Initialize(environment));
            overlay.Hide();
            overlay.Opacity = 1;
            toolbar.Hide();
            toolbar.Opacity = 1;
            PlaceToolbar();
            controller.UpdateDesktop(Native.DesktopFocused(false));
            Broadcast(controller.Snapshot());
            if (IsSmokeTest)
            {
                var result = new { success = true, runtime = environment.BrowserVersionString, toolbarLoaded = toolbar.PageReady && toolbar.ScriptsReady, overlayLoaded = overlay.PageReady && overlay.ScriptsReady, transparentBackground = toolbar.TransparentBackground && overlay.TransparentBackground, desktopFocused = controller.Desktop, toolbarVisible = toolbar.IsVisible, toolbar = Native.Inspect(toolbar), overlay = Native.Inspect(overlay) };
                File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(result));
                Shutdown();
                return;
            }
            BuildTray();
            toolbar.Closing += (_, __) => { if (!closing) Shutdown(); };
            monitor = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            monitor.Tick += (_, __) =>
            {
                try
                {
                    controller.UpdateDesktop(Native.DesktopFocused(controller.Desktop));
                    if (controller.Mode != null && Native.EscapePressed) controller.Stop();
                }
                catch (Exception error) { Fail(error); }
            };
            monitor.Start();
        }

        private void ShowOverlay()
        {
            var work = Forms.Screen.FromHandle(Native.Handle(toolbar)).WorkingArea;
            Native.ClickThrough(overlay, false);
            overlay.Show();
            Native.Move(overlay, work.Left, work.Top, work.Width, work.Height);
            Native.Raise(toolbar);
        }

        private void HideOverlay()
        {
            if (overlay == null || overlay.IsClosed) return;
            Native.ClickThrough(overlay, true);
            overlay.Hide();
        }

        private void Broadcast(InteractionState state)
        {
            var payload = new { kind = "state", state };
            toolbar?.Send(payload);
            overlay?.Send(payload);
        }

        private bool IsSmokeTest => args.Length == 2 && args[0] == "--smoke-test";

        private void SetToolbarVisible(bool visible)
        {
            if (closing || toolbar == null || toolbar.IsClosed) return;
            if (!visible) { toolbar.Hide(); return; }
            toolbar.Show();
            Native.ShowWithoutActivating(toolbar);
            Native.Raise(toolbar);
        }

        private void RefreshToolbar()
        {
            controller.UpdateDesktop(Native.DesktopFocused(controller.Desktop));
            SetToolbarVisible(controller.Ready && controller.Desktop);
        }

        private void OnMessage(WebWindow sender, Dictionary<string, object> message)
        {
            if (!message.TryGetValue("kind", out var kind)) return;
            switch (kind as string)
            {
                case "state":
                    sender.Send(new { kind = "stateReply", id = message.TryGetValue("id", out var id) ? id : null, state = controller.Snapshot() });
                    break;
                case "stop": controller.Stop(); break;
                case "select":
                    if (sender == toolbar && message.TryGetValue("mode", out var mode))
                    {
                        controller.UpdateDesktop(Native.DesktopFocused(controller.Desktop));
                        controller.Start(mode as string);
                    }
                    break;
                case "resize":
                    if (sender == toolbar && message.TryGetValue("height", out var value) && double.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var height) && !double.IsNaN(height) && !double.IsInfinity(height))
                    {
                        toolbar.Height = Math.Max(80, Math.Min(height, 700));
                        ClampToolbar();
                    }
                    break;
                case "drag":
                    if (sender == toolbar && Native.LeftButtonPressed) { toolbar.DragMove(); ClampToolbar(); }
                    break;
                case "menu":
                    if (sender == toolbar) tray?.ContextMenuStrip.Show(Forms.Cursor.Position);
                    break;
            }
        }

        private void PlaceToolbar()
        {
            var work = Forms.Screen.PrimaryScreen.WorkingArea;
            var scale = Native.Scale(toolbar);
            var width = (int)Math.Ceiling(toolbar.Width * scale);
            var height = (int)Math.Ceiling(toolbar.Height * scale);
            Native.Move(toolbar, work.Right - width - 24, work.Top + Math.Max(0, (work.Height - height) / 2), width, Math.Min(height, work.Height));
        }

        private void ClampToolbar()
        {
            var work = Forms.Screen.FromHandle(Native.Handle(toolbar)).WorkingArea;
            var scale = Native.Scale(toolbar);
            var width = (int)Math.Ceiling(toolbar.Width * scale);
            var height = Math.Min((int)Math.Ceiling(toolbar.Height * scale), work.Height);
            var point = toolbar.PointToScreen(new Point(0, 0));
            Native.Move(toolbar, Math.Max(work.Left, Math.Min((int)point.X, work.Right - width)), Math.Max(work.Top, Math.Min((int)point.Y, work.Bottom - height)), width, height);
        }

        private void BuildTray()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("在桌面显示工具条", null, (_, __) => RefreshToolbar());
            menu.Items.Add("结束互动", null, (_, __) => controller.Stop());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出 Petbirds", null, (_, __) => Shutdown());
            tray = new Forms.NotifyIcon
            {
                Text = "Petbirds 轻量桌面版",
                Icon = new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robin.ico")),
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) RefreshToolbar(); };
        }

        private void Fail(Exception error)
        {
            if (closing) return;
            controller?.Unavailable();
            if (IsSmokeTest) File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(new { success = false, error = error.ToString() }));
            else MessageBox.Show("Petbirds 无法继续运行：\n" + error.Message, "Petbirds 轻量版");
            Shutdown(1);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            closing = true;
            monitor?.Stop();
            controller?.Stop();
            tray?.Dispose();
            if (toolbar != null && !toolbar.IsClosed) toolbar.Close();
            if (overlay != null && !overlay.IsClosed) overlay.Close();
            singleton?.Dispose();
            base.OnExit(e);
        }
    }
}
