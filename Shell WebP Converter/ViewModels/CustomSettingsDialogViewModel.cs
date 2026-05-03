using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Shell_WebP_Converter.CLI;
using Shell_WebP_Converter.CustomElements;
using Shell_WebP_Converter.Models;
using System;

namespace Shell_WebP_Converter.ViewModels
{
    public partial class CustomSettingsDialogViewModel : CustomSettingsDialogViewModelBase
    {
        private WebPConversionOptions Options { get; set; }

        [ObservableProperty]
        private bool _useCustomQualitySettings = true;

        [ObservableProperty]
        private int _quality = 80;

        [ObservableProperty]
        private int _compressionValue = 4;

        [ObservableProperty]
        private float _compressionSizeThreshold = 2.0f;

        [ObservableProperty]
        private int _sizeMeasurementUnit = 1;

        [ObservableProperty]
        private bool _lowerTheResolutionWhenNecessary = true;

        public CustomSettingsDialogViewModel(WebPConversionOptions options) : base()
        {
            Options = options;
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(App.regKeyPath))
                {
                    if (key != null)
                    {
                        UseCustomQualitySettings = bool.Parse((key.GetValue("LastCustomWebPRadio") ?? false).ToString() ?? "false");
                        LowerTheResolutionWhenNecessary = bool.Parse((key.GetValue("LastCustomWebPUseDownscaling") ?? true).ToString() ?? "true");
                        Quality = int.Parse((key.GetValue("LastCustomWebPQuality") ?? "80").ToString() ?? "80");
                        CompressionValue = (int)(key.GetValue("LastCustomWebPCompressionValue") ?? 4);
                        CompressionSizeThreshold = float.Parse((key.GetValue("LastCustomWebPSizeThreshold") ?? "2").ToString() ?? "2");
                        SizeMeasurementUnit = (int)(key.GetValue("LastCustomWebPSizeUnit") ?? 1);
                        DeleteOriginalFile = bool.Parse((key.GetValue("LastCustomWebPDeleteOriginal") ?? "false").ToString() ?? "false");
                        Postfix = "";
                    }
                    else
                    {
                        UseCustomQualitySettings = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        public void SelectCustomQuality()
        {

        }

        [RelayCommand]
        public void SelectCompressToThreshold()
        {

        }

        [RelayCommand]
        public void OK()
        {
            ResetHasErrors();
            try
            {
                if (UseCustomQualitySettings)
                {
                    Options.Mode = LossyPresetModes.ToNQuality;
                    Options.Quality = Quality;
                    Options.Compression = (byte)CompressionValue;
                }
                else
                {
                    Options.Mode = LossyPresetModes.ToNSize;
                    Options.Quality = (int)(CompressionSizeThreshold * (float)Math.Pow(1024, SizeMeasurementUnit + 1));
                    if (Options.Quality == 0) throw new Exception("Size can't be zero");
                    Options.Compression = 255;
                    Options.UseDownscaling = LowerTheResolutionWhenNecessary;
                }

                Options.DeleteOriginal = DeleteOriginalFile;
                Options.Postfix = Postfix.Trim();

                if (AdvancedPresetBase.windowsPathForbiddenSymbolsRegex.IsMatch(Options.Postfix))
                {
                    throw new Exception($"{Shell_WebP_Converter.Resources.Resources.Postfix}: {Options.Postfix} — {Shell_WebP_Converter.Resources.Resources.ForbiddenWindowsSymbols}");
                }

                SaveSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                HasErrors = true;
            }
        }

        private void SaveSettings()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(App.regKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue("LastCustomWebPRadio", UseCustomQualitySettings);
                        key.SetValue("LastCustomWebPQuality", Quality.ToString());
                        key.SetValue("LastCustomWebPCompressionValue", CompressionValue);
                        key.SetValue("LastCustomWebPSizeThreshold", CompressionSizeThreshold.ToString());
                        key.SetValue("LastCustomWebPSizeUnit", SizeMeasurementUnit);
                        key.SetValue("LastCustomWebPUseDownscaling", LowerTheResolutionWhenNecessary);
                        key.SetValue("LastCustomWebPDeleteOriginal", DeleteOriginalFile);
                    }
                }
            }
            catch { }
        }
    }
}
