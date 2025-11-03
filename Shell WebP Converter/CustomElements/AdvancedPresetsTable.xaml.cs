using CommandLine;
using Shell_WebP_Converter.CLI;
using Shell_WebP_Converter.Models;
using Shell_WebP_Converter.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shell_WebP_Converter.CustomElements
{
    public partial class AdvancedPresetsTable : UserControl
    {
        private AdvancedPreset? draggedPreset;
        private Point dragStartPoint;
        private bool isDragging;
        private Popup? dragPopup;
        private AdvancedPreset? dragPreview;

        public AdvancedPresetsTable()
        {
            InitializeComponent();
            Loaded += AdvancedPresetsTable_Loaded;
        }

        private void AdvancedPresetsTable_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var child in PresetsGrid.Children.OfType<AdvancedPreset>())
            {
                AttachPresetHandlers(child);
            }
        }

        private void SwapPresets(AdvancedPreset preset, int fromRow, int toRow)
        {
            foreach (var child in PresetsGrid.Children.OfType<AdvancedPreset>())
            {
                int childRow = Grid.GetRow(child);
                if (childRow == toRow)
                {
                    Grid.SetRow(child, fromRow);
                    break;
                }
            }
            Grid.SetRow(preset, toRow);
        }

        /*
        private void SwapPresets(AdvancedPreset preset, int fromRow, int toRow)
        {
            AdvancedPreset other = PresetsGrid.Children
                .OfType<AdvancedPreset>()
                .FirstOrDefault(c => Grid.GetRow(c) == toRow && c != preset);
            if (other != null)
            {
                Grid.SetRow(other, fromRow);
            }
            Grid.SetRow(preset, toRow);
            ReorderPresetsChildrenByRow();
        }
        */
        private void ReorderPresetsChildrenByRow()
        {
            var items = PresetsGrid.Children.Cast<UIElement>()
                .Select((el, idx) => new
                {
                    Element = el,
                    Row = Grid.GetRow(el),
                    OriginalIndex = idx
                })
                .OrderBy(x => x.Row)
                .ThenBy(x => x.OriginalIndex)
                .Select(x => x.Element)
                .ToList();
            PresetsGrid.Children.Clear();
            foreach (var el in items)
                PresetsGrid.Children.Add(el);
        }

        private void Preset_DeleteClicked(object sender, EventArgs e)
        {
            var preset = sender as AdvancedPreset;
            if (preset == null || !PresetsGrid.Children.Contains(preset)) return;

            int row = Grid.GetRow(preset);
            PresetsGrid.Children.Remove(preset);

            if (row < PresetsGrid.RowDefinitions.Count)
            {
                PresetsGrid.RowDefinitions.RemoveAt(row);
            }

            foreach (UIElement child in PresetsGrid.Children)
            {
                int childRow = Grid.GetRow(child);
                if (childRow > row)
                {
                    Grid.SetRow(child, childRow - 1);
                }
            }
        }

        private void AttachPresetHandlers(AdvancedPreset preset)
        {
            preset.DeleteClicked += Preset_DeleteClicked;
            if (preset.PresetNameTextBox != null)
                preset.PresetNameTextBox.KeyDown += PresetNameTextBox_KeyDown;
            if (preset.MoveButton != null)
            {
                preset.MoveButton.PreviewMouseDown += Preset_PreviewMouseDown;
                preset.MoveButton.PreviewMouseMove += Preset_PreviewMouseMove;
                preset.MoveButton.PreviewMouseUp += Preset_PreviewMouseUp;
            }
        }

        private AdvancedPreset? FindParentPreset(DependencyObject? obj)
        {
            while (obj != null)
            {
                if (obj is AdvancedPreset ap)
                    return ap;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private void Preset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            var preset = button?.DataContext as AdvancedPreset ?? FindParentPreset(button);
            if (preset != null)
            {
                draggedPreset = preset;
                dragStartPoint = e.GetPosition(this);
                isDragging = true;
                Mouse.Capture(button);
                draggedPreset.Opacity = 0.25;
                ShowDragPreview(draggedPreset, dragStartPoint);
                e.Handled = true;
            }
        }

        private void ShowDragPreview(AdvancedPreset preset, Point position)
        {
            if (dragPopup != null)
            {
                dragPopup.IsOpen = false;
                dragPopup = null;
            }
            dragPreview = new AdvancedPreset();
            dragPreview.Width = preset.ActualWidth;
            dragPreview.Height = preset.ActualHeight;
            dragPreview.Opacity = 0.7;
            dragPreview.IsHitTestVisible = false;
            dragPreview.PresetNameTextBox.Text = preset.PresetNameTextBox.Text;
            dragPopup = new Popup
            {
                Child = dragPreview,
                Placement = PlacementMode.Relative,
                PlacementTarget = this,
                AllowsTransparency = true,
                IsOpen = true
            };
            UpdateDragPreviewPosition(position);
        }

        private void UpdateDragPreviewPosition(Point position)
        {
            if (dragPopup != null && draggedPreset != null && dragPreview != null)
            {
                var moveBtn = draggedPreset.MoveButton;
                var moveBtnPos = moveBtn.TranslatePoint(new Point(moveBtn.Width / 2, moveBtn.Height / 2), draggedPreset);
                var previewBtn = dragPreview.MoveButton;
                var previewBtnPos = previewBtn.TranslatePoint(new Point(previewBtn.Width / 2, previewBtn.Height / 2), dragPreview);
                var offsetX = moveBtnPos.X - previewBtnPos.X;
                var offsetY = moveBtnPos.Y - previewBtnPos.Y;
                dragPopup.HorizontalOffset = position.X - previewBtnPos.X + offsetX;
                dragPopup.VerticalOffset = position.Y - previewBtnPos.Y + offsetY;
            }
        }

        private void HideDragPreview()
        {
            if (dragPopup != null)
            {
                dragPopup.IsOpen = false;
                dragPopup = null;
                dragPreview = null;
            }
            if (draggedPreset != null)
            {
                draggedPreset.Opacity = 1.0;
            }
        }

        private void Preset_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggedPreset != null)
            {
                Point currentPosition = e.GetPosition(this);
                UpdateDragPreviewPosition(currentPosition);
                var targetPreset = GetPresetAtPosition(currentPosition);
                if (targetPreset != null && targetPreset != draggedPreset)
                {
                    int fromRow = Grid.GetRow(draggedPreset);
                    int toRow = Grid.GetRow(targetPreset);
                    SwapPresets(draggedPreset, fromRow, toRow);
                    dragStartPoint = currentPosition;
                }
                e.Handled = true;
            }
        }

        private void Preset_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                Mouse.Capture(null);
                HideDragPreview();
                e.Handled = true;
            }
        }

        private AdvancedPreset GetPresetAtPosition(Point position)
        {
            for (double xOffset = -20; xOffset <= 20; xOffset += 10)
            {
                for (double yOffset = -10; yOffset <= 10; yOffset += 10)
                {
                    var testPoint = new Point(position.X + xOffset, position.Y + yOffset);
                    DependencyObject element = this.InputHitTest(testPoint) as DependencyObject;
                    while (element != null)
                    {
                        if (element is AdvancedPreset preset)
                            return preset;
                        element = VisualTreeHelper.GetParent(element);
                    }
                }
            }
            return null;
        }

        private void PresetNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var preset = (sender as TextBox)?.DataContext as AdvancedPreset;
            if (preset == null) return;
        }

        private void AddOneEmptyPreset_Click(object sender, RoutedEventArgs e)
        {
            int lastRow = PresetsGrid.Children.Count;
            var newRow = new RowDefinition { Height = GridLength.Auto };
            PresetsGrid.RowDefinitions.Insert(lastRow, newRow);
            var preset = new AdvancedPreset();
            Grid.SetRow(preset, lastRow);
            Grid.SetColumnSpan(preset, 6);
            PresetsGrid.Children.Add(preset);
            AttachPresetHandlers(preset);
            PresetsScrollViewer.ScrollToEnd();
        }

        internal List<Preset> ParsePresets()
        {
            try
            {
                ReorderPresetsChildrenByRow();
                StringBuilder errors = new StringBuilder();
                var presets = new List<Preset>();
                for (int i = 0; i < PresetsGrid.Children.Count; i++)
                {
                    AdvancedPreset presetUIElement = PresetsGrid.Children[i].Cast<AdvancedPreset>();
                    Preset newPreset = new Preset();
                    newPreset.PresetMode = (PresetMode)presetUIElement.ModSelectorComboBox.SelectedIndex;
                    if (newPreset.PresetMode == PresetMode.ToNQuality)
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
                    else if (newPreset.PresetMode == PresetMode.ToNSize)
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
                    if (AdvancedPreset.windowsPathForbiddenSymbolsRegex.IsMatch(newPreset.Postfix))
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
                return presets;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        internal void FillPresetsGridFromList(List<Preset> presets)
        {
            if (presets != null && presets.Count > 0)
            {
                try
                {
                    PresetsGrid.Children.Clear();
                    PresetsGrid.RowDefinitions.Clear();
                    int i = 0;
                    foreach (var preset in presets)
                    {
                        var presetUIElement = new AdvancedPreset();

                        presetUIElement.ModSelectorComboBox.SelectedIndex = (int)preset.PresetMode;

                        if (preset.PresetMode == PresetMode.ToNQuality)
                        {
                            presetUIElement.QualitySettingTextBox.Text = preset.Quality.ToString();
                            presetUIElement.CompressionComboBox.SelectedIndex = (int)preset.Compression;
                        }
                        else if (preset.PresetMode == PresetMode.ToNSize)
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
                        presetUIElement.DeleteOriginalFileCheckBox.IsChecked = preset.DeleteOriginal;
                        presetUIElement.PresetNameTextBox.Text = preset.Name ?? string.Empty;
                        presetUIElement.PostfixNameTextBox.Text = preset.Postfix ?? string.Empty;
                        Grid.SetColumnSpan(presetUIElement, 6);
                        Grid.SetRow(presetUIElement, i);
                        PresetsGrid.RowDefinitions.Add(new RowDefinition());
                        PresetsGrid.Children.Add(presetUIElement);
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }         
        }

    }
}
