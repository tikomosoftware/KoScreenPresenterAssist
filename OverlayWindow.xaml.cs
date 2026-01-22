using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenPresenterAssist
{
    public partial class OverlayWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        private HotKeyManager _hotKeyManager = new HotKeyManager();
        private ModeManager? _modeManager;
        private MouseHook _mouseHook = new MouseHook();
        private CursorVisualizer _cursorVisualizer;
        private MagnifierLayer _magnifierLayer;
        private TrayIconController? _trayIconController;
        private Action? _toolbarShowAction;
        
        // 円描画用
        private bool _isDrawingCircle = false;
        private Point _circleStartPoint;
        private Ellipse? _previewEllipse;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public OverlayWindow()
        {
            InitializeComponent();
            _cursorVisualizer = new CursorVisualizer();
            CursorCanvas.Children.Add(_cursorVisualizer);

            _magnifierLayer = new MagnifierLayer();
            MagnifierCanvas.Children.Add(_magnifierLayer);
            _magnifierLayer.SetActive(false);

            _modeManager = new ModeManager(this);
            
            _mouseHook.LeftButtonDown += (x, y) => 
                _cursorVisualizer.PlayRipple(new System.Windows.Point(x, y), System.Windows.Media.Brushes.Cyan);
            _mouseHook.RightButtonDown += (x, y) => 
                _cursorVisualizer.PlayRipple(new System.Windows.Point(x, y), System.Windows.Media.Brushes.Magenta);
            
            // 円描画用マウスイベント
            DrawingCanvas.PreviewMouseLeftButtonDown += OnDrawingMouseDown;
            DrawingCanvas.PreviewMouseMove += OnDrawingMouseMove;
            DrawingCanvas.PreviewMouseLeftButtonUp += OnDrawingMouseUp;
            
            // PreviewKeyDownでEscキーを監視（描画モード用）+ 色切り替え
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    _modeManager?.SetMode(AppMode.Normal);
                }
                // 描画モード中の色切り替え（1=赤、2=青、3=黄、4=緑）
                else if (_modeManager?.CurrentMode == AppMode.Drawing)
                {
                    switch (e.Key)
                    {
                        case System.Windows.Input.Key.D1:
                        case System.Windows.Input.Key.NumPad1:
                            DrawingCanvas.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Red;
                            break;
                        case System.Windows.Input.Key.D2:
                        case System.Windows.Input.Key.NumPad2:
                            DrawingCanvas.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Blue;
                            break;
                        case System.Windows.Input.Key.D3:
                        case System.Windows.Input.Key.NumPad3:
                            DrawingCanvas.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Yellow;
                            break;
                        case System.Windows.Input.Key.D4:
                        case System.Windows.Input.Key.NumPad4:
                            DrawingCanvas.DefaultDrawingAttributes.Color = System.Windows.Media.Colors.Green;
                            break;
                    }
                }
            };
        }
        
        private void OnDrawingMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && _modeManager?.CurrentMode == AppMode.Drawing)
            {
                _isDrawingCircle = true;
                _circleStartPoint = e.GetPosition(ShapeCanvas);
                
                // プレビュー用の楕円を作成
                _previewEllipse = new Ellipse
                {
                    Stroke = new SolidColorBrush(DrawingCanvas.DefaultDrawingAttributes.Color),
                    StrokeThickness = DrawingCanvas.DefaultDrawingAttributes.Width,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(_previewEllipse, _circleStartPoint.X);
                Canvas.SetTop(_previewEllipse, _circleStartPoint.Y);
                ShapeCanvas.Children.Add(_previewEllipse);
                
                e.Handled = true;
            }
        }
        
        private void OnDrawingMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawingCircle && _previewEllipse != null)
            {
                var currentPoint = e.GetPosition(ShapeCanvas);
                double radius = Math.Sqrt(
                    Math.Pow(currentPoint.X - _circleStartPoint.X, 2) + 
                    Math.Pow(currentPoint.Y - _circleStartPoint.Y, 2));
                
                _previewEllipse.Width = radius * 2;
                _previewEllipse.Height = radius * 2;
                Canvas.SetLeft(_previewEllipse, _circleStartPoint.X - radius);
                Canvas.SetTop(_previewEllipse, _circleStartPoint.Y - radius);
            }
        }
        
        private void OnDrawingMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawingCircle && _previewEllipse != null)
            {
                // プレビューを確定（そのまま残す）
                _previewEllipse = null;
                _isDrawingCircle = false;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 初回起動時は操作モード（入力透過）にする
            SetInputTransparent(true);
            
            // Alt+Tabに表示されないようにToolWindowスタイルを設定
            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            // ホットキーの初期化と登録
            _hotKeyManager.Initialize(this);
            _mouseHook.Install();
            _trayIconController = new TrayIconController(this, () => _toolbarShowAction?.Invoke());
            
            // ESCキーで全モード解除（グローバルホットキー）
            _hotKeyManager.Register(0, 0x1B, // ESC
                () => _modeManager?.SetMode(AppMode.Normal));
            
            // 描画モード切替: Ctrl + Alt + P
            _hotKeyManager.Register(HotKeyManager.MOD_CONTROL | HotKeyManager.MOD_ALT, 0x50, // 'P'
                () => _modeManager?.SetMode(AppMode.Drawing));

            // 全消去: Ctrl + Alt + X
            _hotKeyManager.Register(HotKeyManager.MOD_CONTROL | HotKeyManager.MOD_ALT, 0x58, // 'X'
                () => _modeManager?.ClearDrawing());

            // 拡大モード切替: Ctrl + Alt + Z
            _hotKeyManager.Register(HotKeyManager.MOD_CONTROL | HotKeyManager.MOD_ALT, 0x5A, // 'Z'
                () => _modeManager?.SetMode(AppMode.Magnify));

            // 強調表示モード切替: Ctrl + Alt + H (仕様書にはないが便利なので追加)
            _hotKeyManager.Register(HotKeyManager.MOD_CONTROL | HotKeyManager.MOD_ALT, 0x48, // 'H'
                () => _modeManager?.SetMode(AppMode.Highlight));
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotKeyManager.Dispose();
            _mouseHook.Dispose();
            _trayIconController?.Dispose();
            base.OnClosed(e);
        }

        public void SetCursorHighlight(bool visible)
        {
            _cursorVisualizer.SetHighlightVisible(visible);
        }

        public void SetMagnifierActive(bool active)
        {
            _magnifierLayer.SetActive(active);
        }

        public void SetDrawingColor(System.Windows.Media.Color color)
        {
            DrawingCanvas.DefaultDrawingAttributes.Color = color;
        }

        public ModeManager? GetModeManager()
        {
            return _modeManager;
        }

        public void SetToolbarToggleAction(Action action)
        {
            _toolbarShowAction = action;
        }

        public void UpdateToolbarVisibility(bool visible)
        {
            _trayIconController?.SetToolbarVisible(visible);
        }

        /// <summary>
        /// 入力透過状態を切り替える
        /// </summary>
        /// <param name="transparent">trueの場合は入力透過、falseの場合は描画可能</param>
        public void SetInputTransparent(bool transparent)
        {
            var helper = new WindowInteropHelper(this);
            int exStyle = GetWindowLong(helper.Handle, GWL_EXSTYLE);

            if (transparent)
            {
                SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
                DrawingCanvas.Visibility = Visibility.Collapsed;
            }
            else
            {
                // WS_EX_TRANSPARENTのみ解除、WS_EX_LAYEREDは維持
                SetWindowLong(helper.Handle, GWL_EXSTYLE, (exStyle & ~WS_EX_TRANSPARENT) | WS_EX_LAYERED);
                DrawingCanvas.Visibility = Visibility.Visible;
                // 描画モードではウィンドウをアクティブにしてフォーカスを取得
                this.Activate();
                DrawingCanvas.Focus();
            }
        }
    }
}
