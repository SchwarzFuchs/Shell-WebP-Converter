using CommandLine;
using Hardcodet.Wpf.TaskbarNotification;
using ImageMagick;
using MathNet.Numerics.Interpolation;
using Microsoft.Win32;
using Shell_WebP_Converter.CustomElements;
using System.Drawing;
using System.IO;
using System.Reflection.Emit;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Shell_WebP_Converter.CLI
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Path to the input file")]
        public string Input { get; set; } = "";

        [Option('o', "output", Required = false, HelpText = "Path to the output directory. Default value — directory of the input file")]
        public string Output { get; set; } = "";

        [Option('q', "quality", Required = true, HelpText = "Quality 0-100, 100 = lossless. In custom size threshold mode used for size threshold (in bytes)")]
        public int Quality { get; set; }

        [Option('d', "deleteOriginal", Required = false, HelpText = "Delete original files, True/False")]
        public bool DeleteOriginal { get; set; } = false;

        [Option('c', "compression", Required = true, HelpText = "Compression, 0-6. Higher value decreases file size, impoves quality and increases conversion time. When set to 255, used as signal to use custom size threshold mode")]
        public byte Compression { get; set; }

        [Option("custom", Required = false, HelpText = "Opens GUI window to choose custom conversion settings")]
        public bool Custom { get; set; } = false;

        [Option("useDownscaling", Required = false, HelpText = "Use downscaling when it's not possible to achive designated size threshold in custom mode")]
        public bool useDownscaling { get; set; } = false;

        [Option('p', "Postfix", Required = false, HelpText = "String added after the original file name")]
        public string Postfix { get; set; } = "";


    }
    internal class CLI_Mode
    {
        ProgressCounter progressCounter;
        TaskbarIcon tbi;
        Options options;
        public CLI_Mode(Options options)
        {
            this.options = options;
        }

        public void Run()
        {
            if (File.Exists(options.Input)) //input is file
            {
                if (options.Output.Length == 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(options.Input) + options.Postfix;
                    options.Output = Path.Combine(Path.GetDirectoryName(options.Input) ?? "", fileName + ".webp");
                }
                try
                {
                    if (options.Compression == 255)
                    {
                        using (var ms = CompressToThreshold(options.Quality, options.Input, options.useDownscaling))
                        using (var fs = File.Create(options.Output))
                        {
                            ms.CopyTo(fs);
                            fs.Close();
                        }
                    }
                    else
                    {
                        using (var ms = ConvertSingleFile(options.Input, options.Quality, options.Compression))
                        using (var fs = File.Create(options.Output))
                        {
                            ms.CopyTo(fs);
                            fs.Close();
                        }
                    }
                    if (options.DeleteOriginal == true && options.Input != options.Output)
                    {
                        File.Delete(options.Input);
                    }
                }
                catch (Exception ex)
                {
                    App.Log(options.Input + " | " + ex.Message);
                    throw new Exception();
                }
            }
            else if (Directory.Exists(options.Input)) //input is folder
            {
                if (options.Output.Length == 0 || !Directory.Exists(options.Output))
                {
                    options.Output = options.Input;
                }
                string[] supportedExtensions = GetAllowedExtensionsFromRegistry();
                List<string> filesToConvert = GetFilesRecursively(options.Input, supportedExtensions);
                progressCounter.tasksToBeDone = filesToConvert.Count;
                progressCounter.tasksCompleted = 0;
                object locker = 0;
                Task t = Task.Run(() =>
                {
                    Parallel.ForEach(filesToConvert, file =>
                    {
                        string relativePath = Path.GetRelativePath(options.Input, file);
                        string fileName = Path.GetFileNameWithoutExtension(file) + options.Postfix;
                        string outputDir = Path.Combine(options.Output, Path.GetDirectoryName(relativePath) ?? "");
                        string outputFile = Path.Combine(outputDir, fileName + ".webp");
                        if (!Directory.Exists(outputDir))
                        {
                            Directory.CreateDirectory(outputDir);
                        }
                        try
                        {
                            if (options.Compression == 255)
                            {
                                using (var ms = CompressToThreshold(options.Quality, file, options.useDownscaling))
                                using (var fs = File.Create(outputFile))
                                {
                                    ms.CopyTo(fs);
                                    fs.Close();
                                }
                            }
                            else
                            {
                                using (var ms = ConvertSingleFile(file, options.Quality, options.Compression))
                                using (var fs = File.Create(outputFile))
                                {
                                    ms.CopyTo(fs);
                                    fs.Close();
                                }
                            }
                            if (options.DeleteOriginal == true && file != outputFile)
                            {
                                File.Delete(file);
                            }
                            GC.Collect(int.MaxValue, GCCollectionMode.Forced, false, true);
                            lock (locker)
                            {
                                progressCounter.tasksCompleted++;
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Log(file + " | " + ex.Message + "\n");
                            throw new Exception();
                        }
                    });
                });
                AttachTrayProgressIcon();
                t.Wait();
            }
            else
            {
                throw new Exception("Input does not exist");
            }
        }
        List<string> GetFilesRecursively(string directory, string[] extensions)
        {
            return Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
                            .Where(file => extensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                            .ToList();
        }

        string[] GetAllowedExtensionsFromRegistry()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(App.regKeyPath))
            {
                string value = key?.GetValue("extensions")?.ToString() ?? "";
                if (string.IsNullOrEmpty(value))
                {
                    throw new Exception("No files with allowed extensions were found");
                }
                return value.Split(';').Select(ext => "." + ext.Trim().ToLowerInvariant()).ToArray();
            }
        }
        MemoryStream ConvertSingleFile(string inputFile, int quality, int compression)
        {
            using (var image = new MagickImage(inputFile))
            {
                byte minPossibleCompression = GetMinimumPossibleCompression(image);
                if (compression < minPossibleCompression)
                {
                    compression = minPossibleCompression;
                }
                if (image.Width >= 16384 || image.Height >= 16384)
                {
                    throw new Exception($"Image {inputFile} is too big");
                }
                image.Format = MagickFormat.WebP;
                if (quality == 100)
                {
                    image.Settings.SetDefine("webp:lossless", "true");
                    image.Quality = 100;
                }
                else
                {
                    image.Quality = (uint)quality;
                }
                image.Settings.SetDefine("webp:method", compression.ToString());
                MemoryStream ms = new MemoryStream();
                image.Write(ms);
                ms.Position = 0;
                return (ms);
            }
        }

        MemoryStream CompressToThreshold(int threshold, string inputFile, bool useDownscaling)
        {
            using (var image = new MagickImage(inputFile))
            {
                if (image.Width >= 16384 || image.Height >= 16384)
                {
                    throw new Exception($"Image {inputFile} is too big");
                }
                byte compression = GetMinimumPossibleCompression(image);
                if (compression == 0) compression = 1;
                image.Format = MagickFormat.WebP;
                image.Settings.SetDefine("webp:method", compression.ToString());
                byte resizesThreshold = 15;
                byte resizesCount = 0;
                CubicSpline spline = GetSizePredictor(image);
                if (spline.Interpolate(99) * 2 < threshold)
                {
                    image.Settings.SetDefine("webp:lossless", "true");
                    image.Quality = 100;
                    image.Settings.SetDefine("webp:method", "6");
                    var resultMs = new MemoryStream();
                    image.Write(resultMs);
                    if (resultMs.Length < threshold)
                    {
                        resultMs.Position = 0;
                        return resultMs;
                    }
                    resultMs.Dispose();
                    image.Settings.RemoveDefine("webp:lossless");
                    image.Settings.SetDefine("webp:method", compression.ToString());
                }
                MagickImage originalImage = null;
                if (useDownscaling)
                {
                    if (originalImage != null) originalImage.Dispose();
                    originalImage = new MagickImage(image);
                }
                while (resizesCount <= resizesThreshold)
                {
                    int i;
                    if (resizesCount == 0)
                    {
                        i = 99;
                    }
                    else
                    {
                        i = 25;
                    }
                    for (; i >= 15; i--)
                    {
                        if (spline.Interpolate(i) > threshold)
                        {
                            continue;
                        }
                        image.Quality = (uint)(i);
                        using (var ms = new MemoryStream())
                        {
                            try
                            {
                                image.Write(ms);
                            }
                            catch (Exception ex)
                            {
                                if (ex.GetType() == typeof(MagickCorruptImageErrorException))
                                {
                                    compression++;
                                    image.Settings.SetDefine("webp:method", compression.ToString());
                                    image.Write(ms);
                                }
                            }
                            if (ms.Length <= threshold || i == 15)
                            {
                                image.Settings.SetDefine("webp:method", "5");
                                var resultMs = new MemoryStream();
                                image.Write(resultMs);
                                if (resultMs.Length <= threshold)
                                {
                                    MemoryStream previous = new MemoryStream();
                                    resultMs.Position = 0;
                                    resultMs.CopyTo(previous);
                                    previous.Position = 0;

                                    for (int j = i + 1; j <= 99; j++)
                                    {
                                        resultMs.SetLength(0);
                                        image.Quality = (uint)(j);
                                        image.Write(resultMs);
                                        if (resultMs.Length >= threshold)
                                        {
                                            resultMs.Dispose();
                                            return previous;
                                        }
                                        previous.SetLength(0);
                                        resultMs.Position = 0;
                                        resultMs.CopyTo(previous);
                                        previous.Position = 0;
                                    }
                                    resultMs.Dispose();
                                    previous.Position = 0;
                                    return previous;
                                }
                                resultMs.Dispose();
                            }
                        }
                    }
                    if (useDownscaling)
                    {
                        image.Resize(originalImage.Width, originalImage.Height);
                        image.CopyPixels(originalImage);
                        image.InterpolativeResize(new Percentage(Math.Pow(0.9, resizesCount + 1) * 100), PixelInterpolateMethod.Spline);
                        image.Settings.SetDefine("webp:method", compression.ToString());
                        spline = GetSizePredictor(image);
                        resizesCount++;
                    }
                    else
                    {
                        break;
                    }
                }
                if (originalImage != null) originalImage.Dispose();
                throw new Exception("Failed to compress image to designated size");
            }
        }

        byte GetMinimumPossibleCompression(MagickImage image) // prevents this https://github.com/dlemstra/Magick.NET/issues/1799 error
        {
            uint pixelsCount = image.Height * image.Width;
            if (pixelsCount < 15000000)
            {
                return 0;
            }
            else if (pixelsCount < 35000000)
            {
                return 1;
            }
            else if (pixelsCount < 80000000)
            {
                return 2;
            }
            else return 3;
        }

        public CubicSpline GetSizePredictor(MagickImage image)
        {
            double[] quality = { 15, 45, 80, 93, 99 };
            double[] size = new double[quality.Length];

            Parallel.For(0, quality.Length, i =>
            {
                using (var localImage = (MagickImage)image.Clone())
                {
                    localImage.Quality = (uint)quality[i];
                    using (var ms = new MemoryStream())
                    {
                        localImage.Write(ms);
                        size[i] = ms.Length;
                    }
                }
            });
            GC.Collect(int.MaxValue, GCCollectionMode.Forced, false, true);
            return CubicSpline.InterpolatePchipSorted(quality, size);
        }

        void AttachTrayProgressIcon()
        {
            tbi = new TaskbarIcon();
            tbi.Icon = Shell_WebP_Converter.Resources.Resources.Icon;
            tbi.ToolTipText = $"0/{progressCounter.tasksToBeDone}";
            try
            {               
                while (progressCounter.tasksCompleted < progressCounter.tasksToBeDone)
                {
                    tbi.ToolTipText = $"{progressCounter.tasksCompleted}/{progressCounter.tasksToBeDone}, {progressCounter.progress}%";      
                    Thread.Sleep(500);
                }               
            }
            catch (Exception ex)
            {

            }
            finally
            {
                tbi.Dispose();
            }
        }

        private struct ProgressCounter
        {
            public int tasksToBeDone;
            public int tasksCompleted;
            public float progress
            {
                get
                {
                    return MathF.Round((float)tasksCompleted / (float)tasksToBeDone * 100, 1);
                }
            }
        }
    }
}
