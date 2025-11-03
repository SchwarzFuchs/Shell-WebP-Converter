using CommandLine;
using Shell_WebP_Converter.CLI;
using Shell_WebP_Converter.Resources;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;




namespace Shell_WebP_Converter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string regKeyPath = @"Software\ShellWebPConverter";
        protected override void OnStartup(StartupEventArgs e)
        {
            bool debug = false;
            if (e.Args.Contains("--debug"))
            {
                debug = true;
            }
            if (e.Args.Length > 0)
            {
                bool inputIsFolder = false;
                bool custom = false;
                Semaphore masterSemaphore = new Semaphore(initialCount: Environment.ProcessorCount, maximumCount: Environment.ProcessorCount, name: @"Local\WebPConverterMasterSemaphore");
                Semaphore folderSemaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: @"Local\WebPConverterFolderSemaphore");
                Semaphore customSettingsSemaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: @"Local\WebPConverterCustomSettingsSemaphore");
                Semaphore errorMessageBoxSemaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: @"Local\WebPConverterErrorMessageBoxSemaphore");
                try
                {
                    if (!masterSemaphore.WaitOne(8 * 60 * 60 * 1000))
                    {
                        throw new Exception("Timeout");
                    }
                    if (debug)
                    {
                        AllocConsole();
                    }
                    else
                    {
                        AttachConsole(ATTACH_PARENT_PROCESS);
                    }
                    foreach (var arg in e.Args) { Console.WriteLine(arg); }
                    ParserResult<Options> parserResult = ParseArgs(e.Args);
                    parserResult.WithNotParsed(er => { DisplayErrors(er); Environment.Exit(0); });
                    string target = "";
                    parserResult.WithParsed(opts => { target = opts.Input; custom = opts.Custom; });
                    if (custom)
                    {
                        if (!customSettingsSemaphore.WaitOne(0))
                        {
                            MessageBox.Show(Shell_WebP_Converter.Resources.Resources.NextTaskStartMustWaitCurrent);
                            masterSemaphore.Release();
                            Environment.Exit(0);
                        }
                        parserResult.WithParsed(opts =>
                        {

                            CustomSettingsDialog customSettingsDialog = new CustomSettingsDialog(opts);
                            if (!customSettingsDialog.ShowDialog() ?? false)
                            {
                                masterSemaphore.Release();
                                Environment.Exit(0);
                            }
                        });
                    }
                    if (Directory.Exists(target))
                    {
                        if (!folderSemaphore.WaitOne(8 * 60 * 60 * 1000))
                        {
                            throw new Exception("Timeout");
                        }
                        inputIsFolder = true;
                    }
                    parserResult.WithParsed(opts => { (new CLI_Mode(opts)).Run(); });

                    if (debug)
                    {
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
                catch (Exception ex)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in e.Args) { sb.Append(item.ToString()); sb.Append(" "); }
                    sb.Append(" | ");
                    sb.AppendLine(ex.ToString());
                    Log(sb.ToString());
                    if (!errorMessageBoxSemaphore.WaitOne(0))
                    {
                        Environment.Exit(0);
                    }
                    else
                    {
                        MessageBox.Show($"{Shell_WebP_Converter.Resources.Resources.ConversionFail}");
                        errorMessageBoxSemaphore.Release();
                    }
                }
                finally
                {
                    if (inputIsFolder)
                    {
                        folderSemaphore.Release();
                    }
                    if (custom)
                    {
                        customSettingsSemaphore.Release();
                    }
                    masterSemaphore.Release();
                    if (debug)
                    {
                        Console.ReadLine();
                        Environment.Exit(0);
                    }
                }
            }
            else
            {
                base.OnStartup(e);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        ParserResult<Options> ParseArgs(string[] args)
        {
            return (Parser.Default.ParseArguments<Options>(args));
        }

        public static void Log(string message)
        {
            message = DateTime.Now.ToString() + (" | ") + message;
            string fileName = Path.Combine( Path.GetDirectoryName(Environment.ProcessPath) ?? "", $"ExceptionLog {DateTime.Now.Date.ToString("yyyy-MM-dd")}.txt");
            File.AppendAllText(fileName, message);
            DeleteOldLogFiles(fileName);
        }
        static void DisplayErrors(object er)
        {
            if (er.GetType() == typeof(CommandLine.Error[]))
            {
                if (((CommandLine.Error[])er)[0].Tag == CommandLine.ErrorType.HelpRequestedError || ((CommandLine.Error[])er)[0].Tag == CommandLine.ErrorType.VersionRequestedError)
                {
                    return;
                }
            }
            else
            {
                if (er is IEnumerable<CommandLine.Error> errorEnumerable)
                {
                    var errorList = errorEnumerable.ToList();
                    foreach (var error in errorList)
                    {
                        Console.WriteLine(error.ToString());
                    }
                }
                Console.WriteLine(Shell_WebP_Converter.Resources.Resources.WrongArgumentsHelp);
            }
        }

        public static void DeleteOldLogFiles(string referenceFilePath)
        {
            try
            {
                string? directory = Path.GetDirectoryName(referenceFilePath);
                if (!Directory.Exists(directory))
                {
                    return;
                }
                string[] files = Directory.GetFiles(directory, "ExceptionLog*.txt");
                DateTime now = DateTime.Now;
                DateTime threshold = now.AddDays(-7);
                foreach (string file in files)
                {
                    DateTime creationTime = File.GetCreationTime(file);
                    if (creationTime < threshold)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {

            }
        }
    }
}

