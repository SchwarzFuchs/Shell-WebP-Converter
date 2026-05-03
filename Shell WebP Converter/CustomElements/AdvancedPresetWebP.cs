using System.Windows.Controls;
using Shell_WebP_Converter.CustomElements.PresetSettingTabControls;

namespace Shell_WebP_Converter.CustomElements
{
    public class AdvancedPresetWebP : AdvancedPresetBase
    {
        public AdvancedPresetWebP()
        {
            SettingsHost.Content = new WebP_SettingsTabControl();
        }

        private WebP_SettingsTabControl WebPControl => SettingsHost.Content as WebP_SettingsTabControl ?? throw new InvalidOperationException("WebP control not initialized");

        public TextBox QualitySettingTextBox => (TextBox)WebPControl.FindName("QualitySettingTextBox");
        public ComboBox CompressionComboBox => (ComboBox)WebPControl.FindName("CompressionComboBox");
        public TextBox CompressionSizeThresholdTextBox => (TextBox)WebPControl.FindName("CompressionSizeThresholdTextBox");
        public ComboBox SizeMeasurmentUnitComboBox => (ComboBox)WebPControl.FindName("SizeMeasurmentUnitComboBox");
        public TextBox SSIM_SettingTextBox => (TextBox)WebPControl.FindName("SSIM_SettingTextBox");
        public ComboBox SSIM_CompressionComboBox => (ComboBox)WebPControl.FindName("SSIM_CompressionComboBox");
    }
}
