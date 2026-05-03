using Shell_WebP_Converter.ViewModels;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shell_WebP_Converter
{
    public partial class CustomSettingsDialogJPG : Window
    {
        private CustomSettingsDialogViewModelJPG ViewModel { get; set; }
        private static readonly Regex OnlyDigitsRegex = new Regex(@"^[0-9]+$");
        private static readonly Regex ThresholdRegex = new Regex(@"^[0-9.,]+$");

        public CustomSettingsDialogJPG(JPGConversionOptions options)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ViewModel = new CustomSettingsDialogViewModelJPG(options);
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void QualityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            string newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength).Insert(tb.SelectionStart, e.Text);
            if (!OnlyDigitsRegex.IsMatch(e.Text) || newText.Length > 3)
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
            if (!ThresholdRegex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }
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

        private void CustomSettingsRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                CustomSettingsPanel.Visibility = Visibility.Visible;
                CompressionThresholdPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void CompressToThresholdRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                CustomSettingsPanel.Visibility = Visibility.Collapsed;
                CompressionThresholdPanel.Visibility = Visibility.Visible;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OKCommand.Execute(null);
            if (!ViewModel.HasErrors)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
