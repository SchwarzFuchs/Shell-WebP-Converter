using Shell_WebP_Converter.CLI;
using Shell_WebP_Converter.ViewModels;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace Shell_WebP_Converter
{
    public partial class CustomSettingsDialogPNG : Window
    {
        private CustomSettingsDialogViewModelPNG ViewModel { get; set; }

        public CustomSettingsDialogPNG(PNGConversionOptions options)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ViewModel = new CustomSettingsDialogViewModelPNG(options);
            InitializeComponent();
            DataContext = ViewModel;
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
