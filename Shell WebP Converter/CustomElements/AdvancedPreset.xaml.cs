using Shell_WebP_Converter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Threading;

namespace Shell_WebP_Converter.CustomElements
{
    public partial class AdvancedPreset : UserControl
    {
        private static readonly Regex onlyDigitsRegex = new Regex(@"^[0-9]+$");
        private static readonly Regex thresholdRegex = new Regex(@"^[0-9.,]+$");
        internal static readonly Regex windowsPathForbiddenSymbolsRegex = new Regex(@"[\\\/:*?""<>|]");
        
        private record UniquePresetModeConfig(PresetMode Mode, string MessageResourceGetter);
        
        private static readonly List<UniquePresetModeConfig> UniquePresetModes = new()
        {
            new UniquePresetModeConfig(PresetMode.Custom, Shell_WebP_Converter.Resources.Resources.CustomizableAlreadyExists)
        };
        
        private int _previousSelectedMode;
        private bool _isChangingMode = false;

        public AdvancedPreset()
        {
            InitializeComponent();
            DeleteButton.Click += (s, e) => DeleteClicked?.Invoke(this, EventArgs.Empty);
            
            this.Loaded += (s, e) =>
            {
                UpdateTextBoxMaxWidths();
            };
            
            this.SizeChanged += (s, e) =>
            {
                UpdateTextBoxMaxWidths();
            };
        }

        private void UpdateTextBoxMaxWidths()
        {
            var parent = this.Parent as Grid;
            if (parent == null) return;

            if (parent.ColumnDefinitions.Count > 3 && parent.ColumnDefinitions.Count > 4)
            {
                double col3Width = parent.ColumnDefinitions[3].ActualWidth;
                double col4Width = parent.ColumnDefinitions[4].ActualWidth;

                if (col3Width > 0)
                    PresetNameTextBox.MaxWidth = Math.Max(200, col3Width - 10);
                
                if (col4Width > 0)
                    PostfixNameTextBox.MaxWidth = Math.Max(60, col4Width - 8);
            }
        }

        public event EventHandler? DeleteClicked;

        private bool IsModeAlreadyUsed(PresetMode mode)
        {
            var parent = this.Parent as Grid;
            if (parent == null) return false;

            var presets = parent.Children.OfType<AdvancedPreset>();
            return presets.Any(p => p != this && p.ModSelectorComboBox.SelectedIndex == (int)mode);
        }


        private void ModSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!((ComboBox)sender).IsInitialized || !SettingsTabControl.IsInitialized || _isChangingMode) return;

            var newIndex = ((ComboBox)sender).SelectedIndex;
            var newMode = (PresetMode)newIndex;
            
            var uniqueModeConfig = UniquePresetModes.FirstOrDefault(config => config.Mode == newMode);
            if (uniqueModeConfig != null && IsModeAlreadyUsed(newMode))
            {
                var message = uniqueModeConfig.MessageResourceGetter;
                MessageBox.Show(message, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                _isChangingMode = true;
                ((ComboBox)sender).SelectedIndex = _previousSelectedMode;
                SettingsTabControl.SelectedIndex = _previousSelectedMode;
                _isChangingMode = false;
                e.Handled = true;
                return;
            }
            SettingsTabControl.SelectedIndex = newIndex;
            if (newIndex == 2)
            {
                PresetNameTextBox.IsReadOnly = true;
                PresetNameTextBox.Text = Shell_WebP_Converter.Resources.Resources.Customizable;
            }
            else
            {
                PresetNameTextBox.IsReadOnly = false;
                if (_previousSelectedMode == 2)
                {
                    PresetNameTextBox.Clear();
                }
            }
            _previousSelectedMode = newIndex;
        }

        private void QualityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.Text);
            if (!onlyDigitsRegex.IsMatch(e.Text) || newText.Length > 3)
            {
                e.Handled = true;
                return;
            }
            if (int.TryParse(newText, out int value))
            {
                if (value < 0 || value > 100)
                    e.Handled = true;
            }
            else if (!string.IsNullOrEmpty(newText))
            {
                e.Handled = true;
            }
        }

        private void QualityTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        private void CompressionThresholdTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !thresholdRegex.IsMatch(e.Text);
            string textPreview = ((TextBox)sender).Text + e.Text;
            if ((textPreview.Count(s => s == ',') + textPreview.Count(s => s == '.')) > 1)
            {
                e.Handled = true;
                return;
            }
        }

        private void CompressionThresholdTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }


        private void MoveButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var previewMouseDownEvent = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton)
            {
                RoutedEvent = PreviewMouseDownEvent,
                Source = this
            };
            RaiseEvent(previewMouseDownEvent);
        }

        private void MoveButton_MouseMove(object sender, MouseEventArgs e)
        {
            var previewMouseMoveEvent = new MouseEventArgs(e.MouseDevice, e.Timestamp)
            {
                RoutedEvent = PreviewMouseMoveEvent,
                Source = this
            };
            RaiseEvent(previewMouseMoveEvent);
        }

        private void MoveButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var previewMouseUpEvent = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, e.ChangedButton)
            {
                RoutedEvent = PreviewMouseUpEvent,
                Source = this
            };
            RaiseEvent(previewMouseUpEvent);
        }

        private void QualitySettingTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.Text);
            if (!onlyDigitsRegex.IsMatch(e.Text) || newText.Length > 3)
            {
                e.Handled = true;
                return;
            }
            if (int.TryParse(newText, out int value))
            {
                if (value < 0 || value > 100)
                    e.Handled = true;
            }
            else if (!string.IsNullOrEmpty(newText))
            {
                e.Handled = true;
            }
        }

        private DispatcherTimer popupTimer;
        private void PostfixNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (windowsPathForbiddenSymbolsRegex.IsMatch(e.Text))
            {
                e.Handled = true;
                WarningPopup.Placement = PlacementMode.Top;
                WarningPopup.IsOpen = true;
                if (popupTimer == null)
                {
                    popupTimer = new DispatcherTimer();
                    popupTimer.Interval = TimeSpan.FromSeconds(3);
                    popupTimer.Tick += (s, args) =>
                    {
                        WarningPopup.IsOpen = false;
                        popupTimer.Stop();
                    };
                }
                else
                {
                    popupTimer.Stop(); 
                }

                popupTimer.Start();
            }
        }
    }
}
