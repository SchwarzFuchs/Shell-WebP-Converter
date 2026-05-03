using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;

namespace Shell_WebP_Converter.ViewModels
{
    public abstract partial class CustomSettingsDialogViewModelBase : ObservableObject
    {
        protected static readonly Regex OnlyDigitsRegex = new Regex(@"^[0-9]+$");
        protected static readonly Regex ThresholdRegex = new Regex(@"^[0-9.,]+$");

        [ObservableProperty]
        private bool _deleteOriginalFile = false;

        [ObservableProperty]
        private bool _hasErrors = false;

        [ObservableProperty]
        protected string _postfix = "";

        protected CustomSettingsDialogViewModelBase()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        protected void LoadDeleteOriginalFromRegistry(string registryPrefix)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(App.regKeyPath))
                {
                    if (key != null)
                    {
                        DeleteOriginalFile = bool.Parse((key.GetValue($"{registryPrefix}DeleteOriginal") ?? "false").ToString() ?? "false");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        protected void SaveDeleteOriginalToRegistry(string registryPrefix)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(App.regKeyPath))
                {
                    if (key != null)
                    {
                        key.SetValue($"{registryPrefix}DeleteOriginal", DeleteOriginalFile);
                    }
                }
            }
            catch { }
        }

        protected void ResetHasErrors()
        {
            HasErrors = false;
        }

        [RelayCommand]
        public virtual void Cancel()
        {
        }

        public static bool ValidateQualityInput(string text)
        {
            return OnlyDigitsRegex.IsMatch(text);
        }

        public static bool ValidateThresholdInput(string text)
        {
            return ThresholdRegex.IsMatch(text);
        }
    }
}
