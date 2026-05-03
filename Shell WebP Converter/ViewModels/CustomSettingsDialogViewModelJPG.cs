using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Shell_WebP_Converter.CustomElements;
using Shell_WebP_Converter.Models;
using System;

namespace Shell_WebP_Converter.ViewModels
{
    public partial class CustomSettingsDialogViewModelJPG : CustomSettingsDialogViewModelBase
    {
        private JPGConversionOptions Options { get; set; }

        [ObservableProperty]
        private bool _useCustomQualitySettings = true;

        [ObservableProperty]
        private int _quality = 80;

        [ObservableProperty]
        private float _compressionSizeThreshold = 2;

        [ObservableProperty]
        private int _sizeMeasurementUnit = 1;

        [ObservableProperty]
        private bool _lowerTheResolutionWhenNecessary = true;

        public CustomSettingsDialogViewModelJPG(JPGConversionOptions options) : base()
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
                        UseCustomQualitySettings = bool.Parse((key.GetValue("LastCustomJPGRadio") ?? false).ToString() ?? "false");
                        LowerTheResolutionWhenNecessary = bool.Parse((key.GetValue("LastCustomJPGUseDownscaling") ?? true).ToString() ?? "true");
                        Quality = int.Parse((key.GetValue("LastCustomJPGQuality") ?? "80").ToString() ?? "80");
                        CompressionSizeThreshold = float.Parse((key.GetValue("LastCustomJPGSizeThreshold") ?? "2").ToString() ?? "2");
                        SizeMeasurementUnit = (int)(key.GetValue("LastCustomJPGSizeUnit") ?? 1);
                        LoadDeleteOriginalFromRegistry("LastCustomJPG");
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
                }
                else
                {
                    Options.Mode = LossyPresetModes.ToNSize;
                    Options.Quality = (int)(CompressionSizeThreshold * (float)Math.Pow(1024, SizeMeasurementUnit + 1));
                    if (Options.Quality == 0) throw new Exception("Size can't be zero");
                    Options.UseDownscaling = LowerTheResolutionWhenNecessary;
                }

                Options.DeleteOriginal = DeleteOriginalFile;
                Options.Postfix = Postfix.Trim();
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
                        key.SetValue("LastCustomJPGRadio", UseCustomQualitySettings);
                        key.SetValue("LastCustomJPGQuality", Quality.ToString());
                        key.SetValue("LastCustomJPGSizeThreshold", CompressionSizeThreshold.ToString());
                        key.SetValue("LastCustomJPGSizeUnit", SizeMeasurementUnit);
                        key.SetValue("LastCustomJPGUseDownscaling", LowerTheResolutionWhenNecessary);
                        SaveDeleteOriginalToRegistry("LastCustomJPG");
                    }
                }
            }
            catch { }
        }
    }
}
