using System.Windows;
using System.Windows.Controls;
using System.Linq;
using Shell_WebP_Converter.CustomElements.PresetSettingTabControls;

namespace Shell_WebP_Converter.CustomElements
{
    public class AdvancedPresetPNG : AdvancedPresetBase
    {
        public AdvancedPresetPNG()
        {
            SettingsHost.Content = new PNG_SettingsTabControl();

            ModeSelectorComboBox.Items.Clear();
            ModeSelectorComboBox.Items.Add(new ComboBoxItem { Content = Shell_WebP_Converter.Resources.Resources.ToCompressionN });
            ModeSelectorComboBox.Items.Add(new ComboBoxItem { Content = Shell_WebP_Converter.Resources.Resources.Customizable });
            ModeSelectorComboBox.SelectedIndex = 0;
        }

        protected override bool CanChangeMode(int newIndex)
        {
            const int pngCustomIndex = 1;
            if (newIndex != pngCustomIndex) return true;

            var parent = this.Parent as Grid;
            if (parent == null) return true;

            bool exists = parent.Children.OfType<AdvancedPresetPNG>().Any(p => p != this && p.ModeSelectorComboBox.SelectedIndex == pngCustomIndex);
            if (exists)
            {
                MessageBox.Show(Shell_WebP_Converter.Resources.Resources.CustomizableAlreadyExists, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private PNG_SettingsTabControl PNGControl => SettingsHost.Content as PNG_SettingsTabControl ?? throw new InvalidOperationException("PNG control not initialized");

        public ComboBox CompressionComboBox => (ComboBox)PNGControl.FindName("CompressionComboBox");
        public ComboBox FilterComboBox => (ComboBox)PNGControl.FindName("FilterComboBox");
    }
}
