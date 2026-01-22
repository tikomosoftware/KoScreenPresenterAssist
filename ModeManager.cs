namespace ScreenPresenterAssist
{
    public enum AppMode
    {
        Normal,     // 操作モード（透過）
        Drawing,    // 描画モード
        Highlight,  // 強調表示モード
        Magnify     // 拡大モード
    }

    public class ModeManager
    {
        private readonly OverlayWindow _window;
        public AppMode CurrentMode { get; private set; } = AppMode.Normal;
        
        public event System.Action<AppMode>? ModeChanged;

        public ModeManager(OverlayWindow window)
        {
            _window = window;
        }

        public void SetMode(AppMode mode)
        {
            // 同じモードならNormalに戻す（トグル動作）
            if (CurrentMode == mode && mode != AppMode.Normal)
            {
                mode = AppMode.Normal;
            }

            CurrentMode = mode;
            UpdateWindow();
            ModeChanged?.Invoke(CurrentMode);
        }

        private void UpdateWindow()
        {
            _window.SetCursorHighlight(CurrentMode == AppMode.Highlight);
            _window.SetMagnifierActive(CurrentMode == AppMode.Magnify);

            switch (CurrentMode)
            {
                case AppMode.Normal:
                    _window.SetInputTransparent(true);
                    _window.ShapeCanvas.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case AppMode.Drawing:
                    _window.SetInputTransparent(false);
                    _window.ShapeCanvas.Visibility = System.Windows.Visibility.Visible;
                    break;
                case AppMode.Highlight:
                    _window.SetInputTransparent(true);
                    _window.ShapeCanvas.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case AppMode.Magnify:
                    _window.SetInputTransparent(true);
                    _window.ShapeCanvas.Visibility = System.Windows.Visibility.Collapsed;
                    break;
            }
        }

        public void ClearDrawing()
        {
            _window.DrawingCanvas.Strokes.Clear();
            _window.ShapeCanvas.Children.Clear();
        }
    }
}
