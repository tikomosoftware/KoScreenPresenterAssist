using System;
using System.Globalization;
using System.Resources;

namespace ScreenPresenterAssist
{
    public static class I18n
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("ScreenPresenterAssist.Resources.Strings", typeof(I18n).Assembly);

        public static string GetString(string key)
        {
            try
            {
                return _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
            }
            catch
            {
                return key;
            }
        }

        // Toolbar
        public static string TooltipDraw => GetString("TooltipDraw");
        public static string TooltipHighlight => GetString("TooltipHighlight");
        public static string TooltipMagnify => GetString("TooltipMagnify");
        public static string TooltipClear => GetString("TooltipClear");
        public static string TooltipOff => GetString("TooltipOff");
        public static string TooltipRed => GetString("TooltipRed");
        public static string TooltipBlue => GetString("TooltipBlue");
        public static string TooltipYellow => GetString("TooltipYellow");
        public static string TooltipGreen => GetString("TooltipGreen");

        // Tray Menu
        public static string MenuShowToolbar => GetString("MenuShowToolbar");
        public static string MenuHideToolbar => GetString("MenuHideToolbar");
        public static string MenuHelp => GetString("MenuHelp");
        public static string MenuSettings => GetString("MenuSettings");
        public static string MenuMagnifierDesign => GetString("MenuMagnifierDesign");
        public static string DesignLens => GetString("DesignLens");
        public static string DesignBlackBorder => GetString("DesignBlackBorder");
        public static string DesignWhiteBorder => GetString("DesignWhiteBorder");
        public static string MenuExit => GetString("MenuExit");

        // Help Message
        public static string HelpTitle => GetString("HelpTitle");
        public static string HelpContent => GetString("HelpContent");
    }
}
