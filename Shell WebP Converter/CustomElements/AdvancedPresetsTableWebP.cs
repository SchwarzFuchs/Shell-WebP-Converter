using Shell_WebP_Converter.Models;
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Shell_WebP_Converter.CustomElements
{
    public class AdvancedPresetsTableWebP : AdvancedPresetsTableBase
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
                    var presetUIElement = PresetsGrid.Children[i] as AdvancedPresetWebP;
                    if (presetUIElement == null) continue;
                    WebP_Preset newPreset = new WebP_Preset();
                    newPreset.PresetMode = (LossyPresetModes)presetUIElement.ModeSelectorComboBox.SelectedIndex;
                    if (newPreset.PresetMode == LossyPresetModes.ToNQuality)
                    {
                        int quality;
                        if (!int.TryParse(presetUIElement.QualitySettingTextBox.Text, out quality))
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.Quality} {Shell_WebP_Converter.Resources.Resources.ValueParsingFailed}");
                            quality = 0;
                        }
                        else if (quality < 0 || quality > 100)
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.Quality} {quality}");
                        }
                        newPreset.Quality = quality;
                        newPreset.Compression = (byte)presetUIElement.CompressionComboBox.SelectedIndex;
                    }
                    else if (newPreset.PresetMode == LossyPresetModes.ToNSize)
                    {
                        int size;
                        float value;
                        if (float.TryParse(presetUIElement.CompressionSizeThresholdTextBox.Text, out value))
                        {
                            size = (int)(value * (float)Math.Pow(1024, presetUIElement.SizeMeasurmentUnitComboBox.SelectedIndex + 1));
                            if (size < 1)
                            {
                                errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.DesiredFileSize} {size}");
                            }
                        }
                        else
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.DesiredFileSize} {Shell_WebP_Converter.Resources.Resources.ValueParsingFailed}");
                            size = 0;
                        }
                        newPreset.Quality = size;
                        newPreset.Compression = 255;
                    }
                    else if (newPreset.PresetMode == LossyPresetModes.ToN_SSIM)
                    {
                        double SSIM;
                        if (!double.TryParse(presetUIElement.SSIM_SettingTextBox.Text, out SSIM))
                        {
                            errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.Quality} {Shell_WebP_Converter.Resources.Resources.ValueParsingFailed}");
                            SSIM = 0.0;
                        }
                        else if (SSIM < 0.0 || SSIM > 100.0)
                        {
                                errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.Quality} {SSIM}");
                        }
                        newPreset.Quality = (int)(double.Parse(presetUIElement.SSIM_SettingTextBox.Text) * 10000);
                        newPreset.Compression = (byte)presetUIElement.SSIM_CompressionComboBox.SelectedIndex;
                    }
                    else
                    {
                        newPreset.Quality = 0;
                        newPreset.Compression = 0;
                    }
                    newPreset.DeleteOriginal = presetUIElement.DeleteOriginalFileCheckBox.IsChecked.GetValueOrDefault(false);
                    newPreset.Name = presetUIElement.PresetNameTextBox.Text.Trim();
                    if (string.IsNullOrWhiteSpace(newPreset.Name))
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.PresetName}: {newPreset.Name}");
                    }
                    newPreset.Postfix = presetUIElement.PostfixNameTextBox.Text.Trim();
                    if (AdvancedPresetBase.windowsPathForbiddenSymbolsRegex.IsMatch(newPreset.Postfix))
                    {
                        errors.AppendLine($"{Shell_WebP_Converter.Resources.Resources.Preset} #{i + 1} — {Shell_WebP_Converter.Resources.Resources.InvalidValue} — {Shell_WebP_Converter.Resources.Resources.Postfix}: {newPreset.Postfix} — {Shell_WebP_Converter.Resources.Resources.ForbiddenWindowsSymbols}");
                    }
                    presets.Add(newPreset);
                }
                if (errors.Length > 0)
                {
                    MessageBox.Show(errors.ToString(), Shell_WebP_Converter.Resources.Resources.InvalidValue, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
                return presets.Cast<PresetBase>().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public override void FillPresetsGridFromList(IEnumerable<PresetBase> presetsList)
        {
            var presets = presetsList as List<WebP_Preset> ?? new List<WebP_Preset>();
            try
            {
                PresetsGrid.Children.Clear();
                PresetsGrid.RowDefinitions.Clear();
                int i = 0;
                foreach (var preset in presets)
                {
                    if (preset == null) { i++; continue; }
                    var presetUIElement = new AdvancedPresetWebP();
                    presetUIElement.ModeSelectorComboBox.SelectedIndex = (int)preset.PresetMode;

                    if (preset.PresetMode == LossyPresetModes.ToNQuality)
                    {
                        presetUIElement.QualitySettingTextBox.Text = preset.Quality.ToString();
                        presetUIElement.CompressionComboBox.SelectedIndex = (int)preset.Compression;
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
                        presetUIElement.SSIM_CompressionComboBox.SelectedIndex = (int)preset.Compression;
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
            return new AdvancedPresetWebP();
        }

        internal static List<WebP_Preset> demonstrationWebP_PresetSet = new List<WebP_Preset>
        {
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.LosslessCapital, Postfix = "_lossless", PresetMode = LossyPresetModes.ToNQuality, Quality = 100, Compression = 6, DeleteOriginal = false},
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.MinLossHighCompression, Postfix = "_99_6", PresetMode = LossyPresetModes.ToNQuality, Quality = 99, Compression = 6, DeleteOriginal = false},
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.MinLossMediumCompression, Postfix = "_99_4", PresetMode = LossyPresetModes.ToNQuality, Quality = 99, Compression = 4, DeleteOriginal = false},
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.HighQuality, Postfix = "_90", PresetMode = LossyPresetModes.ToNQuality, Quality = 90, Compression = 4, DeleteOriginal = false},
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.MediumQuality, Postfix = "_60", PresetMode = LossyPresetModes.ToNQuality, Quality = 60, Compression = 4, DeleteOriginal = false},
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.LessThan8MiB, Postfix = ".8MB", PresetMode = LossyPresetModes.ToNSize, Quality = 8378122, Compression = 255, DeleteOriginal = false},
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.VisuallyIdenticalQuality, Postfix = "_viq", PresetMode = LossyPresetModes.ToN_SSIM, Quality = 9700, Compression = 6, DeleteOriginal = false},
            new WebP_Preset {Name = Shell_WebP_Converter.Resources.Resources.Customizable, Postfix = "", PresetMode = LossyPresetModes.Custom, Quality = 0, Compression = 0, DeleteOriginal = false},
        };
    }
}
