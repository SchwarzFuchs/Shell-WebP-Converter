using Shell_WebP_Converter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Shell_WebP_Converter.CustomElements
{
    public class AdvancedPresetsTablePNG : AdvancedPresetsTableBase
    {
        public override List<PresetBase>? ParsePresets()
        {
            try
            {
                ReorderPresetsChildrenByRow();
                StringBuilder errors = new StringBuilder();
                var presets = new List<PresetBase>();
                for (int i = 0; i < PresetsGrid.Children.Count; i++)
                {
                    var pngUi = PresetsGrid.Children[i] as AdvancedPresetPNG;
                    if (pngUi == null) continue;
                    PNG_Preset pp = new PNG_Preset();
                    pp.PresetMode = (LoslessPresetModes)pngUi.ModeSelectorComboBox.SelectedIndex;

                    int compression = pngUi.CompressionComboBox.SelectedIndex;
                    if (compression < 0 || compression > 9)
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.CompressionLevelColon} {compression}");
                    }
                    pp.Compression = (byte)Math.Max(0, Math.Min(9, compression));

                    int filter = pngUi.FilterComboBox.SelectedIndex;
                    if (filter < 0 || filter > 5)
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.Filter}: {filter}");
                    }
                    pp.Filter = (byte)Math.Max(0, Math.Min(5, filter));

                    pp.DeleteOriginal = pngUi.DeleteOriginalFileCheckBox.IsChecked.GetValueOrDefault(false);
                    pp.Name = pngUi.PresetNameTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(pp.Name))
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.PresetName}: {pp.Name}");
                    }
                    pp.Postfix = pngUi.PostfixNameTextBox.Text.Trim();
                    if (AdvancedPresetBase.windowsPathForbiddenSymbolsRegex.IsMatch(pp.Postfix))
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.Postfix}: {pp.Postfix} – {Shell_WebP_Converter.Resources.Resources.ForbiddenWindowsSymbols}");
                    }
                    presets.Add(pp);
                }
                if (errors.Length > 0)
                {
                    MessageBox.Show(errors.ToString(), Shell_WebP_Converter.Resources.Resources.InvalidValue, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
                return presets;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public override void FillPresetsGridFromList(IEnumerable<PresetBase> presetsList)
        {
            var presets = presetsList as List<PNG_Preset> ?? new List<PNG_Preset>();
            try
            {
                PresetsGrid.Children.Clear();
                PresetsGrid.RowDefinitions.Clear();
                int i = 0;
                foreach (var preset in presets)
                {
                    if (preset == null) { i++; continue; }
                    var presetUIElement = new AdvancedPresetPNG();
                    presetUIElement.ModeSelectorComboBox.SelectedIndex = (int)preset.PresetMode;
                    presetUIElement.CompressionComboBox.SelectedIndex = Math.Min((int)preset.Compression, 9);
                    presetUIElement.FilterComboBox.SelectedIndex = Math.Min((int)preset.Filter, 5);
                    presetUIElement.DeleteOriginalFileCheckBox.IsChecked = preset.DeleteOriginal;
                    presetUIElement.PresetNameTextBox.Text = preset.Name ?? string.Empty;
                    presetUIElement.PostfixNameTextBox.Text = preset.Postfix ?? string.Empty;
                    Grid.SetColumnSpan(presetUIElement, 6);
                    Grid.SetRow(presetUIElement, i);
                    PresetsGrid.RowDefinitions.Add(new RowDefinition());
                    PresetsGrid.Children.Add(presetUIElement);
                    AttachPresetHandlers(presetUIElement);
                    i++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override AdvancedPresetBase CreatePresetUIElement()
        {
            return new AdvancedPresetPNG();
        }

        internal static List<PNG_Preset> demonstrationPNG_PresetSet = new List<PNG_Preset>
        {
            new PNG_Preset {Name = Shell_WebP_Converter.Resources.Resources.MaximumCompressionSmallestFileSize, Postfix = "_max_c", PresetMode = LoslessPresetModes.ToNCompression, Compression = 9, Filter = 5, DeleteOriginal = false},
            new PNG_Preset {Name = Shell_WebP_Converter.Resources.Resources.MediumCompressionMediumFileSize, Postfix = "_med_c", PresetMode = LoslessPresetModes.ToNCompression, Compression = 4, Filter = 2, DeleteOriginal = false},
            new PNG_Preset {Name = Shell_WebP_Converter.Resources.Resources.NoCompressionLargeFileSize, Postfix = "_no_c", PresetMode = LoslessPresetModes.ToNCompression, Compression = 0, Filter = 0, DeleteOriginal = false},
            new PNG_Preset {Name = Shell_WebP_Converter.Resources.Resources.Customizable, Postfix = "", PresetMode = LoslessPresetModes.Custom, Compression = 0, Filter = 0, DeleteOriginal = false},
        };

    }
}
