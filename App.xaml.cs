using System.Configuration;
using System.Data;
using System.Windows;

namespace ScreenPresenterAssist;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private OverlayWindow? _overlay;
    private ToolbarWindow? _toolbar;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _overlay = new OverlayWindow();
        _overlay.Show();
        
        var modeManager = _overlay.GetModeManager();
        if (modeManager != null)
        {
            _toolbar = new ToolbarWindow(_overlay, modeManager);
            _toolbar.Show();
            
            // タスクトレイからツールバーの表示/非表示をトグルできるようにする
            _overlay.SetToolbarToggleAction(() => 
            {
                if (_toolbar.IsVisible)
                {
                    _toolbar.Hide();
                    _overlay.UpdateToolbarVisibility(false);
                }
                else
                {
                    _toolbar.Show();
                    _toolbar.Activate();
                    _overlay.UpdateToolbarVisibility(true);
                }
            });
        }
    }
}

