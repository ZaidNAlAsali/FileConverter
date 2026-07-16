// <copyright file="ApplicationThemeManager.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;

    /// <summary>
    /// Applies the selected resource palette and keeps the native Windows title bar in sync.
    /// </summary>
    public static class ApplicationThemeManager
    {
        private const string DarkPalettePath = "Views/Resources/Colors.xaml";
        private const string LightPalettePath = "Views/Resources/Colors.Light.xaml";
        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmUseImmersiveDarkModeBefore20H1 = 19;

        public static readonly DependencyProperty EnableWindowThemeProperty =
            DependencyProperty.RegisterAttached(
                "EnableWindowTheme",
                typeof(bool),
                typeof(ApplicationThemeManager),
                new PropertyMetadata(false, OnEnableWindowThemeChanged));

        public static ApplicationTheme CurrentTheme
        {
            get;
            private set;
        } = ApplicationTheme.Dark;

        public static bool GetEnableWindowTheme(DependencyObject element)
        {
            return (bool)element.GetValue(EnableWindowThemeProperty);
        }

        public static void SetEnableWindowTheme(DependencyObject element, bool value)
        {
            element.SetValue(EnableWindowThemeProperty, value);
        }

        public static void ApplyTheme(ApplicationTheme theme)
        {
            CurrentTheme = theme;

            System.Windows.Application application = System.Windows.Application.Current;
            if (application == null)
            {
                return;
            }

            string targetPalettePath = theme == ApplicationTheme.Light ? LightPalettePath : DarkPalettePath;
            ResourceDictionary replacement = new ResourceDictionary
                                                 {
                                                     Source = new Uri(targetPalettePath, UriKind.Relative),
                                                 };

            int paletteIndex = -1;
            for (int index = 0; index < application.Resources.MergedDictionaries.Count; index++)
            {
                ResourceDictionary dictionary = application.Resources.MergedDictionaries[index];
                string source = dictionary.Source?.OriginalString;
                if (!string.IsNullOrEmpty(source) &&
                    (source.EndsWith("Colors.xaml", StringComparison.OrdinalIgnoreCase) ||
                     source.EndsWith("Colors.Light.xaml", StringComparison.OrdinalIgnoreCase)))
                {
                    paletteIndex = index;
                    break;
                }
            }

            if (paletteIndex >= 0)
            {
                string currentSource = application.Resources.MergedDictionaries[paletteIndex].Source?.OriginalString;
                if (!string.Equals(currentSource, targetPalettePath, StringComparison.OrdinalIgnoreCase))
                {
                    application.Resources.MergedDictionaries[paletteIndex] = replacement;
                }
            }
            else
            {
                application.Resources.MergedDictionaries.Insert(0, replacement);
            }

            foreach (Window window in application.Windows)
            {
                ApplyWindowTheme(window);
            }
        }

        public static void ApplyWindowTheme(Window window)
        {
            if (window == null)
            {
                return;
            }

            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            int useDarkMode = CurrentTheme == ApplicationTheme.Dark ? 1 : 0;
            try
            {
                int result = DwmSetWindowAttribute(
                    handle,
                    DwmUseImmersiveDarkMode,
                    ref useDarkMode,
                    Marshal.SizeOf(typeof(int)));

                if (result != 0)
                {
                    DwmSetWindowAttribute(
                        handle,
                        DwmUseImmersiveDarkModeBefore20H1,
                        ref useDarkMode,
                        Marshal.SizeOf(typeof(int)));
                }
            }
            catch (DllNotFoundException)
            {
                // Older Windows versions keep their native title-bar appearance.
            }
            catch (EntryPointNotFoundException)
            {
                // Older Windows versions keep their native title-bar appearance.
            }
        }

        private static void OnEnableWindowThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (!(dependencyObject is Window window) || !(bool)args.NewValue)
            {
                return;
            }

            window.SourceInitialized -= Window_SourceInitialized;
            window.SourceInitialized += Window_SourceInitialized;
            ApplyWindowTheme(window);
        }

        private static void Window_SourceInitialized(object sender, EventArgs args)
        {
            ApplyWindowTheme(sender as Window);
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr windowHandle,
            int attribute,
            ref int attributeValue,
            int attributeSize);
    }
}
