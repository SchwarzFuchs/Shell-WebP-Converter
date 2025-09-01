using Microsoft.Win32;
using Shell_WebP_Converter.Resources;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shell_WebP_Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string converterPath;
        private static readonly Regex presetsRegex = new Regex(@"^[0-9\s;-]*$");
        private static readonly Regex extensionsRegex = new Regex(@"^[a-zA-Z0-9\s;]*$");

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; 

            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
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
                        ExtensionsTextBox.Text = value;
                    }
                    DeleteOriginalFileCheckbox.IsChecked = bool.Parse((key.GetValue("deleteOriginal") ?? "false").ToString() ?? "false");
                    CompressionValueComboBox.SelectedIndex = byte.Parse((key.GetValue("compression") ?? "4").ToString() ?? "4");
                }
            }
        }

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
                var presets = RegistryHelper.ParsePresets(PresetsTextBox.Text);
                var extensions = RegistryHelper.ParseExtensions(ExtensionsTextBox.Text);
                RegistryHelper.RemoveWebPConversionContextMenu(extensions);
                RegistryHelper.AddWebPConversionContextMenu(extensions, presets, (byte)CompressionValueComboBox.SelectedIndex, DeleteOriginalFileCheckbox.IsChecked ?? false, converterPath);
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(App.regKeyPath))
                {
                    key.SetValue("presets", PresetsTextBox.Text);
                    key.SetValue("extensions", ExtensionsTextBox.Text);
                    key.SetValue("compression", CompressionValueComboBox.SelectedIndex);
                    key.SetValue("deleteOriginal", DeleteOriginalFileCheckbox.IsChecked ?? false);
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
                            RegistryHelper.RemoveWebPConversionContextMenu(RegistryHelper.ParseExtensions(value));
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
            var dicts = Application.Current.Resources.MergedDictionaries;
            var Theme = dicts.FirstOrDefault(d => d.Source.OriginalString.Contains("DarkTheme.xaml"));
            if (Theme != null) dicts.Remove(Theme);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}