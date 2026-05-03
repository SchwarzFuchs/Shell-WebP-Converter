using Shell_WebP_Converter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Shell_WebP_Converter.CustomElements
{
    public class AdvancedPresetsTableJPG : AdvancedPresetsTableBase
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
                    var jpgUi = PresetsGrid.Children[i] as AdvancedPresetJPG;
                    if (jpgUi == null) continue;
                    JPG_Preset jp = new JPG_Preset();
                    jp.PresetMode = (LossyPresetModes)jpgUi.ModeSelectorComboBox.SelectedIndex;
                    if (jp.PresetMode == LossyPresetModes.ToNQuality)
                    {
                        int quality;
                        if (!int.TryParse(jpgUi.QualitySettingTextBox.Text, out quality))
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.Quality} {Shell_WebP_Converter.Resources.Resources.ValueParsingFailed}");
                            quality = 0;
                        }
                        else if (quality < 0 || quality > 100)
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.Quality} {quality}");
                        }
                        jp.Quality = quality;
                    }
                    else if (jp.PresetMode == LossyPresetModes.ToNSize)
                    {
                        int size;
                        float value;
                        if (float.TryParse(jpgUi.CompressionSizeThresholdTextBox.Text, out value))
                        {
                            size = (int)(value * (float)Math.Pow(1024, jpgUi.SizeMeasurmentUnitComboBox.SelectedIndex + 1));
                            if (size < 1)
                            {
                                errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.DesiredFileSize} {size}");
                            }
                        }
                        else
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.DesiredFileSize} {Shell_WebP_Converter.Resources.Resources.ValueParsingFailed}");
                            size = 0;
                        }
                        jp.Quality = size;
                    }
                    else if (jp.PresetMode == LossyPresetModes.ToN_SSIM)
                    {
                        double SSIM;
                        if (!double.TryParse(jpgUi.SSIM_SettingTextBox.Text, out SSIM))
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.Quality} {Shell_WebP_Converter.Resources.Resources.ValueParsingFailed}");
                            SSIM = 0.0;
                        }
                        else if (SSIM < 0.0 || SSIM > 100.0)
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.Quality} {SSIM}");
                        }
                        jp.Quality = (int)(double.Parse(jpgUi.SSIM_SettingTextBox.Text) * 10000);
                    }
                    jp.DeleteOriginal = jpgUi.DeleteOriginalFileCheckBox.IsChecked.GetValueOrDefault(false);
                    jp.Name = jpgUi.PresetNameTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(jp.Name))
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.PresetName}: {jp.Name}");
                    }
                    jp.Postfix = jpgUi.PostfixNameTextBox.Text.Trim();
                    if (AdvancedPresetBase.windowsPathForbiddenSymbolsRegex.IsMatch(jp.Postfix))
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} – {Shell_WebP_Converter.Resources.Resources.InvalidValue} – {Shell_WebP_Converter.Resources.Resources.Postfix}: {jp.Postfix} – {Shell_WebP_Converter.Resources.Resources.ForbiddenWindowsSymbols}");
                    }
                    presets.Add(jp);
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
            var presets = presetsList as List<JPG_Preset> ?? new List<JPG_Preset>();
            try
            {
                PresetsGrid.Children.Clear();
                PresetsGrid.RowDefinitions.Clear();
                int i = 0;
                foreach (var preset in presets)
                {
                    if (preset == null) { i++; continue; }
                    var presetUIElement = new AdvancedPresetJPG();
                    presetUIElement.ModeSelectorComboBox.SelectedIndex = (int)preset.PresetMode;
                    if (preset.PresetMode == LossyPresetModes.ToNQuality)
                    {
                        presetUIElement.QualitySettingTextBox.Text = preset.Quality.ToString();
                    }
                    else if (preset.PresetMode == LossyPresetModes.ToNSize)
                    {
                        float size = preset.Quality;
                        int unitIndex = 0;
                        size /= 1024;
                        while (size >= 1024 && unitIndex < presetUIElement.SizeMeasurmentUnitComboBox.Items.Count - 1)
                        {
                            size /= 1024f;
                            unitIndex++;
                        }
                        presetUIElement.CompressionSizeThresholdTextBox.Text = size.ToString();
                        presetUIElement.SizeMeasurmentUnitComboBox.SelectedIndex = unitIndex;
                    }
                    else if (preset.PresetMode == LossyPresetModes.ToN_SSIM)
                    {
                        presetUIElement.SSIM_SettingTextBox.Text = ((double)preset.Quality / 10000.0).ToString();
                    }
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
            return new AdvancedPresetJPG();
        }

        internal static List<JPG_Preset> demonstrationJPG_PresetSet = new List<JPG_Preset>
        {
            new JPG_Preset {Name = Shell_WebP_Converter.Resources.Resources.MaximumQuality, Postfix = "_99", PresetMode = LossyPresetModes.ToNQuality, Quality = 99, DeleteOriginal = false},
            new JPG_Preset {Name = Shell_WebP_Converter.Resources.Resources.HighQuality, Postfix = "_90", PresetMode = LossyPresetModes.ToNQuality, Quality = 90, DeleteOriginal = false},
            new JPG_Preset {Name = Shell_WebP_Converter.Resources.Resources.MediumQuality, Postfix = "_60", PresetMode = LossyPresetModes.ToNQuality, Quality = 60, DeleteOriginal = false},
            new JPG_Preset {Name = Shell_WebP_Converter.Resources.Resources.LessThan8MiB, Postfix = ".8MB", PresetMode = LossyPresetModes.ToNSize, Quality = 8378122, DeleteOriginal = false},
            new JPG_Preset {Name = Shell_WebP_Converter.Resources.Resources.VisuallyIdenticalQuality, Postfix = "_viq", PresetMode = LossyPresetModes.ToN_SSIM, Quality = 9700, DeleteOriginal = false},
            new JPG_Preset {Name = Shell_WebP_Converter.Resources.Resources.Customizable, Postfix = "", PresetMode = LossyPresetModes.Custom, Quality = 0, DeleteOriginal = false},
        };
    }
}
