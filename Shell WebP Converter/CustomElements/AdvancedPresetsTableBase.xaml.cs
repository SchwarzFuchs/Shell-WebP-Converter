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
    public abstract partial class AdvancedPresetsTableBase : UserControl
    {

        private AdvancedPresetBase? draggedPreset;
        private Point dragStartPoint;
        private bool isDragging;
        private Popup? dragPopup;
        private AdvancedPresetBase? dragPreview;

        public AdvancedPresetsTableBase()
        {
            InitializeComponent();
            Loaded += AdvancedPresetsTable_Loaded;
        }

        private void AdvancedPresetsTable_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var child in PresetsGrid.Children.OfType<AdvancedPresetBase>())
            {
                AttachPresetHandlers(child);
            }
            if (PresetsGrid.Children.Count == 0)
            {
                AddEmptyPresetOfFormat();
            }
        }

        private void SwapPresets(AdvancedPresetBase preset, int fromRow, int toRow)
        {
            foreach (var child in PresetsGrid.Children.OfType<AdvancedPresetBase>())
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

        public void ReorderPresetsChildrenByRow()
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
            var preset = sender as AdvancedPresetBase;
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

        public void AttachPresetHandlers(AdvancedPresetBase preset)
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

        private AdvancedPresetBase? FindParentPreset(DependencyObject? obj)
        {
            while (obj != null)
            {
                if (obj is AdvancedPresetBase ap)
                    return ap;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        private void Preset_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var button = sender as Button;
            var preset = button?.DataContext as AdvancedPresetBase ?? FindParentPreset(button);
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

        private void ShowDragPreview(AdvancedPresetBase preset, Point position)
        {
            if (dragPopup != null)
            {
                dragPopup.IsOpen = false;
                dragPopup = null;
            }
            dragPreview = new AdvancedPresetBase();
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

        private AdvancedPresetBase? GetPresetAtPosition(Point position)
        {
            for (double xOffset = -20; xOffset <= 20; xOffset += 10)
            {
                for (double yOffset = -10; yOffset <= 10; yOffset += 10)
                {
                    var testPoint = new Point(position.X + xOffset, position.Y + yOffset);
                    DependencyObject element = this.InputHitTest(testPoint) as DependencyObject;
                    while (element != null)
                    {
                        if (element is AdvancedPresetBase preset)
                            return preset;
                        element = VisualTreeHelper.GetParent(element);
                    }
                }
            }
            return null;
        }

        private void PresetNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var preset = (sender as TextBox)?.DataContext as AdvancedPresetBase;
            if (preset == null) return;
        }

        private void AddOneEmptyPreset_Click(object sender, RoutedEventArgs e)
        {
            int lastRow = PresetsGrid.Children.Count;
            var newRow = new RowDefinition { Height = GridLength.Auto };
            PresetsGrid.RowDefinitions.Insert(lastRow, newRow);
            var preset = CreatePresetUIElement();
            Grid.SetRow(preset, lastRow);
            Grid.SetColumnSpan(preset, 6);
            PresetsGrid.Children.Add(preset);
            AttachPresetHandlers(preset);
            PresetsScrollViewer.ScrollToEnd();
        }

        private void AddEmptyPresetOfFormat()
        {
            int lastRow = PresetsGrid.Children.Count;
            var newRow = new RowDefinition { Height = GridLength.Auto };
            PresetsGrid.RowDefinitions.Insert(lastRow, newRow);
            var preset = CreatePresetUIElement();
            Grid.SetRow(preset, lastRow);
            Grid.SetColumnSpan(preset, 6);
            PresetsGrid.Children.Add(preset);
            AttachPresetHandlers(preset);
        }
        public abstract List<PresetBase>? ParsePresets();

        public abstract void FillPresetsGridFromList(IEnumerable<PresetBase> presets);

        protected abstract AdvancedPresetBase CreatePresetUIElement();

    }
}
