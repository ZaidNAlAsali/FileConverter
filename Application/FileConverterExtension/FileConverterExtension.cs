// <copyright file="FileConverterExtension.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverterExtension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Forms;

    using SharpShell.Attributes;
    using SharpShell.SharpContextMenu;

    /// <summary>
    /// File converter context menu extension class.
    /// </summary>
    [ComVisible(true), Guid("AF9B72B5-F4E4-44B0-A3D9-B55B748EFE90")]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class FileConverterExtension : SharpContextMenu
    {
        private const int MaximumProcessArgumentsLength = 8000; // https://learn.microsoft.com/en-us/troubleshoot/windows-client/shell-experience/command-line-string-limitation

        private PresetReference[] presetReferences = null;
        private string settingsSourcePath;
        private System.DateTime settingsLastWriteTimeUtc = System.DateTime.MinValue;
        private List<MenuEntry> menuEntries = new List<MenuEntry>();

        private HashSet<string> extensionCache = new HashSet<string>();

        private class MenuEntry
        {
            public PresetReference PresetReference;
            public bool Enabled;
            public int ExtensionRefCount;

            public MenuEntry(PresetReference presetReference)
            {
                this.PresetReference = presetReference;
                this.Enabled = false;
                this.ExtensionRefCount = 0;
            }
        }
        
        private bool DisplayPresetIcons
        {
            get
            {
                var registryKey = PathHelpers.FileConverterRegistryKey;
                if (registryKey == null)
                {
                    return false;
                }

                string displayPresetIcons = registryKey.GetValue("DisplayPresetIcons") as string;
                if (displayPresetIcons == null)
                {
                    return false;
                }

                if (!bool.TryParse(displayPresetIcons, out bool value))
                {
                    return false;
                }

                return value;
            }
        }

        private PresetReference[] PresetReferences
        {
            get
            {
                this.LoadExtensionSettingsIfNecessary();

                return this.presetReferences ?? new PresetReference[0];
            }
        }

        protected override bool CanShowMenu()
        {
            this.RefreshExtensionCacheFromSelectedItems();

            PresetReference[] presets = this.PresetReferences;
            foreach (string extension in this.extensionCache)
            {
                foreach (PresetReference presetReference in presets)
                {
                    if (presetReference.InputTypes != null && presetReference.InputTypes.Contains(extension))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override ContextMenuStrip CreateMenu()
        {
            this.RefreshPresetList();

            bool displayPresetIcons = this.DisplayPresetIcons;

            ContextMenuStrip menu = new ContextMenuStrip();

            ToolStripMenuItem fileConverterItem = new ToolStripMenuItem
            {
                Text = "ZFileConverter",
                Image = new Icon(Properties.Resources.ApplicationIcon, SystemInformation.SmallIconSize).ToBitmap(),
            };

            int menuItemIndex = 0;
            foreach (MenuEntry menuEntry in this.menuEntries)
            {
                menuItemIndex++;
                
                ToolStripMenuItem root = fileConverterItem;
                if (menuEntry.PresetReference.Folders != null)
                {
                    foreach (string folder in menuEntry.PresetReference.Folders)
                    {
                        ToolStripItem[] folderItems = root.DropDownItems.Find(folder, false);
                        if (folderItems.Length == 0)
                        {
                            ToolStripMenuItem folderItem = new ToolStripMenuItem
                            {
                                Name = folder,
                                Text = folder,
                                Image = new Icon(Properties.Resources.FolderIcon, SystemInformation.SmallIconSize).ToBitmap(),
                            };

                            root.DropDownItems.Add(folderItem);
                            root = folderItem;
                        }
                        else
                        {
                            root = folderItems[0] as ToolStripMenuItem;
                        }

                        if (root == null)
                        {
                            break;
                        }
                    }
                }

                if (root == null)
                {
                    // Fallback when something went wrong during folder creation.
                    root = fileConverterItem;
                }

                // Make each menu item text unique using invisible zero-width spaces
                // This prevents Windows Forms menu collision while being completely invisible to users
                string uniqueSuffix = new string('\u200B', menuItemIndex); // Zero-Width Space characters
                string displayText = menuEntry.PresetReference.Name + uniqueSuffix;

                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = displayText,
                    Enabled = menuEntry.Enabled
                };

                if (displayPresetIcons)
                {
                    subItem.Image = new Icon(Properties.Resources.PresetIcon, SystemInformation.SmallIconSize).ToBitmap();
                }

                root.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.ConvertFiles(menuEntry.PresetReference.FullName);
            }

            if (this.menuEntries.Count > 0)
            {
                fileConverterItem.DropDownItems.Add(new ToolStripSeparator());
            }

            {
                ToolStripMenuItem subItem = new ToolStripMenuItem
                {
                    Text = "Configure presets...",
                    Image = new Icon(Properties.Resources.SettingsIcon, SystemInformation.SmallIconSize).ToBitmap(),
                };

                fileConverterItem.DropDownItems.Add(subItem);
                subItem.Click += (sender, args) => this.OpenSettings();
            }

            menu.Items.Add(fileConverterItem);

            return menu;
        }

        private void RefreshExtensionCacheFromSelectedItems()
        {
            // Retrieve selected files extensions.
            this.extensionCache.Clear();
            foreach (string filePath in this.SelectedItemPaths)
            {
                string extension = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(extension))
                {
                    continue;
                }

                extension = extension.Substring(1).ToLowerInvariant();

                this.extensionCache.Add(extension);
            }
        }

        private void RefreshPresetList()
        {
            this.RefreshExtensionCacheFromSelectedItems();

            // Activate compatible menu entries.
            PresetReference[] presets = this.PresetReferences;
            this.menuEntries.Clear();
            foreach (string extension in this.extensionCache)
            {
                foreach (PresetReference presetReference in presets)
                {
                    if (presetReference.InputTypes == null || !presetReference.InputTypes.Contains(extension))
                    {
                        continue;
                    }

                    MenuEntry menuEntry = this.menuEntries.Find(entry => entry.PresetReference.FullName == presetReference.FullName);
                    if (menuEntry == null)
                    {
                        menuEntry = new MenuEntry(presetReference);
                        this.menuEntries.Add(menuEntry);
                    }

                    menuEntry.ExtensionRefCount++;
                }
            }

            // Enable presets compatible with all input files.
            foreach (MenuEntry menuEntry in this.menuEntries)
            {
                menuEntry.Enabled = menuEntry.ExtensionRefCount == this.extensionCache.Count;
            }
        }

        private void LoadExtensionSettingsIfNecessary()
        {
            string sourcePath = File.Exists(PathHelpers.UserSettingsFilePath)
                ? PathHelpers.UserSettingsFilePath
                : PathHelpers.DefaultSettingsFilePath;

            System.DateTime lastWriteTimeUtc = File.Exists(sourcePath)
                ? File.GetLastWriteTimeUtc(sourcePath)
                : System.DateTime.MinValue;

            if (this.presetReferences != null &&
                string.Equals(this.settingsSourcePath, sourcePath, System.StringComparison.OrdinalIgnoreCase) &&
                this.settingsLastWriteTimeUtc == lastWriteTimeUtc)
            {
                return;
            }

            try
            {
                XmlHelpers.LoadFromFile("Settings", sourcePath, out PresetReference[] loadedPresetReferences);
                if (loadedPresetReferences != null)
                {
                    this.presetReferences = loadedPresetReferences;
                    this.settingsSourcePath = sourcePath;
                    this.settingsLastWriteTimeUtc = lastWriteTimeUtc;
                }
            }
            catch
            {
                // Keep the last known-good menu if settings are briefly unavailable while
                // the application replaces the file, then retry on the next menu request.
                if (this.presetReferences == null &&
                    !string.Equals(sourcePath, PathHelpers.DefaultSettingsFilePath, System.StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(PathHelpers.DefaultSettingsFilePath))
                {
                    try
                    {
                        XmlHelpers.LoadFromFile("Settings", PathHelpers.DefaultSettingsFilePath, out this.presetReferences);
                        this.settingsSourcePath = PathHelpers.DefaultSettingsFilePath;
                        this.settingsLastWriteTimeUtc = File.GetLastWriteTimeUtc(PathHelpers.DefaultSettingsFilePath);
                    }
                    catch
                    {
                        // Explorer cannot surface this error. An empty menu is safer than
                        // throwing from the shell extension.
                    }
                }
            }

            if (this.presetReferences == null)
            {
                this.presetReferences = new PresetReference[0];
            }
        }

        private void OpenSettings()
        {
            string fileConverterPath = this.GetFileConverterPathOrShowError();
            if (string.IsNullOrEmpty(fileConverterPath))
            {
                return;
            }

            ProcessStartInfo processStartInfo = new ProcessStartInfo(fileConverterPath)
            {
                CreateNoWindow = false, 
                UseShellExecute = false, 
                RedirectStandardOutput = false,
            };

            // Build arguments string.
            StringBuilder stringBuilder = new StringBuilder();
            AppendQuotedArgument(stringBuilder, "--settings");
            
            processStartInfo.Arguments = stringBuilder.ToString();
            this.TryStartFileConverter(processStartInfo, null);
        }

        private static void AppendQuotedArgument(StringBuilder stringBuilder, string argument)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(' ');
            }

            if (argument == null)
            {
                argument = string.Empty;
            }

            stringBuilder.Append('"');
            int backslashCount = 0;
            for (int index = 0; index < argument.Length; index++)
            {
                char character = argument[index];
                if (character == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (character == '"')
                {
                    stringBuilder.Append('\\', backslashCount * 2 + 1);
                    stringBuilder.Append('"');
                    backslashCount = 0;
                    continue;
                }

                if (backslashCount > 0)
                {
                    stringBuilder.Append('\\', backslashCount);
                    backslashCount = 0;
                }

                stringBuilder.Append(character);
            }

            if (backslashCount > 0)
            {
                stringBuilder.Append('\\', backslashCount * 2);
            }

            stringBuilder.Append('"');
        }

        private void ConvertFiles(string presetName)
        {
            string fileConverterPath = this.GetFileConverterPathOrShowError();
            if (string.IsNullOrEmpty(fileConverterPath))
            {
                return;
            }

            void BuildConversionPresetArgument(StringBuilder sb)
            {
                AppendQuotedArgument(sb, "--conversion-preset");
                AppendQuotedArgument(sb, presetName);
            }

            // Build arguments string.
            StringBuilder stringBuilder = new StringBuilder();
            BuildConversionPresetArgument(stringBuilder);
            
            string fileListPath = null;
            foreach (var filePath in this.SelectedItemPaths)
            {
                AppendQuotedArgument(stringBuilder, filePath);

                if (stringBuilder.Length >= MaximumProcessArgumentsLength)
                {
                    // Alternative way of passing arguments to not overflow the command line.
                    stringBuilder.Clear();
                    BuildConversionPresetArgument(stringBuilder);

                    // Store list of file to convert in a file in Temp folder.
                    fileListPath = Path.Combine(Path.GetTempPath(), "file-converter-input-list.txt");
                    int index = 1;
                    while (File.Exists(fileListPath))
                    {
                        fileListPath = Path.Combine(Path.GetTempPath(), $"file-converter-input-list-{index}.txt");
                        index++;
                    }

                    using (FileStream file = File.OpenWrite(fileListPath))
                    using (StreamWriter writer = new StreamWriter(file))
                    {
                        foreach (var path in this.SelectedItemPaths)
                        {
                            writer.WriteLine(path);
                        }
                    }

                    AppendQuotedArgument(stringBuilder, "--input-files");
                    AppendQuotedArgument(stringBuilder, fileListPath);
                    break;
                }
            }

            var processStartInfo = new ProcessStartInfo(fileConverterPath)
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                Arguments = stringBuilder.ToString(),
            };

            Process exeProcess = this.TryStartFileConverter(processStartInfo, fileListPath);
            if (exeProcess == null)
            {
                return;
            }

            exeProcess.EnableRaisingEvents = true;
            exeProcess.Exited += (sender, args) =>
            {
                DeleteInputListFile(fileListPath);
            };
        }

        private string GetFileConverterPathOrShowError()
        {
            string fileConverterPath = PathHelpers.FileConverterPath;
            if (string.IsNullOrEmpty(fileConverterPath))
            {
                MessageBox.Show("Can't retrieve the file converter executable path. You should try to reinstall the application.");
                return null;
            }

            if (!File.Exists(fileConverterPath))
            {
                MessageBox.Show($"Can't find the file converter executable ({fileConverterPath}). You should try to reinstall the application.");
                return null;
            }

            return fileConverterPath;
        }

        private Process TryStartFileConverter(ProcessStartInfo processStartInfo, string temporaryInputListPath)
        {
            try
            {
                Process process = Process.Start(processStartInfo);
                if (process != null)
                {
                    return process;
                }

                MessageBox.Show("Failed to start File Converter.");
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Failed to start File Converter. {exception.Message}");
            }

            DeleteInputListFile(temporaryInputListPath);
            return null;
        }

        private static void DeleteInputListFile(string fileListPath)
        {
            if (fileListPath == null)
            {
                return;
            }

            try
            {
                File.Delete(fileListPath);
            }
            catch
            {
            }
        }
    }
}
