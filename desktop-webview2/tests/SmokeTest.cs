using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PetbirdsLite
{
    internal static class SmokeTest
    {
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        public static async Task Run(WidgetWindow widget, WebWindow overlay, StateController controller, Action restore, string output)
        {
            await Task.Delay(250);
            Check(!widget.Expanded && widget.IsVisible && !overlay.IsVisible, "启动应只显示小鸟");
            Check(Native.IsFloating(widget), "小鸟必须为独立置顶窗口");
            var bounds = Native.Bounds(widget);
            Check(Native.ReceivesInput(widget, bounds.Left + bounds.Width / 2, bounds.Bottom - bounds.Width / 2), "小鸟必须实际接收屏幕命中");
            Capture(widget, output + ".bird.png");
            widget.SetExpanded(true);
            await Task.Delay(100);
            Check(widget.Expanded && widget.Height == 400, "菜单应展开");
            Capture(widget, output + ".menu.png");
            var root = (Grid)widget.Content;
            var actions = (StackPanel)root.Children[0];
            var bird = (Border)root.Children[1];
            Check(actions.Children.Count == 5, "菜单应保留五个动作");
            var tested = 0;
            foreach (Button button in actions.Children)
            {
                widget.SetExpanded(true);
                await Task.Delay(50);
                var point = button.PointToScreen(new Point(button.ActualWidth / 2, button.ActualHeight / 2));
                Check(button.IsVisible && button.IsEnabled && Native.ReceivesInput(widget, (int)point.X, (int)point.Y), "动作按钮必须可见并接收屏幕命中");
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(100);
                Check(controller.Mode == InteractionAction.All[tested].Id && overlay.IsVisible && !widget.Expanded, "选择动作应进入互动并收起菜单");
                var area = Native.Bounds(overlay);
                Check(Native.ReceivesInput(overlay, area.Left + 100, area.Top + 100), "互动层必须实际接收屏幕命中");
                var particles = await overlay.Core.ExecuteScriptAsync("document.dispatchEvent(new PointerEvent('pointerdown',{button:0,isPrimary:true,clientX:200,clientY:200}));document.querySelector('#effects').childElementCount");
                Check(int.Parse(particles) > 0, "点击互动层必须生成特效");
                await overlay.Core.ExecuteScriptAsync("document.dispatchEvent(new MouseEvent('contextmenu',{bubbles:true,cancelable:true}))");
                await Task.Delay(100);
                Check(controller.Mode == null && !overlay.IsVisible, "右键必须撤掉互动层");
                tested++;
            }
            controller.Start("pet");
            ((MenuItem)bird.ContextMenu.Items[0]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Check(!widget.IsVisible && !overlay.IsVisible && controller.Mode == null, "隐藏必须同时结束互动");
            restore();
            await Task.Delay(100);
            Check(widget.IsVisible && !widget.Expanded && Native.IsFloating(widget), "托盘恢复必须只显示小鸟");
            File.WriteAllText(output, new JavaScriptSerializer().Serialize(new { success = true, birdReceivesInput = true, actionsTested = tested, rightClickStops = true, hideAndRestore = true, rendererReady = overlay.PageReady && overlay.ScriptsReady }));
            ((MenuItem)bird.ContextMenu.Items[1]).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }
        private static void Capture(Window window, string filename)
        {
            window.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(filename)) encoder.Save(stream);
        }
    }
}
