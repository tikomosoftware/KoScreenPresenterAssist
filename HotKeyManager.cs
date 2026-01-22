using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenPresenterAssist
{
    public class HotKeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        private IntPtr _hWnd;
        private HwndSource? _source;
        private int _currentId = 0;
        private Dictionary<int, Action> _hotkeys = new Dictionary<int, Action>();

        public void Initialize(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hWnd = helper.Handle;
            _source = HwndSource.FromHwnd(_hWnd);
            _source.AddHook(HwndHook);
        }

        public void Register(uint modifiers, uint key, Action action)
        {
            int id = _currentId++;
            if (RegisterHotKey(_hWnd, id, modifiers, key))
            {
                _hotkeys[id] = action;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeys.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (var id in _hotkeys.Keys)
            {
                UnregisterHotKey(_hWnd, id);
            }
            _source?.RemoveHook(HwndHook);
        }
    }
}
