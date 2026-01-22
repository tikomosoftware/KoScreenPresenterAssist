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
            _notifyIcon.Icon = SystemIcons.Application; // 本来は専用アイコンを使うべき
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "Screen Presenter Assist";

            var contextMenu = new ContextMenuStrip();
            
            if (toggleToolbarAction != null)
            {
                _toolbarMenuItem = new ToolStripMenuItem("ツールバー非表示");
                _toolbarMenuItem.Click += (s, e) => toggleToolbarAction();
                contextMenu.Items.Add(_toolbarMenuItem);
                contextMenu.Items.Add(new ToolStripSeparator());
            }
            
            contextMenu.Items.Add("ヘルプ", null, (s, e) => ShowHelp());
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, (s, e) => System.Windows.Application.Current.Shutdown());
            
            _notifyIcon.ContextMenuStrip = contextMenu;
        }
        
        private void ShowHelp()
        {
            string helpText = @"【Reading a Book 使い方】

■ ショートカットキー
  Ctrl + Alt + P : 描画モード切替
  Ctrl + Alt + H : 強調表示モード切替
  Ctrl + Alt + Z : 拡大鏡モード切替
  Ctrl + Alt + X : 描画を全消去
  Esc : 全モード解除（描画/強調/拡大）

■ 描画モード中
  1～4キー : 色変更（1:赤 2:青 3:黄 4:緑）
  Ctrl + ドラッグ : 円を描く
  通常ドラッグ : フリーハンド描画

■ ツールバー
  ✏️ : 描画モード
  🔦 : 強調表示モード
  🔍 : 拡大鏡モード
  🗑️ : 全消去
  ⏹️ : 機能OFF
";
            System.Windows.MessageBox.Show(helpText, "ヘルプ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void SetToolbarVisible(bool visible)
        {
            _isToolbarVisible = visible;
            if (_toolbarMenuItem != null)
            {
                _toolbarMenuItem.Text = visible ? "ツールバー非表示" : "ツールバー表示";
            }
        }

        public void Dispose()
        {
            _notifyIcon.Dispose();
        }
    }
}
