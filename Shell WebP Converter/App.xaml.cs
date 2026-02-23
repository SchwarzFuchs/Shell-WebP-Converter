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
                bool waitCurrentTaskMessageSemaphoreAcquired = false;
                bool errorMessageBoxSemaphoreAcquired = false;
                bool masterSemaphoreAcquired = false;
                bool customSettingsSemaphoreAcquired = false;
                bool folderSemaphoreAcquired = false;
                Semaphore masterSemaphore = new Semaphore(initialCount: Environment.ProcessorCount, maximumCount: Environment.ProcessorCount, name: @"Local\WebPConverterMasterSemaphore");
                Semaphore folderSemaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: @"Local\WebPConverterFolderSemaphore");
                Semaphore customSettingsSemaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: @"Local\WebPConverterCustomSettingsSemaphore");
                Semaphore errorMessageBoxSemaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: @"Local\WebPConverterErrorMessageBoxSemaphore");
                Semaphore waitCurrentTaskMessageSemaphore = new Semaphore(initialCount: 1, maximumCount: 1, name: @"Local\WebPConverterWaitCurrentTaskMessageSemaphore");
                try
                {
                    if (!masterSemaphore.WaitOne(8 * 60 * 60 * 1000))
                    {
                        throw new Exception("Timeout");
                    }
                    masterSemaphoreAcquired = true;
                    if (debug)
                    {
                        AllocConsole();
                    }
                    else
                    {
                        AttachConsole(ATTACH_PARENT_PROCESS);
                    }
                    if (debug)
                    {
                        foreach (var arg in e.Args) { Console.WriteLine(arg); }
                    }
                    ParserResult<ConverterCommon.ConversionDirectionSetting> directionParserResult = new Parser(p => { p.IgnoreUnknownArguments = true; }).ParseArguments<ConverterCommon.ConversionDirectionSetting>(e.Args);
                    
                    bool parsingFailed = false;
                    directionParserResult.WithNotParsed(er => {
                        string aviableDirections = string.Join(", ", Enum.GetNames(typeof(ConverterCommon.ConversionDirection)));
                        Console.WriteLine("Specify the conversion --direction first to see detailed --help. Aviable directions: " + aviableDirections);
                        DisplayErrors(er);
                        parsingFailed = true;
                    });

                    if (!parsingFailed)
                    {
                        directionParserResult.WithParsed(opts => {
                            ConverterCommon.ConversionDirection conversionDirection = opts.ConversionDirection;
                            if (conversionDirection == ConverterCommon.ConversionDirection.AnyToWebP)
                            {
                                ProcessWebPConversion(e.Args, customSettingsSemaphore, waitCurrentTaskMessageSemaphore, 
                                    folderSemaphore, ref customSettingsSemaphoreAcquired, 
                                    ref waitCurrentTaskMessageSemaphoreAcquired, ref folderSemaphoreAcquired);
                            }
                            else if (conversionDirection == ConverterCommon.ConversionDirection.AnyToJPG)
                            {
                                ProcessJPGConversion(e.Args);
                            }
                            else if (conversionDirection == ConverterCommon.ConversionDirection.AnyToPNG)
                            {
                                ProcessPNGConversion(e.Args);
                            }
                        });
                    }
                    if (debug)
                    {
                        Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in e.Args) { sb.Append(item.ToString()); sb.Append(" "); }
                    sb.Append(" | ");
                    sb.AppendLine(ex.ToString());
                    Log(sb.ToString());
                    if (errorMessageBoxSemaphore.WaitOne(0))
                    {
                        errorMessageBoxSemaphoreAcquired = true;
                        MessageBox.Show($"{Shell_WebP_Converter.Resources.Resources.ConversionFail}");
                    }
                }
                finally
                {
                    if (folderSemaphoreAcquired)
                    {
                        folderSemaphore.Release();
                    }
                    if (customSettingsSemaphoreAcquired)
                    {
                        customSettingsSemaphore.Release();
                    }
                    if (waitCurrentTaskMessageSemaphoreAcquired)
                    {
                        waitCurrentTaskMessageSemaphore.Release();
                    }
                    if (errorMessageBoxSemaphoreAcquired)
                    {
                        errorMessageBoxSemaphore.Release();
                    }
                    if (masterSemaphoreAcquired)
                    {
                        masterSemaphore.Release();
                    }
                    if (debug)
                    {
                        Console.ReadLine();
                    }
                    Environment.Exit(0);
                }
            }
            else
            {
                base.OnStartup(e);
            }
        }

        private void ProcessWebPConversion(string[] args, Semaphore customSettingsSemaphore, 
            Semaphore waitCurrentTaskMessageSemaphore, Semaphore folderSemaphore,
            ref bool customSettingsSemaphoreAcquired, ref bool waitCurrentTaskMessageSemaphoreAcquired,
            ref bool folderSemaphoreAcquired)
        {
            if (args.Contains("--help"))
            {
                DisplayHelpForOptions<WebPConversionOptions>(args);
                return;
            }
            
            var parserResult = ParseOptions<WebPConversionOptions>(args);
            
            if (CheckParsingFailed(parserResult))
            {
                return;
            }

            string target = "";
            bool custom = false;
            parserResult.WithParsed(opts => { 
                target = opts.Input; 
                custom = opts.Mode == Models.PresetMode.Custom; 
            });

            if (custom)
            {
                if (!customSettingsSemaphore.WaitOne(0))
                {
                    if (!waitCurrentTaskMessageSemaphore.WaitOne(0))
                    {
                        return;
                    }
                    waitCurrentTaskMessageSemaphoreAcquired = true;
                    MessageBox.Show(Shell_WebP_Converter.Resources.Resources.NextTaskStartMustWaitCurrent);
                    return;
                }
                customSettingsSemaphoreAcquired = true;
                
                bool customDialogCancelled = false;
                parserResult.WithParsed(opts =>
                {
                    CustomSettingsDialog customSettingsDialog = new CustomSettingsDialog(opts);
                    if (!customSettingsDialog.ShowDialog() ?? false)
                    {
                        customDialogCancelled = true;
                    }
                });

                if (customDialogCancelled)
                {
                    return;
                }
            }

            if (Directory.Exists(target))
            {
                if (!folderSemaphore.WaitOne(8 * 60 * 60 * 1000))
                {
                    throw new Exception("Timeout");
                }
                folderSemaphoreAcquired = true;
            }

            parserResult.WithParsed(opts => { (new CLI_ModeWebPConverter(opts)).Run(); });
        }

        private void ProcessJPGConversion(string[] args)
        {
            if (args.Contains("--help"))
            {
                DisplayHelpForOptions<JPGConversionOptions>(args);
                return;
            }
            
            var parserResult = ParseOptions<JPGConversionOptions>(args);
            
            if (!CheckParsingFailed(parserResult))
            {
                parserResult.WithParsed(opts => { (new CLI_ModeJPGConverter(opts)).Run(); });
            }
        }

        private void ProcessPNGConversion(string[] args)
        {
            if (args.Contains("--help"))
            {
                DisplayHelpForOptions<PNGConversionOptions>(args);
                return;
            }
            
            var parserResult = ParseOptions<PNGConversionOptions>(args);
            
            if (!CheckParsingFailed(parserResult))
            {
                parserResult.WithParsed(opts => { (new CLI_ModePNGConverter(opts)).Run(); });
            }
        }

        private void DisplayHelpForOptions<T>(string[] args) where T : class
        {
            var helpParser = new Parser(p => 
            { 
                p.IgnoreUnknownArguments = false;
                p.HelpWriter = Console.Out;
            });
            helpParser.ParseArguments<T>(args);
        }

        private ParserResult<T> ParseOptions<T>(string[] args) where T : class
        {
            return new Parser(p => { 
                p.IgnoreUnknownArguments = true;
                p.HelpWriter = Console.Out;
            }).ParseArguments<T>(args);
        }

        private bool CheckParsingFailed<T>(ParserResult<T> parserResult) where T : class
        {
            bool failed = false;
            parserResult.WithNotParsed(er => { 
                DisplayErrors(er);
                failed = true;
            });
            return failed;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;


        public static void Log(string message)
        {
            message = DateTime.Now.ToString() + (" | ") + message + (" | ");
            string fileName = Path.Combine( Path.GetDirectoryName(Environment.ProcessPath) ?? "", $"ExceptionLog {DateTime.Now.Date.ToString("yyyy-MM-dd")}.txt");
            File.AppendAllText(fileName, message);
            DeleteOldLogFiles(fileName);
        }
        static void DisplayErrors(object er)
        {
            if (er is IEnumerable<CommandLine.Error> errorEnumerable)
            {
                var errorList = errorEnumerable.ToList();
                
                if (errorList.Any(e => e.Tag == CommandLine.ErrorType.HelpRequestedError || e.Tag == CommandLine.ErrorType.VersionRequestedError))
                {
                    return;
                }
                
                foreach (var error in errorList)
                {
                    Console.WriteLine(error.ToString());
                }
            }
            Console.WriteLine(Shell_WebP_Converter.Resources.Resources.WrongArgumentsHelp);
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

