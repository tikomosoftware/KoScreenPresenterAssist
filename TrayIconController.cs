using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace ScreenPresenterAssist
{
    public class TrayIconController : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private Window _parent;
        private ToolStripMenuItem? _toolbarMenuItem;
        private Action? _toggleToolbarAction;
        private bool _isToolbarVisible = true;

        public TrayIconController(Window parent, Action? toggleToolbarAction = null)
        {
            _parent = parent;
            _toggleToolbarAction = toggleToolbarAction;
            _notifyIcon = new NotifyIcon();
            
            // アプリケーションアイコンの読み込み
            var iconStream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/app-icon.ico"))?.Stream;
            if (iconStream != null)
            {
                _notifyIcon.Icon = new Icon(iconStream);
            }
            else
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
            
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Screen Presenter Assist";

            var contextMenu = new ContextMenuStrip();
            
            if (toggleToolbarAction != null)
            {
                _toolbarMenuItem = new ToolStripMenuItem(I18n.MenuHideToolbar);
                _toolbarMenuItem.Click += (s, e) => toggleToolbarAction();
                contextMenu.Items.Add(_toolbarMenuItem);
                contextMenu.Items.Add(new ToolStripSeparator());
            }
            
            contextMenu.Items.Add(I18n.MenuHelp, null, (s, e) => ShowHelp());
            
            // 設定メニュー
            var settingsMenu = new ToolStripMenuItem(I18n.MenuSettings);
            var magnifierMenu = new ToolStripMenuItem(I18n.MenuMagnifierDesign);
            
            var lensItem = new ToolStripMenuItem(I18n.DesignLens, null, (s, e) => UpdateMagnifierDesign(MagnifierDesign.Lens));
            var blackItem = new ToolStripMenuItem(I18n.DesignBlackBorder, null, (s, e) => UpdateMagnifierDesign(MagnifierDesign.BlackBorder));
            var whiteItem = new ToolStripMenuItem(I18n.DesignWhiteBorder, null, (s, e) => UpdateMagnifierDesign(MagnifierDesign.WhiteBorder));
            
            magnifierMenu.DropDownItems.AddRange(new ToolStripItem[] { lensItem, blackItem, whiteItem });
            settingsMenu.DropDownItems.Add(magnifierMenu);
            contextMenu.Items.Add(settingsMenu);
            
            // 現在の設定をチェックする関数
            _updateCheckAction = (design) => {
                lensItem.Checked = design == MagnifierDesign.Lens;
                blackItem.Checked = design == MagnifierDesign.BlackBorder;
                whiteItem.Checked = design == MagnifierDesign.WhiteBorder;
            };

            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(I18n.MenuExit, null, (s, e) => System.Windows.Application.Current.Shutdown());
            
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private Action<MagnifierDesign>? _updateCheckAction;

        private void UpdateMagnifierDesign(MagnifierDesign design)
        {
            if (_parent is OverlayWindow overlay)
            {
                overlay.SetMagnifierDesign(design);
                _updateCheckAction?.Invoke(design);
            }
        }

        public void InitialCheckDesign(MagnifierDesign design)
        {
            _updateCheckAction?.Invoke(design);
        }
        
        private void ShowHelp()
        {
            System.Windows.MessageBox.Show(I18n.HelpContent, I18n.HelpTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetToolbarVisible(bool visible)
        {
            _isToolbarVisible = visible;
            if (_toolbarMenuItem != null)
            {
                _toolbarMenuItem.Text = visible ? I18n.MenuHideToolbar : I18n.MenuShowToolbar;
            }
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }
}
