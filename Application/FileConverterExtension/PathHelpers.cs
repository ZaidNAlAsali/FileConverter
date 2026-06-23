// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System;
    using System.IO;
    using Microsoft.Win32;

    public static class PathHelpers
    {
        private const string ProductName = "ZFileConverter";
        private const string LegacyProductName = "FileConverter";
        private const string RegistryKeyPath = @"Software\ZFileConverter";
        private const string LegacyRegistryKeyPath = @"Software\FileConverter";

        private static RegistryKey fileConverterRegistryKey;
        private static string fileConverterPath;

        public static string UserSettingsFilePath
        {
            get
            {
                string userSettingsFilePath = Path.Combine(PathHelpers.GetUserDataFolderPath, "Settings.user.xml");
                PathHelpers.TryMigrateLegacyUserSettings(userSettingsFilePath);
                return userSettingsFilePath;
            }
        }

        public static string DefaultSettingsFilePath
        {
            get
            {
                string localDefaultSettingsPath = Path.Combine(Path.GetDirectoryName(typeof(PathHelpers).Assembly.Location), "Settings.default.xml");
                if (File.Exists(localDefaultSettingsPath))
                {
                    return localDefaultSettingsPath;
                }

                string pathToFileConverterExecutable = PathHelpers.FileConverterPath;
                if (string.IsNullOrEmpty(pathToFileConverterExecutable))
                {
                    return null;
                }

                return Path.Combine(Path.GetDirectoryName(pathToFileConverterExecutable), "Settings.default.xml");
            }
        }

        public static RegistryKey FileConverterRegistryKey
        {
            get
            {
                if (PathHelpers.fileConverterRegistryKey == null)
                {
                    PathHelpers.fileConverterRegistryKey = Registry.CurrentUser.OpenSubKey(PathHelpers.RegistryKeyPath) ??
                                                           Registry.CurrentUser.OpenSubKey(PathHelpers.LegacyRegistryKeyPath);
                    if (PathHelpers.fileConverterRegistryKey == null)
                    {
                        throw new Exception("Can't retrieve ZFileConverter registry entry.");
                    }
                }

                return PathHelpers.fileConverterRegistryKey;
            }
        }

        public static string FileConverterPath
        {
            get
            {
                if (string.IsNullOrEmpty(PathHelpers.fileConverterPath))
                {
                    PathHelpers.fileConverterPath = PathHelpers.FileConverterRegistryKey.GetValue("Path") as string;
                }

                return PathHelpers.fileConverterPath;
            }
        }

        public static string GetUserDataFolderPath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                path = Path.Combine(path, PathHelpers.ProductName);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        private static string GetLegacyUserDataFolderPath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(path, PathHelpers.LegacyProductName);
            }
        }

        private static void TryMigrateLegacyUserSettings(string userSettingsFilePath)
        {
            if (File.Exists(userSettingsFilePath))
            {
                return;
            }

            string legacyUserSettingsFilePath = Path.Combine(PathHelpers.GetLegacyUserDataFolderPath, "Settings.user.xml");
            if (!File.Exists(legacyUserSettingsFilePath))
            {
                return;
            }

            try
            {
                File.Copy(legacyUserSettingsFilePath, userSettingsFilePath);
            }
            catch
            {
                // Best effort migration. If it fails, the application will recreate settings from defaults.
            }
        }
    }
}
