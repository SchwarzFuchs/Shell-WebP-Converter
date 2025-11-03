using Microsoft.Win32;
using Newtonsoft.Json;
using Shell_WebP_Converter.CustomElements;
using Shell_WebP_Converter.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Shell_WebP_Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string converterPath;
        private static readonly Regex presetsRegex = new Regex(@"^[0-9\s;-]*$");
        private static readonly Regex extensionsRegex = new Regex(@"^[a-zA-Z0-9\s;]*$");
        private string _extensions = "jpeg; jpg; png; webp;";
        private bool _addConversionEntryForFolders;
        public string Extensions
        {
            get { return _extensions; }
            set {
                if (_extensions != value)
                {
                    _extensions = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool addConversionEntryForFolders
        {
            get { return _addConversionEntryForFolders; }
            set
            {
                if (_addConversionEntryForFolders != value)
                {
                    _addConversionEntryForFolders = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; 
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                DataContext = this;
                if (key != null)
                {
                    int value = (int)(key.GetValue("AppsUseLightTheme") ?? 1);
                    if (value == 1)
                    {
                        TurnDarkThemeOff();
                    }
                }
            }
            string title = $"{Shell_WebP_Converter.Resources.Resources.MainWindowTitle} — {Shell_WebP_Converter.Resources.Resources.Settings}";
            converterPath = (Process.GetCurrentProcess().MainModule??throw new Exception("Program can't get the path of executable file")).FileName;
            InitializeComponent();
            this.Title = title;
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(App.regKeyPath))
            {
                if (key != null)
                {
                    string? value = key.GetValue("presets")?.ToString();
                    if (value != null && value.Length > 0)
                    {
                        PresetsTextBox.Text = value;
                    }
                    value = key.GetValue("extensions")?.ToString();
                    if (value != null && value.Length > 0)
                    {
                        Extensions = value;
                    }
                    DeleteOriginalFileCheckbox.IsChecked = bool.Parse((key.GetValue("deleteOriginal") ?? "false").ToString() ?? "false");
                    addConversionEntryForFolders = bool.Parse((key.GetValue("addMenuEntryForFolders") ?? "false").ToString() ?? "false");
                    CompressionValueComboBox.SelectedIndex = byte.Parse((key.GetValue("compression") ?? "4").ToString() ?? "4");
                    if ((key.GetValue("lastMode") ?? "basic").ToString() == "advanced")
                    {
                        ModeTabToggleSwitch.Position = ToggleSwitch.TogglePosition.Right;
                        MainTabControl.SelectedIndex = 1;
                    }
                    else
                    {
                        ModeTabToggleSwitch.Position = ToggleSwitch.TogglePosition.Left;
                        MainTabControl.SelectedIndex = 0;
                    }
                }
            }
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shell WebP Converter", "Advanced presets list.json")))
            {
                AdvancedPresetsTable.FillPresetsGridFromList((List<Preset>)(JsonConvert.DeserializeObject<List<Preset>>(File.ReadAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shell WebP Converter", "Advanced presets list.json"))) ?? new List<Preset>()));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private void PresetsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !presetsRegex.IsMatch(e.Text);
        }

        private void ExtensionsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !extensionsRegex.IsMatch(e.Text);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                TextBox textBox = (TextBox)sender;
                int caretIndex = textBox.CaretIndex;
                textBox.Text = textBox.Text.Insert(caretIndex, " ");
                textBox.CaretIndex = caretIndex + 1;
            }
        }

        private void UpdateMenuButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var extensions = ParseExtensions(Extensions, addConversionEntryForFolders);
                RegistryHelper.RemoveWebPConversionContextMenu(extensions);
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(App.regKeyPath))
                {
                    key.SetValue("extensions", Extensions);
                    key.SetValue("compression", CompressionValueComboBox.SelectedIndex);
                    key.SetValue("addMenuEntryForFolders", addConversionEntryForFolders);
                    List<Preset> presets;
                    if (ModeTabToggleSwitch.Position == ToggleSwitch.TogglePosition.Left)
                    {
                        key.SetValue("presets", PresetsTextBox.Text);
                        key.SetValue("deleteOriginal", DeleteOriginalFileCheckbox.IsChecked ?? false);
                        key.SetValue("lastMode", "basic");
                        presets = ParsePresetsBasic(PresetsTextBox.Text);
                        if (presets != null)
                        {
                            RegistryHelper.AddWebPConversionContextMenu(extensions, presets, converterPath);
                        }
                        else return;
                    }
                    else if (ModeTabToggleSwitch.Position == ToggleSwitch.TogglePosition.Right)
                    {
                        presets = AdvancedPresetsTable.ParsePresets();
                        if (presets != null)
                        {
                            key.SetValue("lastMode", "advanced");
                            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shell WebP Converter"));
                            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shell WebP Converter", "Advanced presets list.json"), JsonConvert.SerializeObject(presets));
                            RegistryHelper.AddWebPConversionContextMenu(extensions, presets, converterPath);
                        }
                        else return;
                    }
                }
                MessageBox.Show(Shell_WebP_Converter.Resources.Resources.MenuUpdateSuccess);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ClearMenuButton_Click(object sender, RoutedEventArgs e)
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
                            RegistryHelper.RemoveWebPConversionContextMenu(ParseExtensions(value));
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

        private void DeleteOriginalFileCheckbox_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        void TurnDarkThemeOff()
        {
            var Theme = System.Windows.Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains("DarkTheme.xaml"));
            if (Theme != null) System.Windows.Application.Current.Resources.MergedDictionaries.Remove(Theme);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void ToggleSwitch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToggleSwitch.TogglePosition position = ((ToggleSwitch)sender).Position;
            if (position == ToggleSwitch.TogglePosition.Left)
            {
                MainTabControl.SelectedIndex = 0;
            }
            else if (position == ToggleSwitch.TogglePosition.Right) 
            { 
                MainTabControl.SelectedIndex = 1;
            }
        }

        internal List<Preset> ParsePresetsBasic(string presetsString)
        {
            List<Preset> presets = new List<Preset>();
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
                        presets.Add(new Preset { PresetMode = number == -1 ? PresetMode.Custom : PresetMode.ToNQuality, Quality = number, Compression = (byte)CompressionValueComboBox.SelectedIndex, DeleteOriginal = DeleteOriginalFileCheckbox.IsChecked ?? false, Name = "" });
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
                if (RegistryHelper.allowedFileExtensions.Contains($".{str}."))
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