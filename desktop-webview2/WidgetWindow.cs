using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;

namespace PetbirdsLite
{
    internal sealed class WidgetWindow : Window
    {
        private readonly StackPanel actions;
        private readonly Border bird;
        private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
        private System.Drawing.Point dragOrigin;
        private System.Drawing.Rectangle dragBounds;
        private bool dragging;
        public bool Expanded { get; private set; }
        public event Action<string> Selected;
        public event Action HideRequested;
        public event Action ExitRequested;

        public WidgetWindow()
        {
            Title = "Petbirds 知更鸟";
            Width = Height = 80;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            ShowActivated = false;
            Topmost = true;
            Icon = BitmapFrame.Create(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robin.ico")));
            SourceInitialized += (_, __) => Native.Configure(this);
            var root = new Grid();
            actions = new StackPanel { Margin = new Thickness(14, 8, 14, 0), VerticalAlignment = VerticalAlignment.Top, Visibility = Visibility.Collapsed, IsEnabled = false };
            foreach (var action in InteractionAction.All)
            {
                var button = ActionButton(action);
                button.Click += (_, __) => Selected?.Invoke(action.Id);
                buttons.Add(action.Id, button);
                actions.Children.Add(button);
            }
            root.Children.Add(actions);
            var image = new Image { Source = new BitmapImage(new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "robin.png"))), Stretch = Stretch.Uniform };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            bird = new Border { Width = 72, Height = 72, Margin = new Thickness(4), VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)), Child = image, Cursor = Cursors.Hand,
                ToolTip = "点击展开或收起 · 拖动移动 · 右键隐藏或退出" };
            AutomationProperties.SetName(bird, "知更鸟，展开或收起动作菜单");
            var menu = new ContextMenu();
            var hide = new MenuItem { Header = "隐藏" };
            hide.Click += (_, __) => HideRequested?.Invoke();
            var exit = new MenuItem { Header = "退出程序" };
            exit.Click += (_, __) => ExitRequested?.Invoke();
            menu.Items.Add(hide);
            menu.Items.Add(exit);
            bird.ContextMenu = menu;
            bird.MouseLeftButtonDown += BeginDrag;
            bird.MouseMove += MoveDrag;
            bird.MouseLeftButtonUp += EndDrag;
            bird.LostMouseCapture += (_, __) => dragging = false;
            root.Children.Add(bird);
            Content = root;
        }

        private static Button ActionButton(InteractionAction action)
        {
            var button = new Button { Width = 52, Height = 52, Margin = new Thickness(0, 4, 0, 4), ToolTip = action.Label,
                Background = Brush("#FFFBF7"), BorderBrush = Brush("#DDC9BF"), BorderThickness = new Thickness(1.5), Cursor = Cursors.Hand,
                Foreground = Brush("#66544C"), Focusable = false };
            AutomationProperties.SetName(button, action.Label);
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(26));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
            var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(BorderBrushProperty, Brush("#EE6594")));
            template.Triggers.Add(hover);
            button.Template = template;
            if (action.Id == "pet" || action.Id == "poke")
            {
                button.Content = new System.Windows.Shapes.Path { Data = Geometry.Parse(action.Id == "pet"
                    ? "M5 20 L6 13 C6 11 8 11 8 13 L9 8 C9 6 12 6 12 8 L11 13 L13 6 C14 4 16 5 15 7 L14 14 L16 9 C17 7 19 8 18 10 L17 15 C20 13 22 15 20 17 L16 21 Z"
                    : "M8 13 V4.5 C8 1.8 11.8 1.8 11.8 4.5 V9.5 C11.8 7.4 14.8 7.4 14.8 9.5 V10.5 C14.8 8.6 17.6 8.6 17.6 10.5 V11.5 C17.6 9.8 20.4 9.8 20.4 11.5 V15.2 C20.4 17.9 19 19.9 17.2 21.2 H9 C7.8 19.2 6.7 17.6 5.4 16.2 L3.9 14.6 C2.1 12.7 4.4 10.5 6.2 12.1 L8 14"),
                    Fill = Brushes.White, Stroke = Brush("#66544C"), StrokeThickness = 1.5, StrokeLineJoin = PenLineJoin.Round, Stretch = Stretch.Uniform, Width = 27, Height = 29 };
            }
            else button.Content = new TextBlock { Text = action.Id == "kiss" ? "♡" : action.Id == "shazi" ? "啥" : "㊗",
                FontSize = 27, FontWeight = FontWeights.Bold, Foreground = action.Id == "bless" ? Brush("#DD4848") : Brush("#66544C") };
            return button;
        }
        private static SolidColorBrush Brush(string color) => (SolidColorBrush)new BrushConverter().ConvertFromString(color);
        public void SetState(InteractionState state)
        {
            actions.IsEnabled = state.ready;
            bird.ToolTip = state.ready ? "点击展开或收起 · 拖动移动 · 右键隐藏或退出"
                : string.IsNullOrEmpty(state.message) ? "正在准备互动效果 · 右键可隐藏或退出" : state.message;
            foreach (var entry in buttons) entry.Value.Background = Brush(entry.Key == state.mode ? "#FFE2ED" : "#FFFBF7");
        }
        public void SetExpanded(bool expanded)
        {
            if (Expanded == expanded) return;
            var bounds = Native.Bounds(this);
            Expanded = expanded;
            actions.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            Height = expanded ? 400 : 80;
            var height = (int)Math.Round(Height * Native.Scale(this));
            MoveWithinScreen(bounds.Left, bounds.Bottom - height);
        }
        public void PlaceInitially()
        {
            var work = Forms.Screen.PrimaryScreen.WorkingArea;
            var size = (int)Math.Round(80 * Native.Scale(this));
            Native.Move(this, work.Right - size - 24, work.Top + (work.Height - size) / 2, size, size);
        }
        public void ClampPosition()
        {
            var bounds = Native.Bounds(this);
            MoveWithinScreen(bounds.Left, bounds.Top);
        }
        private void MoveWithinScreen(int x, int y)
        {
            var scale = Native.Scale(this);
            var width = (int)Math.Round(Width * scale);
            var height = (int)Math.Round(Height * scale);
            var work = Forms.Screen.FromPoint(new System.Drawing.Point(x + width / 2, y + height / 2)).WorkingArea;
            Native.Move(this, Math.Max(work.Left, Math.Min(x, work.Right - width)), Math.Max(work.Top, Math.Min(y, work.Bottom - height)), width, height);
        }
        private void BeginDrag(object sender, MouseButtonEventArgs e)
        {
            dragOrigin = Forms.Cursor.Position;
            dragBounds = Native.Bounds(this);
            dragging = false;
            bird.CaptureMouse();
            e.Handled = true;
        }
        private void MoveDrag(object sender, MouseEventArgs e)
        {
            if (!bird.IsMouseCaptured || e.LeftButton != MouseButtonState.Pressed) return;
            var cursor = Forms.Cursor.Position;
            var dx = cursor.X - dragOrigin.X;
            var dy = cursor.Y - dragOrigin.Y;
            if (!dragging && Math.Abs(dx) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(dy) < SystemParameters.MinimumVerticalDragDistance) return;
            dragging = true;
            Native.Move(this, dragBounds.Left + dx, dragBounds.Top + dy, dragBounds.Width, dragBounds.Height);
        }
        private void EndDrag(object sender, MouseButtonEventArgs e)
        {
            if (!bird.IsMouseCaptured) return;
            var moved = dragging;
            bird.ReleaseMouseCapture();
            if (moved) ClampPosition();
            else SetExpanded(!Expanded);
            e.Handled = true;
        }
    }
}
