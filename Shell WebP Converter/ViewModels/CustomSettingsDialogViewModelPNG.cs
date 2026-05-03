using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Shell_WebP_Converter.CLI;
using System;

namespace Shell_WebP_Converter.ViewModels
{
    public partial class CustomSettingsDialogViewModelPNG : CustomSettingsDialogViewModelBase
    {
        private PNGConversionOptions Options { get; set; }

        [ObservableProperty]
        private int _compression = 4;

        [ObservableProperty]
        private int _filter = 0;

        public CustomSettingsDialogViewModelPNG(PNGConversionOptions options) : base()
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
                        Compression = (int)(key.GetValue("LastCustomPNGCompression") ?? 4);
                        Filter = (int)(key.GetValue("LastCustomPNGFilter") ?? 0);
                        LoadDeleteOriginalFromRegistry("LastCustomPNG");
                        Postfix = "";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        public void OK()
        {
            ResetHasErrors();
            try
            {
                Options.Compression = (byte)Compression;
                Options.Filter = (byte)Filter;
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
                        key.SetValue("LastCustomPNGCompression", Compression);
                        key.SetValue("LastCustomPNGFilter", Filter);
                        SaveDeleteOriginalToRegistry("LastCustomPNG");
                    }
                }
            }
            catch { }
        }
    }
}
