using System.Windows.Controls;
using Shell_WebP_Converter.CustomElements.PresetSettingTabControls;

namespace Shell_WebP_Converter.CustomElements
{
    public class AdvancedPresetJPG : AdvancedPresetBase
    {
        public AdvancedPresetJPG()
        {
            SettingsHost.Content = new JPG_SettingsTabControl();
        }

        private JPG_SettingsTabControl JPGControl => SettingsHost.Content as JPG_SettingsTabControl ?? throw new InvalidOperationException("JPG control not initialized");

        public TextBox QualitySettingTextBox => (TextBox)JPGControl.FindName("QualitySettingTextBox");
        public TextBox CompressionSizeThresholdTextBox => (TextBox)JPGControl.FindName("CompressionSizeThresholdTextBox");
        public ComboBox SizeMeasurmentUnitComboBox => (ComboBox)JPGControl.FindName("SizeMeasurmentUnitComboBox");
        public TextBox SSIM_SettingTextBox => (TextBox)JPGControl.FindName("SSIM_SettingTextBox");
        public ComboBox SSIM_CompressionComboBox => (ComboBox)JPGControl.FindName("SSIM_CompressionComboBox");
    }
}
