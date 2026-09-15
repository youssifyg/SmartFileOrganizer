using System;
using System.Windows;

namespace SmartFileOrganizer.UI.Themes
{
    public static class ThemeManager
    {
        public static void ApplyThemeAndLanguage(string theme, string language)
        {
            var dicts = System.Windows.Application.Current.Resources.MergedDictionaries;
            dicts.Clear();

            // Apply Theme
            string themeFile = theme == "Dark" ? "DarkTheme.xaml" : "LightTheme.xaml";
            dicts.Add( new ResourceDictionary { Source = new Uri( $"pack://application:,,,/SmartFileOrganizer.UI;component/Themes/{themeFile}", UriKind.Absolute ) } );

            // Apply Language
            string langFile = language == "Arabic" ? "Strings.ar.xaml" : "Strings.en.xaml";
            dicts.Add( new ResourceDictionary { Source = new Uri( $"pack://application:,,,/SmartFileOrganizer.UI;component/Themes/{langFile}", UriKind.Absolute ) } );
        }
    }
}
