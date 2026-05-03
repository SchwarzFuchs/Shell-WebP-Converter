using CommandLine;
using Hardcodet.Wpf.TaskbarNotification;
using ImageMagick;
using ImageMagick.Formats;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using Microsoft.Win32;
using Shell_WebP_Converter.CustomElements;
using Shell_WebP_Converter.Models;
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
    public class WebPConversionOptions
    {

        [Option('i', "input", Required = true, HelpText = "Path to the input file/directory")]
        public string Input { get; set; } = "";

        [Option('o', "output", Required = false, HelpText = "Path to the output directory. Default value — directory of the input file")]
        public string Output { get; set; } = "";

        [Option('q', "quality", Required = true, HelpText = "Quality 0-100, 100 = lossless. In custom size threshold mode used for size threshold (in bytes)")]
        public int Quality { get; set; }

        [Option('d', "deleteOriginal", Required = false, HelpText = "Delete original files, True/False")]
        public bool DeleteOriginal { get; set; } = false;

        [Option('c', "compression", Required = true, HelpText = "Compression, 0-6. Higher value decreases file size, impoves quality and increases conversion time. When set to 255, used as signal to use custom size threshold mode")]
        public byte Compression { get; set; }

        [Option('m', "mode", Required = false, HelpText = "Mode, 0-3. 0 — ToNQuality, 1 — ToNSize, 2 — Customizable, 3 — ToSameQuality")]
        public LossyPresetModes Mode { get; set; } = LossyPresetModes.ToNQuality;

        [Option("useDownscaling", Required = false, HelpText = "Use downscaling when it's not possible to achive designated size threshold in custom mode")]
        public bool UseDownscaling { get; set; } = false;

        [Option('p', "Postfix", Required = false, HelpText = "String added after the original file name")]
        public string Postfix { get; set; } = "";

        [Option('n', "Notify", Required = false, HelpText = "Notify when folder processing is complete")]
        public bool NotifyWhenFolderProcessingEnds { get; set; }

        [Option("overwrite", Required = false, HelpText = "Overwrite files with the same names")]
        public bool OverwriteFiles { get; set; }

    }
    internal class CLI_ModeWebPConverter : CLIConverterBase
    {
        ProgressCounter FolderProcessingProgressCounter = new ProgressCounter();
        TaskbarIcon? TBI;
        WebPConversionOptions Options;
        
        public CLI_ModeWebPConverter(WebPConversionOptions options)
            : base(options.Input, options.Output, options.DeleteOriginal, options.OverwriteFiles)
        {
            this.Options = options;
        }

        protected override string GenerateDefaultOutputPath(string inputFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFile) + Options.Postfix;
            return Path.Combine(Path.GetDirectoryName(inputFile) ?? "", fileName + ".webp");
        }

        protected override MemoryStream ConvertFile(string inputFile)
        {
            return Options.Mode switch
            {
                LossyPresetModes.ToNSize => CompressToThreshold(Options.Quality, inputFile, Options.UseDownscaling),
                LossyPresetModes.ToNQuality => ConvertSingleFile(inputFile, Options.Quality, Options.Compression),
                LossyPresetModes.ToN_SSIM => ConvertToN_SSIM(inputFile, (double)Options.Quality / 10000.0),
                _ => throw new NotSupportedException()
            };
        }

        public override void Run()
        {
            if (File.Exists(Input)) //input is file
            {
                base.Run();
            }
            else if (Directory.Exists(Input)) //input is folder
            {
                if (Output.Length == 0 || !Directory.Exists(Output))
                {
                    Output = Input;
                }
                string[] supportedExtensions = GetAllowedExtensionsFromRegistry();
                List<string> filesToConvert = GetFilesRecursively(Input, supportedExtensions);
                FolderProcessingProgressCounter.tasksToBeDone = filesToConvert.Count;
                FolderProcessingProgressCounter.tasksCompleted = 0;
                object locker = new object();
                Exception taskException = null;
                bool taskCompleted = false;
                InitializeTaskbarIcon();
                Task conversionTask = Task.Run(() =>
                {
                    try
                    {
                        try
                        {
                            Parallel.ForEach(filesToConvert, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
                            {
                                string relativePath = Path.GetRelativePath(Input, file);
                                string fileName = Path.GetFileNameWithoutExtension(file) + Options.Postfix;
                                string outputDir = Path.Combine(Output, Path.GetDirectoryName(relativePath) ?? "");
                                string outputFile = Path.Combine(outputDir, fileName + ".webp");

                                if (!OverwriteFiles)
                                {
                                    outputFile = ConverterCommon.GetUniqueFilePath(outputFile);
                                }

                                if (!Directory.Exists(outputDir))
                                {
                                    Directory.CreateDirectory(outputDir);
                                }
                                try
                                {
                                    Func<MemoryStream> converter = Options.Mode switch
                                    {
                                        LossyPresetModes.ToNSize => () => CompressToThreshold(Options.Quality, file, Options.UseDownscaling),
                                        LossyPresetModes.ToNQuality => () => ConvertSingleFile(file, Options.Quality, Options.Compression),
                                        LossyPresetModes.ToN_SSIM => () => ConvertToN_SSIM(file, (double)Options.Quality / 10000.0),
                                        _ => throw new NotSupportedException($"Unknown mode: {Options.Mode}")
                                    };

                                    using (MemoryStream ms = converter())
                                    using (FileStream fs = File.Create(outputFile))
                                    {
                                        ms.CopyTo(fs);
                                    }
                                    if (DeleteOriginal && file != outputFile)
                                    {
                                        File.Delete(file);
                                    }
                                    lock (locker)
                                    {
                                        FolderProcessingProgressCounter.tasksCompleted++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    App.Log(file + " | " + ex.Message + "\n");
                                    lock (locker)
                                    {
                                        FolderProcessingProgressCounter.tasksCompleted++;
                                        if (taskException == null)
                                        {
                                            taskException = ex;
                                        }
                                    }
                                }
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            if (taskException == null)
                            {
                                taskException = ex;
                            }
                        }
                    }
                    finally
                    {
                        taskCompleted = true;
                    }
                });
                
                try
                {
                    UpdateProgressWhileProcessing(() => taskCompleted);
                    conversionTask.Wait();
                }
                catch (AggregateException ae)
                {
                    taskException = ae.InnerException ?? ae;
                }
                
                if (TBI != null)
                {
                    if (Options.NotifyWhenFolderProcessingEnds == true)
                    {
                        TBI.ShowBalloonTip("Shell WebP converter", String.Format(Shell_WebP_Converter.Resources.Resources.ObjectProcessingCompleted, Path.GetFileName(Input)), BalloonIcon.Info);
                    }
                    TBI.Dispose();
                }
                if (taskException != null)
                {
                    throw taskException;
                }
            }
            else
            {
                throw new Exception("Input does not exist");
            }
        }

        private MemoryStream ConvertSingleFile(string inputFile, int quality, int compression)
        {
            using (MagickImageCollection images = new MagickImageCollection(inputFile))
            {
                if (images[0].Format == MagickFormat.Gif || images[0].Format == MagickFormat.Gif87)
                {
                    images.Coalesce();
                    images.OptimizePlus();
                }
                byte minPossibleCompression = GetMinimumPossibleCompression(images[0].Width * images[0].Height);
                if (compression < minPossibleCompression)
                {
                    compression = minPossibleCompression;
                }
                WebP_SizeCheck(images[0], inputFile);
                WebPWriteDefines defines = new WebPWriteDefines();
                if (quality == 100)
                {
                    defines.Lossless = true;
                    foreach (IMagickImage<ushort> image in images)
                    {
                        image.Quality = 100;
                    }
                }
                else
                {
                    foreach (IMagickImage<ushort> image in images)
                    {
                        image.Quality = (uint)quality;
                    }
                }
                defines.Method = compression;
                defines.AutoFilter = true;
                defines.FilterStrength = 75;
                MemoryStream ms = new MemoryStream();
                images.Write(ms, defines);
                ms.Position = 0;
                return (ms);
            }
        }

        private MemoryStream CompressToThreshold(int threshold, string inputFile, bool useDownscaling)
        {
            using (MagickImageCollection images = new MagickImageCollection(inputFile))
            {
                if (images[0].Format == MagickFormat.Gif || images[0].Format == MagickFormat.Gif87)
                {
                    images.Coalesce();
                    images.OptimizePlus();
                }
                WebP_SizeCheck(images[0], inputFile);
                byte compression = GetMinimumPossibleCompression(images[0].Width * images[0].Height);
                if (compression == 0) compression = 1;

                byte resizesThreshold = 15;
                byte resizesCount = 0;
                CubicSpline sizePredictor = GetSizePredictor(images);
                if (sizePredictor.Interpolate(99) * 2 < threshold)
                {
                    WebPWriteDefines defines = new WebPWriteDefines();
                    defines.Lossless = true;
                    defines.Method = 6;
                    defines.AutoFilter = true;
                    defines.FilterStrength = 75;
                    foreach (IMagickImage<ushort> image in images)
                    {
                        image.Quality = 100;
                    }
                    MemoryStream resultMs = new MemoryStream();
                    try
                    {
                        images.Write(resultMs, defines);
                        if (resultMs.Length < threshold)
                        {
                            resultMs.Position = 0;
                            return resultMs;
                        }
                    }
                    finally
                    {
                        if (resultMs.Length >= threshold)
                        {
                            resultMs.Dispose();
                        }
                    }
                }

                MagickImageCollection originalImages = null;
                try
                {
                    if (useDownscaling)
                    {
                        originalImages = (MagickImageCollection)images.Clone();
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
                            if (sizePredictor.Interpolate(i) > threshold)
                            {
                                continue;
                            }
                            foreach (IMagickImage<ushort> image in images)
                            {
                                image.Quality = (uint)i;
                            }
                            using (MemoryStream ms = new MemoryStream())
                            {
                                try
                                {
                                    WebPWriteDefines defines = new WebPWriteDefines();
                                    defines.Method = compression;
                                    defines.AutoFilter = true;
                                    defines.FilterStrength = 75;
                                    images.Write(ms, defines);
                                }
                                catch (Exception ex)
                                {
                                    if (ex.GetType() == typeof(MagickCorruptImageErrorException))
                                    {
                                        compression++;
                                        WebPWriteDefines defines = new WebPWriteDefines();
                                        defines.Method = compression;
                                        defines.AutoFilter = true;
                                        defines.FilterStrength = 75;
                                        images.Write(ms, defines);
                                    }
                                }
                                if (ms.Length <= threshold || i == 15)
                                {
                                    MemoryStream resultMs = new MemoryStream();
                                    try
                                    {
                                        WebPWriteDefines defines = new WebPWriteDefines();
                                        defines.Method = 5;
                                        defines.AutoFilter = true;
                                        defines.FilterStrength = 75;
                                        images.Write(resultMs, defines);
                                        if (resultMs.Length <= threshold)
                                        {
                                            MemoryStream previous = new MemoryStream();
                                            try
                                            {
                                                resultMs.Position = 0;
                                                resultMs.CopyTo(previous);
                                                previous.Position = 0;

                                                for (int j = i + 1; j <= 99; j++)
                                                {
                                                    resultMs.SetLength(0);
                                                    foreach (IMagickImage<ushort> image in images)
                                                    {
                                                        image.Quality = (uint)j;
                                                    }
                                                    images.Write(resultMs, defines);
                                                    if (resultMs.Length >= threshold)
                                                    {
                                                        resultMs.Dispose();
                                                        previous.Position = 0;
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
                                            catch
                                            {
                                                previous.Dispose();
                                                throw;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (resultMs != null)
                                        {
                                            resultMs.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                        if (useDownscaling)
                        {
                            foreach (IMagickImage<ushort> image in images)
                            {
                                image.Resize(originalImages[0].Width, originalImages[0].Height);
                                image.CopyPixels(originalImages[images.IndexOf(image)]);
                                image.InterpolativeResize(new Percentage(Math.Pow(0.9, resizesCount + 1) * 100), PixelInterpolateMethod.Spline);
                            }
                            sizePredictor = GetSizePredictor(images);
                            resizesCount++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    if (originalImages != null) originalImages.Dispose();
                }
                throw new Exception("Failed to compress image to designated size");
            }
        }

        private MemoryStream ConvertToN_SSIM(string inputFile, double SSIM_Threshold)
        {
            using (MagickImage image = new MagickImage(inputFile))
            {
                WebP_SizeCheck(image, inputFile);
                byte compression = GetMinimumPossibleCompression(image.Width * image.Height);
                if (compression == 0) compression = 1;
                image.Format = MagickFormat.WebP;
                image.Settings.SetDefine("webp:method", compression.ToString());
                CubicSpline spline = GetQualityPredictor(image);
                for (int i = 15; i <= 99; i++)
                {
                    if (spline.Interpolate(i) < Math.Max(0, SSIM_Threshold - 0.005))
                    {
                        continue;
                    }
                    image.Quality = (uint)(i);
                    using (MemoryStream ms = new MemoryStream())
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
                                ms.Position = 0;
                                image.Write(ms);
                            }
                            else
                            {
                                throw;
                            }
                        }
                        ms.Position = 0;
                        using (MagickImage newImage = new MagickImage(ms))
                        {
                            newImage.ColorSpace = ColorSpace.sRGB;
                            if ((1 - 2 * image.Compare(newImage, ErrorMetric.StructuralSimilarity)) > SSIM_Threshold)
                            {
                                image.Settings.SetDefine("webp:method", Options.Compression.ToString());
                                MemoryStream resultMs = new MemoryStream();
                                image.Write(resultMs);
                                resultMs.Position = 0;
                                return resultMs;
                            }
                        }
                    }
                }
                throw new Exception("Failed to convert image to the same quality");
            }
        }

        byte GetMinimumPossibleCompression(uint pixelsCount) // prevents this https://github.com/dlemstra/Magick.NET/issues/1799 error
        {
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
        private CubicSpline GetSizePredictor(MagickImageCollection images)
        {
            double[] quality = { 15, 45, 80, 93, 99 };
            double[] size = new double[quality.Length];

            Parallel.For(0, quality.Length, i =>
            {
                using (MagickImageCollection localImages = new MagickImageCollection())
                {
                    foreach (IMagickImage<ushort> image in images)
                    {
                        localImages.Add(image.Clone());
                    }

                    foreach (IMagickImage<ushort> image in localImages)
                    {
                        image.Quality = (uint)quality[i];
                    }

                    using (MemoryStream resultMs = new MemoryStream())
                    {
                        WebPWriteDefines defines = new WebPWriteDefines();
                        defines.AutoFilter = true;
                        defines.FilterStrength = 75;
                        localImages.Write(resultMs, defines);
                        size[i] = resultMs.Length;
                    }
                }
            });

            GC.Collect(int.MaxValue, GCCollectionMode.Forced, false, true);
            return CubicSpline.InterpolatePchipSorted(quality, size);
        }
        private CubicSpline GetQualityPredictor(MagickImage image)
        {
            double[] quality = { 15, 45, 80, 93, 99 };
            double[] SSIM = new double[quality.Length];

            Parallel.For(0, quality.Length, i =>
            {
                using (MagickImage localImage = (MagickImage)image.Clone())
                {
                    localImage.ColorSpace = ColorSpace.sRGB;
                    localImage.Quality = (uint)quality[i];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        localImage.Write(ms);
                        ms.Position = 0;
                        using (MagickImage newImage = new MagickImage(ms))
                        {
                            newImage.ColorSpace = ColorSpace.sRGB;
                            SSIM[i] = 1 - 2 * localImage.Compare(newImage, ErrorMetric.StructuralSimilarity);
                        }
                    }
                }
            });
            GC.Collect(int.MaxValue, GCCollectionMode.Forced, false, true);
            return CubicSpline.InterpolatePchipSorted(quality, SSIM);
        }

        void InitializeTaskbarIcon()
        {
            TBI = new TaskbarIcon();
            TBI.Icon = Shell_WebP_Converter.Resources.Resources.Icon;
            TBI.ToolTipText = $"0/{FolderProcessingProgressCounter.tasksToBeDone}";
        }

        void WebP_SizeCheck(IMagickImage<ushort> image, string inputFile)
        {
            if (image.Width >= 16384 || image.Height >= 16384)
            {
                throw new Exception($"Image {inputFile} is too big");
            }
        }
        void UpdateProgressWhileProcessing(Func<bool> isCompleted)
        {
            try
            {
                while (!isCompleted())
                {
                    if (FolderProcessingProgressCounter.tasksToBeDone == 0)
                    {
                        break;
                    }
                    if (TBI != null)
                    {
                        TBI.ToolTipText = $"{FolderProcessingProgressCounter.tasksCompleted}/{FolderProcessingProgressCounter.tasksToBeDone}, {FolderProcessingProgressCounter.progress}%";
                    }
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                App.Log("ProgressIcon error: " + ex.Message);
            }
        }
    }
}
