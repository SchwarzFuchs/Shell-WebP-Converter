using Shell_WebP_Converter.ViewModels;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Linq;
using Newtonsoft.Json;
using Shell_WebP_Converter.CustomElements;
using Shell_WebP_Converter.Models;
using System.Collections.Generic;
using System.IO;

namespace Shell_WebP_Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel { get; set; }
        private static readonly Regex PresetsRegex = new Regex(@"^[0-9\s;-]*$");
        private static readonly Regex ExtensionsRegex = new Regex(@"^[a-zA-Z0-9\s;]*$");

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

            ViewModel = new MainWindowViewModel();
            string title = $"{Shell_WebP_Converter.Resources.Resources.MainWindowTitle} — {Shell_WebP_Converter.Resources.Resources.Settings}";
            InitializeComponent();
            this.Title = title;
            DataContext = ViewModel;
            UpdateModeTabPosition();
            LoadAdvancedPresets();
        }

        private void LoadAdvancedPresets()
        {
            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shell WebP Converter");
            Directory.CreateDirectory(appDataDir);
            string webpFile = Path.Combine(appDataDir, "Advanced presets list.json"); //Kept the original name for backward compatibility, but it will only be used for WebP presets.
            string jpgFile = Path.Combine(appDataDir, "Advanced presets list JPG.json");
            string pngFile = Path.Combine(appDataDir, "Advanced presets list PNG.json");

            List<WebP_Preset> webPresetsLoaded = AdvancedPresetsTableWebP.demonstrationWebP_PresetSet;
            if (File.Exists(webpFile))
            {
                webPresetsLoaded = JsonConvert.DeserializeObject<List<WebP_Preset>>(File.ReadAllText(webpFile)) ?? AdvancedPresetsTableWebP.demonstrationWebP_PresetSet;
            }

            List<JPG_Preset> jpgPresetsLoaded = AdvancedPresetsTableJPG.demonstrationJPG_PresetSet;
            if (File.Exists(jpgFile))
            {
                jpgPresetsLoaded = JsonConvert.DeserializeObject<List<JPG_Preset>>(File.ReadAllText(jpgFile)) ?? jpgPresetsLoaded;
            }

            List<PNG_Preset> pngPresetsLoaded = AdvancedPresetsTablePNG.demonstrationPNG_PresetSet;
            if (File.Exists(pngFile))
            {
                pngPresetsLoaded = JsonConvert.DeserializeObject<List<PNG_Preset>>(File.ReadAllText(pngFile)) ?? pngPresetsLoaded;
            }

            AdvancedPresetsTableWebP.FillPresetsGridFromList(webPresetsLoaded);
            AdvancedPresetsTableJPG.FillPresetsGridFromList(jpgPresetsLoaded);
            AdvancedPresetsTablePNG.FillPresetsGridFromList(pngPresetsLoaded);
        }

        private void UpdateModeTabPosition()
        {
            if (ModeTabToggleSwitch == null) return;
            ModeTabToggleSwitch.Position = ViewModel.ModeTabIndex == 0
                ? CustomElements.ToggleSwitch.TogglePosition.Left
                : CustomElements.ToggleSwitch.TogglePosition.Right;
        }

        private void PresetsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !PresetsRegex.IsMatch(e.Text);
        }

        private void ExtensionsTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ExtensionsRegex.IsMatch(e.Text);
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
            var presetsTablesInfo = (AdvancedPresetsTableWebP, AdvancedPresetsTableJPG, AdvancedPresetsTablePNG);
            ViewModel.UpdateMenuCommand.Execute(presetsTablesInfo);
        }

        void TurnDarkThemeOff()
        {
            var Theme = System.Windows.Application.Current.Resources.MergedDictionaries.FirstOrDefault(d => d.Source.OriginalString.Contains("DarkTheme.xaml"));
            if (Theme != null) System.Windows.Application.Current.Resources.MergedDictionaries.Remove(Theme);
        }

        private void ToggleSwitch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CustomElements.ToggleSwitch.TogglePosition position = ((CustomElements.ToggleSwitch)sender).Position;
            if (position == CustomElements.ToggleSwitch.TogglePosition.Left)
            {
                MainTabControl.SelectedIndex = 0;
                ViewModel.ModeTabIndex = 0;
            }
            else if (position == CustomElements.ToggleSwitch.TogglePosition.Right) 
            { 
                MainTabControl.SelectedIndex = 1;
                ViewModel.ModeTabIndex = 1;
            }
        }

        private void PresetsRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (PresetsTablesTabControl == null) return;

            if (AnyToWebP_PresetsRadioButton.IsChecked == true)
            {
                PresetsTablesTabControl.SelectedIndex = 0;
            }
            else if (WebP_ToJPG_PresetsRadioButton.IsChecked == true)
            {
                PresetsTablesTabControl.SelectedIndex = 1;
            }
            else if (WebP_ToPNG_PresetsRadioButton.IsChecked == true)
            {
                PresetsTablesTabControl.SelectedIndex = 2;
            }
        }
    }
} 