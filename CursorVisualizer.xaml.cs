using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ScreenPresenterAssist
{
    public partial class CursorVisualizer : UserControl
    {
        private DispatcherTimer _timer;

        public CursorVisualizer()
        {
            InitializeComponent();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += (s, e) => UpdatePosition();
            _timer.Start();
        }

        public void SetHighlightVisible(bool visible)
        {
            HighlightRing.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetHighlightColor(Color color)
        {
            HighlightRing.Stroke = new SolidColorBrush(color);
        }

        private void UpdatePosition()
        {
            var pos = System.Windows.Forms.Control.MousePosition;
            // TODO: DPIスケーリングを考慮する必要がある
            // ここでは簡易的に設定
            Canvas.SetLeft(HighlightRing, pos.X - HighlightRing.Width / 2);
            Canvas.SetTop(HighlightRing, pos.Y - HighlightRing.Height / 2);
        }

        public void PlayRipple(System.Windows.Point position, System.Windows.Media.Brush color)
        {
            var ripple = new Ellipse
            {
                Width = 2,
                Height = 2,
                Stroke = color,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            VisualCanvas.Children.Add(ripple);
            
            // 初期の中心合わせ
            Canvas.SetLeft(ripple, position.X);
            Canvas.SetTop(ripple, position.Y);

            var duration = TimeSpan.FromMilliseconds(500);
            var sizeAnim = new DoubleAnimation(2, 100, duration);
            var opacityAnim = new DoubleAnimation(1, 0, duration);

            // 中心を維持するためのアニメーション（RenderTransformを使用）
            ripple.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
            var transform = new ScaleTransform(1, 1);
            ripple.RenderTransform = transform;

            // シンプルに左上の座標をずらしながら拡大する
            var leftAnim = new DoubleAnimation(position.X, position.X - 50, duration);
            var topAnim = new DoubleAnimation(position.Y, position.Y - 50, duration);

            opacityAnim.Completed += (s, e) => VisualCanvas.Children.Remove(ripple);

            ripple.BeginAnimation(Ellipse.WidthProperty, sizeAnim);
            ripple.BeginAnimation(Ellipse.HeightProperty, sizeAnim);
            ripple.BeginAnimation(Ellipse.OpacityProperty, opacityAnim);
            ripple.BeginAnimation(Canvas.LeftProperty, leftAnim);
            ripple.BeginAnimation(Canvas.TopProperty, topAnim);
        }
    }
}
