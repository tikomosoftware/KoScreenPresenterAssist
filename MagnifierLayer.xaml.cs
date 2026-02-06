using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace ScreenPresenterAssist
{
    public enum MagnifierDesign
    {
        Lens,           // 縁取り(白) + 影
        BlackBorder,    // 縁取り(黒)
        WhiteBorder     // 縁取り(白)
    }

    /// <summary>
    /// Magnification APIを使用した拡大鏡実装
    /// </summary>
    public partial class MagnifierLayer : UserControl
    {
        private DispatcherTimer _timer;
        private double _zoomFactor = 1.5;
        private IntPtr _hwndMag = IntPtr.Zero;
        private bool _isInitialized = false;

        #region P/Invoke
        [DllImport("Magnification.dll")]
        private static extern bool MagInitialize();

        [DllImport("Magnification.dll")]
        private static extern bool MagUninitialize();

        [DllImport("Magnification.dll")]
        private static extern bool MagSetWindowSource(IntPtr hwnd, RECT rect);

        [DllImport("Magnification.dll")]
        private static extern bool MagSetWindowTransform(IntPtr hwnd, ref MAGTRANSFORM pTransform);

        [DllImport("Magnification.dll")]
        private static extern bool MagSetWindowFilterList(IntPtr hwnd, uint dwFilterMode, int count, IntPtr[] phwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateEllipticRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
            public RECT(int left, int top, int right, int bottom)
            {
                Left = left; Top = top; Right = right; Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MAGTRANSFORM
        {
            public float v11, v12, v13;
            public float v21, v22, v23;
            public float v31, v32, v33;
        }

        private const string WC_MAGNIFIER = "Magnifier";
        private const uint WS_CHILD = 0x40000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint MW_FILTERMODE_EXCLUDE = 0;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        #endregion

        public MagnifierLayer()
        {
            InitializeComponent();
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += (s, e) => UpdateMagnifier();
            
            this.Loaded += (s, e) => InitMagnifier();
            this.Unloaded += (s, e) => ReleaseMagnifier();
        }

        private void InitMagnifier()
        {
            if (_isInitialized) return;

            if (!MagInitialize()) return;

            var window = Window.GetWindow(this);
            if (window == null) return;

            IntPtr hWndParent = new WindowInteropHelper(window).Handle;
            
            // 拡大鏡コントロール（子ウィンドウ）の作成 - 初期状態は非表示（WS_VISIBLEを外す）
            _hwndMag = CreateWindowEx(0, WC_MAGNIFIER, "MagnifierWindow", WS_CHILD, 0, 0, (int)MagnifierBorder.Width, (int)MagnifierBorder.Height, hWndParent, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (_hwndMag == IntPtr.Zero)
            {
                MagUninitialize();
                return;
            }

            // 自分自身のウィンドウを除外リストに追加（合わせ鏡防止）
            MagSetWindowFilterList(_hwndMag, MW_FILTERMODE_EXCLUDE, 1, new IntPtr[] { hWndParent });

            // 倍率の設定
            SetZoom(_zoomFactor);

            // 円形リージョンを設定
            ApplyCircularRegion();

            _isInitialized = true;
        }

        private void ApplyCircularRegion()
        {
            if (_hwndMag == IntPtr.Zero) return;
            int width = (int)MagnifierBorder.Width;
            int height = (int)MagnifierBorder.Height;
            IntPtr hRgn = CreateEllipticRgn(0, 0, width, height);
            SetWindowRgn(_hwndMag, hRgn, true);
            // リージョンはウィンドウに渡すのでDeleteObjectは呼ばない
        }

        private void ReleaseMagnifier()
        {
            if (!_isInitialized) return;
            if (_hwndMag != IntPtr.Zero)
            {
                DestroyWindow(_hwndMag);
                _hwndMag = IntPtr.Zero;
            }
            MagUninitialize();
            _isInitialized = false;
        }

        private void SetZoom(double zoom)
        {
            if (_hwndMag == IntPtr.Zero) return;
            MAGTRANSFORM matrix = new MAGTRANSFORM
            {
                v11 = (float)zoom, v22 = (float)zoom, v33 = 1.0f
            };
            MagSetWindowTransform(_hwndMag, ref matrix);
        }

        public void SetActive(bool active)
        {
            if (active)
            {
                this.Visibility = Visibility.Visible;
                InitMagnifier();
                if (_hwndMag != IntPtr.Zero)
                {
                    ShowWindow(_hwndMag, SW_SHOW);
                }
                _timer.Start();
            }
            else
            {
                _timer.Stop();
                if (_hwndMag != IntPtr.Zero)
                {
                    ShowWindow(_hwndMag, SW_HIDE);
                }
                this.Visibility = Visibility.Collapsed;
            }
        }

        public void SetDesign(MagnifierDesign design)
        {
            switch (design)
            {
                case MagnifierDesign.Lens:
                    MagnifierBorder.BorderBrush = System.Windows.Media.Brushes.White;
                    MagnifierBorder.BorderThickness = new Thickness(2);
                    MagnifierBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 15, Opacity = 0.5 };
                    break;
                case MagnifierDesign.BlackBorder:
                    MagnifierBorder.BorderBrush = System.Windows.Media.Brushes.Black;
                    MagnifierBorder.BorderThickness = new Thickness(2);
                    MagnifierBorder.Effect = null;
                    break;
                case MagnifierDesign.WhiteBorder:
                    MagnifierBorder.BorderBrush = System.Windows.Media.Brushes.White;
                    MagnifierBorder.BorderThickness = new Thickness(2);
                    MagnifierBorder.Effect = null;
                    break;
            }
        }

        private void UpdateMagnifier()
        {
            if (_hwndMag == IntPtr.Zero) return;

            var mousePos = System.Windows.Forms.Control.MousePosition;
            var source = PresentationSource.FromVisual(this);
            if (source == null || source.CompositionTarget == null) return;

            double dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
            double dpiScaleY = source.CompositionTarget.TransformToDevice.M22;

            // WPF座標への変換
            double wpfMouseX = mousePos.X / dpiScaleX;
            double wpfMouseY = mousePos.Y / dpiScaleY;

            // 拡大鏡枠の移動
            Canvas.SetLeft(MagnifierBorder, wpfMouseX - MagnifierBorder.Width / 2);
            Canvas.SetTop(MagnifierBorder, wpfMouseY - MagnifierBorder.Height / 2);

            // 実際の拡大鏡ウィンドウ（Win32）のサイズ（DPI考慮）
            int magWidth = (int)(MagnifierBorder.Width * dpiScaleX);
            int magHeight = (int)(MagnifierBorder.Height * dpiScaleY);
            
            // 偶数に揃えてピクセル境界を正確にする（シャープさ向上）
            magWidth = (magWidth / 2) * 2;
            magHeight = (magHeight / 2) * 2;
            
            int magLeft = (int)((wpfMouseX - MagnifierBorder.Width / 2) * dpiScaleX);
            int magTop = (int)((wpfMouseY - MagnifierBorder.Height / 2) * dpiScaleY);
            
            SetWindowPos(_hwndMag, IntPtr.Zero, magLeft, magTop, magWidth, magHeight, SWP_NOZORDER | SWP_NOACTIVATE);

            // 拡大ソース範囲の設定（整数除算で正確なピクセル境界）
            int srcWidth = (int)Math.Round(magWidth / _zoomFactor);
            int srcHeight = (int)Math.Round(magHeight / _zoomFactor);
            
            // ソース領域も偶数に揃える
            srcWidth = (srcWidth / 2) * 2;
            srcHeight = (srcHeight / 2) * 2;
            
            RECT srcRect = new RECT(
                mousePos.X - srcWidth / 2,
                mousePos.Y - srcHeight / 2,
                mousePos.X + srcWidth / 2,
                mousePos.Y + srcHeight / 2
            );

            MagSetWindowSource(_hwndMag, srcRect);
        }
    }
}
