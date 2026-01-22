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
        private readonly string _settingsPath;
        
        private static readonly SolidColorBrush NormalBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A4A6A"));
        private static readonly SolidColorBrush ActiveBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));

        public ToolbarWindow(OverlayWindow overlay, ModeManager modeManager)
        {
            InitializeComponent();
            _overlay = overlay;
            _modeManager = modeManager;
            
            // 設定ファイルのパス
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ReadingABook");
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "toolbar-settings.json");
            
            // ToolbarをOverlayの子ウィンドウにして、常に手前に表示されるようにする
            this.Owner = overlay;
            
            // モード変更時にボタン色を更新
            _modeManager.ModeChanged += UpdateModeButtons;
            
            // ウィンドウロード時に位置を復元
            this.Loaded += ToolbarWindow_Loaded;
            
            // ウィンドウ位置変更時に保存
            this.LocationChanged += ToolbarWindow_LocationChanged;
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
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<ToolbarSettings>(json);
                    if (settings != null)
                    {
                        this.Left = settings.Left;
                        this.Top = settings.Top;
                        return;
                    }
                }
            }
            catch
            {
                // 設定読み込み失敗時はデフォルト位置を使用
            }
            
            // デフォルト位置: 左下
            this.Left = 20;
            this.Top = SystemParameters.PrimaryScreenHeight - this.Height - 60;
        }
        
        private void SavePosition()
        {
            try
            {
                var settings = new ToolbarSettings
                {
                    Left = this.Left,
                    Top = this.Top
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // 保存失敗は無視
            }
        }
        
        private void UpdateModeButtons(AppMode mode)
        {
            BtnDraw.Background = mode == AppMode.Drawing ? ActiveBrush : NormalBrush;
            BtnHighlight.Background = mode == AppMode.Highlight ? ActiveBrush : NormalBrush;
            BtnMagnify.Background = mode == AppMode.Magnify ? ActiveBrush : NormalBrush;
            
            // 色パネルは描画モード時のみ表示
            ColorPanel.Visibility = mode == AppMode.Drawing ? Visibility.Visible : Visibility.Collapsed;
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

        private void BtnRed_Click(object sender, RoutedEventArgs e)
        {
            _overlay.SetDrawingColor((Color)ColorConverter.ConvertFromString("#FF6B6B"));
        }

        private void BtnBlue_Click(object sender, RoutedEventArgs e)
        {
            _overlay.SetDrawingColor((Color)ColorConverter.ConvertFromString("#74B9FF"));
        }

        private void BtnYellow_Click(object sender, RoutedEventArgs e)
        {
            _overlay.SetDrawingColor((Color)ColorConverter.ConvertFromString("#FDCB6E"));
        }

        private void BtnGreen_Click(object sender, RoutedEventArgs e)
        {
            _overlay.SetDrawingColor((Color)ColorConverter.ConvertFromString("#55EFC4"));
        }
    }
    
    public class ToolbarSettings
    {
        public double Left { get; set; }
        public double Top { get; set; }
    }
}
