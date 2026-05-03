using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shell_WebP_Converter.CustomElements.PresetSettingTabControls
{
    /// <summary>
    /// Interaction logic for WebP_SettingsTabControl.xaml
    /// </summary>
    public partial class WebP_SettingsTabControl : UserControl
    {
        private static readonly Regex onlyDigitsRegex = new Regex(@"^[0-9]+$");
        private static readonly Regex thresholdRegex = new Regex(@"^[0-9.,]+$");

        public WebP_SettingsTabControl()
        {
            InitializeComponent();
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
    }
}
