using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace PetbirdsLite
{
    internal sealed class WebWindow : Window
    {
        public const string Origin = "https://petbirds.local";
        private readonly string page;
        private readonly WebView2CompositionControl view;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        public event Action<WebWindow, Dictionary<string, object>> Message;
        public event Action<Exception> Failed;
        public bool PageReady { get; private set; }
        public bool ScriptsReady { get; private set; }
        public bool TransparentBackground => view.DefaultBackgroundColor.A == 0;
        public bool IsClosed { get; private set; }
        public CoreWebView2 Core => view.CoreWebView2;

        public WebWindow(string page, string title, double width, double height)
        {
            this.page = page;
            Title = title;
            Width = width;
            Height = height;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = false;
            Icon = BitmapFrame.Create(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robin.ico")));
            view = new WebView2CompositionControl { DefaultBackgroundColor = System.Drawing.Color.Transparent };
            Content = view;
            SourceInitialized += (_, __) => Native.Configure(this);
            Closed += (_, __) => { IsClosed = true; view.Dispose(); };
        }

        public async Task Initialize(CoreWebView2Environment environment)
        {
            var initializing = view.EnsureCoreWebView2Async(environment);
            if (await Task.WhenAny(initializing, Task.Delay(20000)) != initializing) throw new TimeoutException("WebView2 初始化超时。");
            await initializing;
            view.DefaultBackgroundColor = System.Drawing.Color.Transparent;
            var settings = Core.Settings;
            settings.AreDevToolsEnabled = false;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
            settings.AreHostObjectsAllowed = false;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = false;
            Core.SetVirtualHostNameToFolderMapping("petbirds.local", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"), CoreWebView2HostResourceAccessKind.DenyCors);
            Core.NewWindowRequested += (_, e) => e.Handled = true;
            Core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
            Core.NavigationStarting += (_, e) => { if (e.Uri != Origin + "/ui/" + page) e.Cancel = true; };
            Core.ProcessFailed += (_, e) => Failed?.Invoke(new InvalidOperationException("WebView2 进程退出：" + e.ProcessFailedKind));
            var scripts = new TaskCompletionSource<bool>();
            Core.WebMessageReceived += (_, e) =>
            {
                if (e.Source != Origin + "/ui/" + page) return;
                try
                {
                    var message = json.Deserialize<Dictionary<string, object>>(e.WebMessageAsJson);
                    if (message != null && message.TryGetValue("kind", out var kind) && kind as string == "ready") { ScriptsReady = true; scripts.TrySetResult(true); }
                    if (message != null) Message?.Invoke(this, message);
                }
                catch (ArgumentException) { }
            };
            var loaded = new TaskCompletionSource<bool>();
            Core.NavigationCompleted += (_, e) =>
            {
                if (e.IsSuccess) { PageReady = true; loaded.TrySetResult(true); }
                else loaded.TrySetException(new InvalidOperationException("界面加载失败：" + e.WebErrorStatus));
            };
            Core.Navigate(Origin + "/ui/" + page);
            var complete = Task.WhenAll(loaded.Task, scripts.Task);
            if (await Task.WhenAny(complete, Task.Delay(20000)) != complete) throw new TimeoutException("WebView2 界面或脚本加载超时。");
            await complete;
        }

        public void Send(object message)
        {
            if (!IsClosed && Core != null) Core.PostWebMessageAsJson(json.Serialize(message));
        }

        public async Task<RenderEvidence> CaptureDiagnostic(string prefix)
        {
            using (var stream = File.Create(prefix + ".web.png"))
                await Core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            UpdateLayout();
            var width = Math.Max(1, (int)Math.Ceiling(view.ActualWidth));
            var height = Math.Max(1, (int)Math.Ceiling(view.ActualHeight));
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(view);
            var pixels = new byte[width * height * 4];
            bitmap.CopyPixels(pixels, width * 4, 0);
            var visiblePixels = 0;
            for (var i = 3; i < pixels.Length; i += 4) if (pixels[i] > 32) visiblePixels++;
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(prefix + ".wpf.png")) encoder.Save(stream);
            return new RenderEvidence { width = width, height = height, visiblePixels = visiblePixels };
        }
    }

    internal sealed class RenderEvidence
    {
        public int width { get; set; }
        public int height { get; set; }
        public int visiblePixels { get; set; }
    }
}
