// <copyright file="SettingsService.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

using FileConverter.Properties;

namespace FileConverter.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;

    using CommunityToolkit.Mvvm.ComponentModel;

    using Debug = FileConverter.Diagnostics.Debug;

    public partial class SettingsService : ObservableObject, ISettingsService
    {
        public SettingsService()
        {
            // Load settigns.
            Debug.Log("Load settings...");
            this.Settings = this.Load();
        }

        public Settings Settings
        {
            get;
            private set;
        }

        private string UserSettingsTemporaryFilePath
        {
            get
            {
                string path = FileConverterExtension.PathHelpers.GetUserDataFolderPath;
                path = Path.Combine(path, "Settings.temp.xml");
                return path;
            }
        }

        public bool PostInstallationInitialization()
        {
            Debug.Log("Execute post installation initialization.");

            Settings defaultSettings = null;

            // Load the default settings.
            if (File.Exists(FileConverterExtension.PathHelpers.DefaultSettingsFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.DefaultSettingsFilePath, out defaultSettings);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Fail to load ZFileConverter default settings. {exception.Message}");
                    return false;
                }
            }
            else
            {
                Debug.LogError($"Default settings not found at path {FileConverterExtension.PathHelpers.DefaultSettingsFilePath}. You should try to reinstall the application.");
                return false;
            }

            // Load user settings if exists.
            Settings userSettings = null;
            if (File.Exists(FileConverterExtension.PathHelpers.UserSettingsFilePath))
            {
                try
                {
                    XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.UserSettingsFilePath, out userSettings);
                }
                catch (Exception)
                {
                    File.Delete(FileConverterExtension.PathHelpers.UserSettingsFilePath);
                }

                if (userSettings != null)
                {
                    if (userSettings.SerializationVersion != Settings.Version)
                    {
                        this.MigrateSettingsToCurrentVersion(userSettings);

                        Debug.Log($"ZFileConverter settings have been imported from version {userSettings.SerializationVersion} to version {Settings.Version}.");
                        userSettings.SerializationVersion = Settings.Version;
                    }

                    // Preserve the user's existing preset library. Merge only adds currently missing
                    // first-run defaults, so edited or retired presets are never deleted during an update.
                }
            }

            Settings settings = userSettings != null ? userSettings.Merge(defaultSettings) : defaultSettings;
            return this.Save(settings);
        }

        public void SaveSettings()
        {
            this.Save(this.Settings);
        }

        public void RevertSettings()
        {
            // Load previous preset in order to cancel changes.
            this.Settings = this.Load();
            ApplicationThemeManager.ApplyTheme(
                this.Settings?.AppearanceTheme ?? ApplicationTheme.Dark);
        }

        private Settings Load()
        {
            Settings settings = null;
            if (File.Exists(FileConverterExtension.PathHelpers.UserSettingsFilePath))
            {
                Settings userSettings = null;
                try
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.UserSettingsFilePath, out userSettings);
                    stopwatch.Stop();
                    Debug.Log($"Settings load time: {stopwatch.Elapsed.TotalMilliseconds}ms");

                    settings = userSettings;
                }
                catch (Exception exception)
                {
                    Debug.Log(exception.ToString());
                    if (!Debug.ShowMessageBoxes)
                    {
                        Debug.LogError("Can't load ZFileConverter user settings. Delete Settings.user.xml or run the app normally to choose a reset option.");
                        return null;
                    }

                    MessageBoxResult messageBoxResult =
                        MessageBox.Show(Resources.ErrorCantLoadSettings,
                            Resources.Error,
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Exclamation);

                    if (messageBoxResult == MessageBoxResult.Yes)
                    {
                        File.Delete(FileConverterExtension.PathHelpers.UserSettingsFilePath);
                        return this.Load();
                    }
                    else if (messageBoxResult == MessageBoxResult.No)
                    {
                        return null;
                    }
                }

                if (userSettings != null && userSettings.SerializationVersion != Settings.Version)
                {
                    this.MigrateSettingsToCurrentVersion(userSettings);

                    Debug.Log($"ZFileConverter settings have been imported from version {userSettings.SerializationVersion} to version {Settings.Version}.");
                    userSettings.SerializationVersion = Settings.Version;
                    this.Save(userSettings);
                }

                if (userSettings != null && File.Exists(FileConverterExtension.PathHelpers.DefaultSettingsFilePath))
                {
                    try
                    {
                        XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.DefaultSettingsFilePath, out Settings defaultSettings);
                        settings = userSettings.Merge(defaultSettings);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Fail to merge ZFileConverter default settings. {exception.Message}");
                    }
                }
            }
            else
            {
                // Load the default settings.
                if (File.Exists(FileConverterExtension.PathHelpers.DefaultSettingsFilePath))
                {
                    try
                    {
                        XmlHelpers.LoadFromFile<Settings>("Settings", FileConverterExtension.PathHelpers.DefaultSettingsFilePath, out Settings defaultSettings);
                        settings = defaultSettings;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Fail to load ZFileConverter default settings. {exception.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Default settings not found at path {FileConverterExtension.PathHelpers.DefaultSettingsFilePath}. You should try to reinstall the application.");
                }
            }

            return settings;
        }

        private bool Save(Settings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Clean();

            // Save the settings in a temporary files (we'll write the settings file when we'll succeed to write the registry keys).
            XmlHelpers.SaveToFile("Settings", this.UserSettingsTemporaryFilePath, settings);

            // Copy temporary settings file to the real settings file.
            File.Copy(this.UserSettingsTemporaryFilePath, FileConverterExtension.PathHelpers.UserSettingsFilePath, true);
            File.Delete(this.UserSettingsTemporaryFilePath);

            return true;
        }
    }
}
