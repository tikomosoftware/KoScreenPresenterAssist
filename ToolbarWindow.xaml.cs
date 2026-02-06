using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ScreenPresenterAssist
{
    public partial class ToolbarWindow : Window
    {
        private readonly OverlayWindow _overlay;
        private readonly ModeManager _modeManager;
        private AppSettings _settings;
        
        private static readonly SolidColorBrush NormalBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A4A6A"));
        private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
        private SolidColorBrush _currentDrawingBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B"));
        private SolidColorBrush _currentHighlightBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F39C12"));

        public ToolbarWindow(OverlayWindow overlay, ModeManager modeManager)
        {
            InitializeComponent();
            _overlay = overlay;
            _modeManager = modeManager;
            _settings = AppSettings.Load();
            
            // ToolbarをOverlayの子ウィンドウにして、常に手前に表示されるようにする
            this.Owner = overlay;
            
            // モード変更時にボタン色を更新
            _modeManager.ModeChanged += UpdateModeButtons;
            
            // 描画色変更時にボタン色を更新
            _overlay.DrawingColorChanged += (color) =>
            {
                _currentDrawingBrush = new SolidColorBrush(color);
                UpdateModeButtons(_modeManager.CurrentMode);
            };

            // ハイライト色変更時にボタン色を更新
            _overlay.HighlightColorChanged += (color) =>
            {
                _currentHighlightBrush = new SolidColorBrush(color);
                UpdateModeButtons(_modeManager.CurrentMode);
            };
            
            // ウィンドウロード時に位置を復元
            this.Loaded += ToolbarWindow_Loaded;
            
            // ウィンドウ位置変更時に保存
            this.LocationChanged += ToolbarWindow_LocationChanged;

            SetTooltips();
        }

        private void SetTooltips()
        {
            BtnDraw.ToolTip = I18n.TooltipDraw;
            BtnHighlight.ToolTip = I18n.TooltipHighlight;
            BtnMagnify.ToolTip = I18n.TooltipMagnify;
            BtnClear.ToolTip = I18n.TooltipClear;
            BtnOff.ToolTip = I18n.TooltipOff;
            BtnRed.ToolTip = I18n.TooltipRed;
            BtnBlue.ToolTip = I18n.TooltipBlue;
            BtnYellow.ToolTip = I18n.TooltipYellow;
            BtnGreen.ToolTip = I18n.TooltipGreen;
        }
        
        private void ToolbarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPosition();
        }
        
        private void ToolbarWindow_LocationChanged(object? sender, EventArgs e)
        {
            SavePosition();
        }
        
        private void LoadPosition()
        {
            if (_settings.ToolbarLeft != 0 || _settings.ToolbarTop != 0)
            {
                this.Left = _settings.ToolbarLeft;
                this.Top = _settings.ToolbarTop;
                return;
            }
            
            // デフォルト位置: 左下
            this.Left = 20;
            this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 60;
        }
        
        private void SavePosition()
        {
            _settings.ToolbarLeft = this.Left;
            _settings.ToolbarTop = this.Top;
            _settings.Save();
        }
        
        private void UpdateModeButtons(AppMode mode)
        {
            BtnDraw.Background = mode == AppMode.Drawing ? _currentDrawingBrush : NormalBrush;
            BtnHighlight.Background = mode == AppMode.Highlight ? _currentHighlightBrush : NormalBrush;
            BtnMagnify.Background = mode == AppMode.Magnify ? ActiveBrush : NormalBrush;
            
            // 色パネルは描画モードまたは強調表示モード時のみ表示
            ColorPanel.Visibility = (mode == AppMode.Drawing || mode == AppMode.Highlight) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnDraw_Click(object sender, RoutedEventArgs e)
        {
            _modeManager.SetMode(AppMode.Drawing);
        }

        private void BtnHighlight_Click(object sender, RoutedEventArgs e)
        {
            _modeManager.SetMode(AppMode.Highlight);
        }

        private void BtnMagnify_Click(object sender, RoutedEventArgs e)
        {
            _modeManager.SetMode(AppMode.Magnify);
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            _modeManager.ClearDrawing();
        }

        private void BtnOff_Click(object sender, RoutedEventArgs e)
        {
            _modeManager.SetMode(AppMode.Normal);
        }

        private void ChangeColor(string colorCode)
        {
            var color = (Color)ColorConverter.ConvertFromString(colorCode);
            if (_modeManager.CurrentMode == AppMode.Highlight)
            {
                _overlay.SetHighlightColor(color);
            }
            else
            {
                _overlay.SetDrawingColor(color);
            }
            // 実際の更新はイベント経由で行われる
        }

        private void BtnRed_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor("#FF6B6B");
        }

        private void BtnBlue_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor("#74B9FF");
        }

        private void BtnYellow_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor("#FDCB6E");
        }

        private void BtnGreen_Click(object sender, RoutedEventArgs e)
        {
            ChangeColor("#27AE60");
        }
    }
}
