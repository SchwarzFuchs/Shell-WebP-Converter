using Microsoft.Win32;
using Shell_WebP_Converter.CLI;
using Shell_WebP_Converter.CustomElements;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Shell_WebP_Converter
{
    /// <summary>
    /// 
    /// </summary>
    public partial class CustomSettingsDialog : Window
    {
        private static readonly Regex onlyDigitsRegex = new Regex(@"^[0-9]+$");
        private static readonly Regex thresholdRegex = new Regex(@"^[0-9.,]+$");
        private Options options { get; set; }

        public CustomSettingsDialog(Options options)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; 
            this.options = options;
            try
            {
                InitializeComponent();


                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(App.regKeyPath))
                {
                    if (key != null)
                    {
                        if (bool.Parse((key.GetValue("LastCustomRadio") ?? false).ToString() ?? "false"))
                        {
                            CustomSettingsRadioButton.IsChecked = true;
                        }
                        else
                        {
                            CompressToThresholdRadioButton.IsChecked = true;
                        }
                        LowerTheResolutionWhenNecessaryCheckbox.IsChecked = bool.Parse((key.GetValue("LastCustomUseDownscaling") ?? true).ToString() ?? "true");
                        QualityTextBox.Text = (key.GetValue("LastCustomQualitty") ?? "80").ToString();
                        CompressionValueComboBox.SelectedIndex = (int)(key.GetValue("LastCustomCompressionValue") ?? 4);
                        CompressionSizeThresholdTextBox.Text = (key.GetValue("LastCustomSizeThreshold") ?? "2").ToString();
                        SizeMeasurmentUnitComboBox.SelectedIndex = (int)(key.GetValue("LastCustomSizeUnit") ?? 1);
                        DeleteOriginalFileCheckbox.IsChecked = bool.Parse((key.GetValue("LastCustomDeleteOriginal") ?? "false").ToString() ?? "false");
                        PostfixTextBox.Text = options.Postfix;

                    }
                    else
                    {
                        CustomSettingsRadioButton.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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

        private void CustomSettingsRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            CustomSettingsPanel.Visibility = Visibility.Visible;
            CompressionThresholdPanel.Visibility = Visibility.Collapsed;
        }

        private void CompressToThresholdRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            CustomSettingsPanel.Visibility = Visibility.Collapsed;
            CompressionThresholdPanel.Visibility = Visibility.Visible;
        }

        private void CompressionValueComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void SizeMeasurmentUnitComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CustomSettingsRadioButton.IsChecked.GetValueOrDefault())
                {
                    options.Quality = int.Parse(QualityTextBox.Text);
                    options.Compression = (byte)CompressionValueComboBox.SelectedIndex;
                }
                else
                {
                    options.Quality = (int)(float.Parse(CompressionSizeThresholdTextBox.Text) * (float)Math.Pow(1024, SizeMeasurmentUnitComboBox.SelectedIndex + 1));
                    if (options.Quality == 0) throw new Exception("Size can't be zero");
                    options.Compression = 255;
                    options.useDownscaling = LowerTheResolutionWhenNecessaryCheckbox.IsChecked.GetValueOrDefault(true);
                }
                options.DeleteOriginal = DeleteOriginalFileCheckbox.IsChecked.GetValueOrDefault(false);
                options.Postfix = PostfixTextBox.Text.Trim();
                if (AdvancedPreset.windowsPathForbiddenSymbolsRegex.IsMatch(options.Postfix))
                {
                    throw new Exception($"{Shell_WebP_Converter.Resources.Resources.Postfix}: {options.Postfix} — {Shell_WebP_Converter.Resources.Resources.ForbiddenWindowsSymbols}");
                }
                try
                {
                    using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(App.regKeyPath))
                    {
                        if (key != null)
                        {
                            key.SetValue("LastCustomRadio", CustomSettingsRadioButton.IsChecked.GetValueOrDefault(false));
                            key.SetValue("LastCustomQualitty", QualityTextBox.Text);
                            key.SetValue("LastCustomCompressionValue", CompressionValueComboBox.SelectedIndex);
                            key.SetValue("LastCustomSizeThreshold", CompressionSizeThresholdTextBox.Text);
                            key.SetValue("LastCustomSizeUnit", SizeMeasurmentUnitComboBox.SelectedIndex);
                            key.SetValue("LastCustomUseDownscaling", LowerTheResolutionWhenNecessaryCheckbox.IsChecked.GetValueOrDefault(true));
                            key.SetValue("LastCustomDeleteOriginal", DeleteOriginalFileCheckbox.IsChecked ?? false);
                        }
                    }
                }
                catch { }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }
}
