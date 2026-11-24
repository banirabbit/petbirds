using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Forms = System.Windows.Forms;

namespace PetbirdsLite
{
    internal sealed class Program : Application
    {
        private WidgetWindow widget;
        private WebWindow overlay;
        private StateController controller;
        private Forms.NotifyIcon tray;
        private DispatcherTimer escapeTimer;
        private Mutex singleton;
        private bool closing;
        private readonly string[] args;
        private bool IsSelfTest => args.Length == 2 && args[0] == "--self-test";

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
                var name = IsSelfTest ? @"Local\PetbirdsLite.Test." + System.Diagnostics.Process.GetCurrentProcess().Id : @"Local\PetbirdsLite";
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
            widget = new WidgetWindow();
            overlay = new WebWindow();
            controller = new StateController(ShowOverlay, HideOverlay, Broadcast);
            widget.Selected += mode => { controller.Start(mode); widget.SetExpanded(false); };
            widget.HideRequested += HideWidget;
            widget.ExitRequested += () => Shutdown();
            widget.Closing += (_, __) => { if (!closing) Shutdown(); };
            overlay.Message += OnMessage;
            overlay.Failed += Fail;
            widget.Show();
            widget.PlaceInitially();
            BuildTray();

            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PetbirdsLite", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(null, data);
            if (closing) return;
            new WindowInteropHelper(overlay).EnsureHandle();
            Native.ClickThrough(overlay, true);
            overlay.Opacity = 0;
            overlay.Show();
            await overlay.Initialize(environment);
            if (closing) return;
            overlay.Hide();
            overlay.Opacity = 1;
            controller.Initialize();
            Native.Raise(widget);
            escapeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            escapeTimer.Tick += (_, __) => { if (controller.Mode != null && Native.EscapePressed) controller.Stop(); };
            escapeTimer.Start();
            if (IsSelfTest)
            {
                await SmokeTest.Run(widget, overlay, controller, ShowWidget, args[1]);
                Shutdown();
            }
        }

        private void ShowOverlay()
        {
            var work = Forms.Screen.FromHandle(Native.Handle(widget)).WorkingArea;
            Native.ClickThrough(overlay, false);
            overlay.Show();
            Native.Move(overlay, work.Left, work.Top, work.Width, work.Height);
            Native.Raise(widget);
        }
        private void HideOverlay()
        {
            if (overlay == null || overlay.IsClosed || Native.Handle(overlay) == IntPtr.Zero) return;
            Native.ClickThrough(overlay, true);
            overlay.Hide();
        }
        private void Broadcast(InteractionState state)
        {
            widget?.SetState(state);
            overlay?.Send(new { kind = "state", state });
            if (tray != null) tray.Text = state.ready ? "Petbirds · 点击小鸟选择动作" : "Petbirds · 互动效果正在准备";
        }
        private void HideWidget()
        {
            controller.Stop();
            widget.SetExpanded(false);
            widget.Hide();
        }
        private void ShowWidget()
        {
            widget.Show();
            widget.ClampPosition();
            Native.Raise(widget);
        }
        private void OnMessage(WebWindow sender, Dictionary<string, object> message)
        {
            if (message.TryGetValue("kind", out var kind) && kind as string == "stop") controller.Stop();
        }
        private void BuildTray()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("显示知更鸟", null, (_, __) => ShowWidget());
            menu.Items.Add("隐藏知更鸟", null, (_, __) => HideWidget());
            menu.Items.Add("结束互动", null, (_, __) => controller.Stop());
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出程序", null, (_, __) => Shutdown());
            tray = new Forms.NotifyIcon
            {
                Text = "Petbirds · 互动效果正在准备",
                Icon = new System.Drawing.Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robin.ico")),
                ContextMenuStrip = menu,
                Visible = true
            };
            tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) ShowWidget(); };
        }
        private void Fail(Exception error)
        {
            if (closing) return;
            controller?.Unavailable();
            if (IsSelfTest) File.WriteAllText(args[1], new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new { success = false, error = error.ToString() }));
            else MessageBox.Show("Petbirds 无法继续运行：\n" + error.Message, "Petbirds 知更鸟");
            Shutdown(1);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            closing = true;
            escapeTimer?.Stop();
            controller?.Stop();
            tray?.Dispose();
            widget?.Close();
            if (overlay != null && !overlay.IsClosed) overlay.Close();
            singleton?.Dispose();
            base.OnExit(e);
        }
    }
}
