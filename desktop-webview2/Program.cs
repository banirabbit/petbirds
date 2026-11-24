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
        private WebWindow toolbar, overlay;
        private StateController controller;
        private Forms.NotifyIcon tray;
        private DispatcherTimer monitor;
        private DispatcherTimer dragTimer;
        private System.Drawing.Point dragOrigin;
        private System.Drawing.Rectangle dragBounds;
        private Mutex singleton;
        private bool closing;
        private readonly string[] args;
        private bool IsSmokeTest => args.Length == 2 && args[0] == "--smoke-test";
        private bool IsDesktopTest => args.Length == 2 && args[0] == "--desktop-test";
        private bool IsDiagnostic => IsSmokeTest || IsDesktopTest;

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
                var name = IsDiagnostic ? @"Local\PetbirdsLite.Test." + System.Diagnostics.Process.GetCurrentProcess().Id : @"Local\PetbirdsLite";
                singleton = new Mutex(true, name, out var first);
                if (!first) { Shutdown(); return; }
                await Start();
            }
            catch (WebView2RuntimeNotFoundException)
            {
                Fail(new InvalidOperationException("需要 Microsoft Edge WebView2 Evergreen Runtime。\n官方下载：https://developer.microsoft.com/microsoft-edge/webview2/"));
            }
            catch (Exception error) { Fail(error); }
        }

        private async Task Start()
        {
            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PetbirdsLite", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, data);
            toolbar = new WebWindow("toolbar.html", "Petbirds 桌面挂件", 88, 403);
            overlay = new WebWindow("overlay.html", "Petbirds 桌面特效", 800, 600);
            new WindowInteropHelper(toolbar).EnsureHandle();
            new WindowInteropHelper(overlay).EnsureHandle();
            Native.ClickThrough(overlay, true);
            toolbar.Opacity = 0;
            overlay.Opacity = 0;
            toolbar.Show();
            overlay.Show();
            Native.AttachToDesktop(toolbar);
            Native.AttachToDesktop(overlay);
            controller = new StateController(ShowOverlay, HideOverlay, Broadcast);
            toolbar.Message += OnMessage;
            overlay.Message += OnMessage;
            toolbar.Failed += Fail;
            overlay.Failed += Fail;
            await Task.WhenAll(toolbar.Initialize(environment), overlay.Initialize(environment));
            overlay.Hide();
            overlay.Opacity = 1;
            toolbar.Opacity = 1;
            PlaceToolbar();
            Native.ShowWithoutActivating(toolbar);
            Native.Raise(toolbar);
            controller.Initialize();
            if (IsSmokeTest)
            {
                await Task.Delay(500);
                var attached = Native.IsDesktopChild(toolbar) && Native.IsDesktopChild(overlay);
                controller.Start("pet");
                var overlayReshownAttached = Native.IsDesktopChild(overlay);
                controller.Stop();
                var rendering = await toolbar.CaptureDiagnostic(args[1]);
                WriteDiagnostic(new { success = attached && overlayReshownAttached && rendering.visiblePixels > 1000, runtime = environment.BrowserVersionString, toolbarLoaded = toolbar.PageReady && toolbar.ScriptsReady, overlayLoaded = overlay.PageReady && overlay.ScriptsReady, overlayReshownAttached, rendering, desktopFocused = Native.DesktopFocused(), toolbarVisible = toolbar.IsVisible, inputHitsToolbar = Native.ToolbarReceivesInput(toolbar), toolbar = Native.Inspect(toolbar), overlay = Native.Inspect(overlay) });
                Shutdown();
                return;
            }
            if (!IsDesktopTest) BuildTray();
            toolbar.Closing += (_, __) => { if (!closing) Shutdown(); };
            monitor = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            monitor.Tick += (_, __) =>
            {
                try
                {
                    if (!Native.IsDesktopChild(toolbar) || !Native.IsDesktopChild(overlay)) throw new InvalidOperationException("Windows 桌面层已重建，请重新启动 Petbirds。");
                    if (controller.Mode != null && Native.EscapePressed) controller.Stop();
                }
                catch (Exception error) { Fail(error); }
            };
            monitor.Start();
            if (IsDesktopTest) await VerifyDesktopRoundTrip();
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
            var message = new { kind = "state", state };
            toolbar?.Send(message);
            overlay?.Send(message);
        }
        private void ShowToolbar()
        {
            toolbar.Show();
            Native.ShowWithoutActivating(toolbar);
            Native.Raise(toolbar);
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
                    if (sender == toolbar && message.TryGetValue("mode", out var mode)) controller.Start(mode as string);
                    break;
                case "resize":
                    if (sender == toolbar && message.TryGetValue("height", out var value) && double.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var height) && !double.IsNaN(height) && !double.IsInfinity(height))
                    {
                        toolbar.Height = Math.Max(80, Math.Min(height, 700));
                        ClampToolbar();
                    }
                    break;
                case "drag":
                    if (sender == toolbar && Native.LeftButtonPressed) StartDragging();
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
            var height = Math.Min((int)Math.Ceiling(toolbar.Height * scale), work.Height);
            Native.Move(toolbar, work.Right - width - 24, work.Top + Math.Max(0, (work.Height - height) / 2), width, height);
        }
        private void StartDragging()
        {
            dragOrigin = Forms.Cursor.Position;
            dragBounds = Native.Bounds(toolbar);
            if (dragTimer == null)
            {
                dragTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                dragTimer.Tick += (_, __) =>
                {
                    if (!Native.LeftButtonPressed) { dragTimer.Stop(); ClampToolbar(); return; }
                    var cursor = Forms.Cursor.Position;
                    Native.Move(toolbar, dragBounds.Left + cursor.X - dragOrigin.X, dragBounds.Top + cursor.Y - dragOrigin.Y, dragBounds.Width, dragBounds.Height);
                };
            }
            dragTimer.Start();
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
            menu.Items.Add("显示桌面挂件", null, (_, __) => ShowToolbar());
            menu.Items.Add("结束互动", null, (_, __) => controller.Stop());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出 Petbirds", null, (_, __) => Shutdown());
            tray = new Forms.NotifyIcon
            {
                Text = "Petbirds 桌面挂件",
                Icon = new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robin.ico")),
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) ShowToolbar(); };
        }
        private async Task VerifyDesktopRoundTrip()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            object desktop = null, application = null;
            while (!closing && timer.Elapsed.TotalSeconds < 75)
            {
                if (Native.DesktopFocused() && Native.ToolbarReceivesInput(toolbar)) desktop = new { receivesInput = true, toolbar = Native.Inspect(toolbar) };
                if (desktop != null && !Native.DesktopFocused() && toolbar.IsVisible && Native.IsDesktopChild(toolbar) && !Native.ToolbarReceivesInput(toolbar))
                {
                    application = new { stillInDesktopLayer = true, coveredByApplication = true, toolbar = Native.Inspect(toolbar) };
                    break;
                }
                await Task.Delay(100);
            }
            if (closing) return;
            WriteDiagnostic(new { success = desktop != null && application != null, desktop, application });
            Shutdown();
        }
        private void WriteDiagnostic(object result) => File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(result));
        private void Fail(Exception error)
        {
            if (closing) return;
            controller?.Unavailable();
            if (IsDiagnostic) WriteDiagnostic(new { success = false, error = error.ToString() });
            else MessageBox.Show("Petbirds 无法继续运行：\n" + error.Message, "Petbirds 桌面挂件");
            Shutdown(1);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            closing = true;
            monitor?.Stop();
            dragTimer?.Stop();
            controller?.Stop();
            tray?.Dispose();
            if (toolbar != null && !toolbar.IsClosed) toolbar.Close();
            if (overlay != null && !overlay.IsClosed) overlay.Close();
            singleton?.Dispose();
            base.OnExit(e);
        }
    }
}
