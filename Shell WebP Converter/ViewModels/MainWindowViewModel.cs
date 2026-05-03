using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using Shell_WebP_Converter.CustomElements;
using Shell_WebP_Converter.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace Shell_WebP_Converter.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private string ConverterPath;
        private static readonly Regex PresetsRegex = new Regex(@"^[0-9\s;-]*$");
        private static readonly Regex ExtensionsRegex = new Regex(@"^[a-zA-Z0-9\s;]*$");

        [ObservableProperty]
        private string _extensions = "jpeg; jpg; png; webp;";

        [ObservableProperty]
        private bool _addConversionEntryForFolders = true;

        [ObservableProperty]
        private bool _notifyWhenFolderProcessingEnds = true;

        [ObservableProperty]
        private bool _overwriteFiles = true;

        [ObservableProperty]
        private bool _addConversionToJPG_PNG_Option = false;

        [ObservableProperty]
        private string _presetsText = "100; 99; 90; 75; 60; 50; 25; -1";

        [ObservableProperty]
        private int _compressionValue = 4;

        [ObservableProperty]
        private bool _deleteOriginal = false;

        [ObservableProperty]
        private int _modeTabIndex = 1;

        public static readonly Regex PresetsRegexStatic = PresetsRegex;
        public static readonly Regex ExtensionsRegexStatic = ExtensionsRegex;

        public MainWindowViewModel()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ConverterPath = (System.Diagnostics.Process.GetCurrentProcess().MainModule ?? throw new Exception("Program can't get the path of executable file")).FileName;
            LoadSettings();
        }

        private void LoadSettings()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(App.regKeyPath))
            {
                if (key != null)
                {
                    string? value = key.GetValue("presets")?.ToString();
                    if (value != null && value.Length > 0)
                    {
                        PresetsText = value;
                    }

                    value = key.GetValue("extensions")?.ToString();
                    if (value != null && value.Length > 0)
                    {
                        Extensions = value;
                    }

                    DeleteOriginal = bool.Parse((key.GetValue("deleteOriginal") ?? "false").ToString() ?? "false");
                    NotifyWhenFolderProcessingEnds = bool.Parse((key.GetValue("notifyWhenFolderProcessingEnds") ?? "true").ToString() ?? "true");
                    OverwriteFiles = bool.Parse((key.GetValue("overwriteFiles") ?? "true").ToString() ?? "true");
                    AddConversionEntryForFolders = bool.Parse((key.GetValue("addMenuEntryForFolders") ?? "true").ToString() ?? "true");
                    CompressionValue = byte.Parse((key.GetValue("compression") ?? "4").ToString() ?? "4");
                    AddConversionToJPG_PNG_Option = bool.Parse((key.GetValue("addConversionToJPG_PNG_Option") ?? "false").ToString() ?? "false");

                    string lastMode = (key.GetValue("lastMode") ?? "advanced").ToString();
                    ModeTabIndex = lastMode == "basic" ? 0 : 1;
                }
            }
        }

        private void LoadAdvancedPresets()
        {

        }

        [RelayCommand]
        public void UpdateMenu((AdvancedPresetsTableWebP webpTable, AdvancedPresetsTableJPG jpgTable, AdvancedPresetsTablePNG pngTable) tables)
        {
            try
            {
                var extensions = ParseExtensions(Extensions, AddConversionEntryForFolders);
                RegistryHelper.RemoveAllConversionContextMenus(extensions);

                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(App.regKeyPath))
                {
                    key.SetValue("extensions", Extensions);
                    key.SetValue("addMenuEntryForFolders", AddConversionEntryForFolders);
                    key.SetValue("notifyWhenFolderProcessingEnds", NotifyWhenFolderProcessingEnds);
                    key.SetValue("overwriteFiles", OverwriteFiles);
                    key.SetValue("addConversionToJPG_PNG_Option", AddConversionToJPG_PNG_Option);

                    List<WebP_Preset>? presets;
                    List<JPG_Preset>? jpgPresets = null;
                    List<PNG_Preset>? pngPresets = null;
                    if (ModeTabIndex == 0)
                    {
                        key.SetValue("presets", PresetsText);
                        key.SetValue("compression", CompressionValue);
                        key.SetValue("deleteOriginal", DeleteOriginal);
                        key.SetValue("lastMode", "basic");
                        presets = ParsePresetsBasic(PresetsText);
                        if (presets != null)
                        {
                            RegistryHelper.AddConversionContextMenu(extensions, presets, ConverterPath, NotifyWhenFolderProcessingEnds, OverwriteFiles, AddConversionToJPG_PNG_Option, jpgPresets, pngPresets);
                        }
                        else return;
                    }
                    else if (ModeTabIndex == 1)
                    {
                        var webPresets = new List<WebP_Preset>();
                        jpgPresets = new List<JPG_Preset>();
                        pngPresets = new List<PNG_Preset>();

                        var webpParsed = tables.webpTable.ParsePresets();
                        if (webpParsed == null)
                            return;
                        foreach (var p in webpParsed)
                        {
                            if (p is WebP_Preset wp)
                                webPresets.Add(wp);
                        }

                        var jpgParsed = tables.jpgTable.ParsePresets();
                        if (jpgParsed == null)
                            return;
                        foreach (var p in jpgParsed)
                        {
                            if (p is JPG_Preset jp)
                                jpgPresets.Add(jp);
                        }

                        var pngParsed = tables.pngTable.ParsePresets();
                        if (pngParsed == null)
                            return;
                        foreach (var p in pngParsed)
                        {
                            if (p is PNG_Preset pp)
                                pngPresets.Add(pp);
                        }

                        key.SetValue("lastMode", "advanced");
                        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shell WebP Converter"));
                        string appDataDirLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shell WebP Converter");
                        Directory.CreateDirectory(appDataDirLocal);
                        File.WriteAllText(Path.Combine(appDataDirLocal, "Advanced presets list.json"), JsonConvert.SerializeObject(webPresets, Formatting.Indented));
                        File.WriteAllText(Path.Combine(appDataDirLocal, "Advanced presets list JPG.json"), JsonConvert.SerializeObject(jpgPresets, Formatting.Indented));
                        File.WriteAllText(Path.Combine(appDataDirLocal, "Advanced presets list PNG.json"), JsonConvert.SerializeObject(pngPresets, Formatting.Indented));

                        RegistryHelper.AddConversionContextMenu(extensions, webPresets, ConverterPath, NotifyWhenFolderProcessingEnds, OverwriteFiles, AddConversionToJPG_PNG_Option, jpgPresets, pngPresets);
                    }
                }

                MessageBox.Show(Shell_WebP_Converter.Resources.Resources.MenuUpdateSuccess);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        public void ClearMenu(object? parameter)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(App.regKeyPath))
                {
                    if (key != null)
                    {
                        string? value = key.GetValue("extensions")?.ToString();
                        if (value != null && value.Length > 0)
                        {
                            RegistryHelper.RemoveAllConversionContextMenus(ParseExtensions(value));
                        }
                    }
                }
                MessageBox.Show(Shell_WebP_Converter.Resources.Resources.MenuClearSuccess);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        internal List<WebP_Preset> ParsePresetsBasic(string presetsString)
        {
            List<WebP_Preset> presets = new List<WebP_Preset>();
            if (string.IsNullOrWhiteSpace(presetsString))
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.EmptyPresetsList}");
            }
            var stringValues = presetsString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var str in stringValues)
            {
                if (int.TryParse(str.Trim(), out int number))
                {
                    if (number >= -1 && number <= 100)
                    {
                        presets.Add(new WebP_Preset { PresetMode = number == -1 ? LossyPresetModes.Custom : LossyPresetModes.ToNQuality, Quality = number, Compression = (byte)CompressionValue, DeleteOriginal = DeleteOriginal, Name = "" });
                    }
                    else
                    {
                        throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.InvalidValue}: '{str}'");
                    }
                }
                else
                {
                    throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.InvalidValue}: '{str}'");
                }
            }
            if (presets.Count == 0)
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.Presets}");
            }
            return presets;
        }

        internal List<string> ParseExtensions(string extensionsString, bool addMenuEntryForFolders = false)
        {
            List<string> extensions = new List<string>();
            if (string.IsNullOrWhiteSpace(extensionsString))
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.EmptyExtensionsList}");
            }
            var stringValues = extensionsString.Replace(" ", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var str in stringValues)
            {
                if (RegistryHelper.AllowedFileExtensions.Contains($".{str}."))
                {
                    extensions.Add(str);
                }
                else
                {
                    throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.UnsupportedExtension}: '{str}'");
                }
            }
            if (addMenuEntryForFolders == true && !extensions.Contains("folder"))
            {
                extensions.Add("folder");
            }
            if (extensions.Count == 0)
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.EmptyExtensionsList}");
            }
            return extensions;
        }
    }
}
