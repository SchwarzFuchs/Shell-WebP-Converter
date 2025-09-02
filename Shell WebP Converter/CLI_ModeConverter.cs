using CommandLine;
using CommandLine.Text;
using ImageMagick;
using Microsoft.Win32;
using Shell_WebP_Converter.Resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MathNet.Numerics.Interpolation;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Navigation;
using System.Windows;

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
        public bool DeleteOriginal { get; set; }

        [Option('c', "compression", Required = true, HelpText = "Compression, 0-6. Higher value decreases file size, impoves quality and increases conversion time")]
        public byte Compression { get; set; } = 5;

        [Option("custom", Required = false, HelpText = "Opens window to choose custom conversion settings")]
        public bool Custom { get; set; } = false;

        [Option("useDownscaling", Required = false, HelpText = "Use downscaling when it's not possible to achive designated size threshold in custom mode")]
        public bool useDownscaling { get; set; } = false;

    }
    internal class CLI_Mode
    {
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
                    options.Output = Path.Combine(options.Output, Path.ChangeExtension(options.Input, "webp"));
                }
                try
                {
                    if (options.Custom && options.Compression == 255)
                    {
                        File.WriteAllBytes(options.Output, CompressToThreshold(options.Quality, options.Input, options.useDownscaling));
                    }
                    else
                    {
                        ConvertSingleFile(options.Input, options.Output, options.Quality, options.Compression);
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
                if (!Directory.Exists(options.Output) || options.Output.Length == 0)
                {
                    options.Output = options.Input;
                }
                string[] supportedExtensions = GetAllowedExtensionsFromRegistry();
                List<string> filesToConvert = GetFilesRecursively(options.Input, supportedExtensions);

                Parallel.ForEach(filesToConvert, file =>
                {
                    string relativePath = file.Substring(options.Input.Length).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string outputFile = Path.Combine(options.Output, relativePath, file);
                    outputFile = Path.ChangeExtension(outputFile, "webp");
                    string outputDir = Path.GetDirectoryName(outputFile) ?? throw new Exception($"Null path to output dir. File was: {outputFile}");
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    try
                    {
                        if (options.Custom && options.Compression == 255)
                        {
                            File.WriteAllBytes(outputFile, CompressToThreshold(options.Quality, file, options.useDownscaling));
                        }
                        else
                        {
                            ConvertSingleFile(file, outputFile, options.Quality, options.Compression);
                        }
                        if (options.DeleteOriginal == true && file != outputFile)
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Log(file + " | " + ex.Message + "\n");
                        throw new Exception();
                    }

                });
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
                    throw new Exception("No allowed extensions were found in the registry");
                }
                return value.Split(';').Select(ext => "." + ext.Trim().ToLowerInvariant()).ToArray();
            }
        }
        void ConvertSingleFile(string inputFile, string outputFile, int quality, int compression)
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
                image.Write(outputFile);
            }
        }

        byte[] CompressToThreshold(int threshold, string inputFile, bool useDownscaling)
        {
            using (var image = new MagickImage(inputFile))
            {
                if (image.Width >= 16384 || image.Height >= 16384)
                {
                    throw new Exception($"Image {inputFile} is too big");
                }
                byte compression = GetMinimumPossibleCompression(image);
                image.Format = MagickFormat.WebP;
                image.Settings.SetDefine("webp:method", compression.ToString());
                byte resizesThreshold = 15;
                byte resizesCount = 0;
                CubicSpline spline = GetSizePredictor(image);
                if (spline.Interpolate(99) * 2 < threshold)
                {
                    image.Settings.SetDefine("webp:lossless", "true");
                    image.Quality = 100;
                    image.Settings.SetDefine("webp:method", "5");
                    using (var ms = new MemoryStream())
                    {
                        image.Write(ms);
                        if (ms.Length < threshold)
                        {
                            return ms.ToArray();
                        }
                    }
                    image.Settings.RemoveDefine("webp:lossless");
                    image.Settings.SetDefine("webp:method", compression.ToString());
                }
                MagickImage originalImage = new MagickImage();
                if (useDownscaling)
                {
                    originalImage.Dispose();
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
                                ms.SetLength(0);
                                image.Settings.SetDefine("webp:method", "5");
                                image.Write(ms);
                                if (ms.Length <= threshold)
                                {
                                    byte[] previous = ms.ToArray();
                                    for (int j = i + 1; j <= 99; j++)
                                    {
                                        ms.SetLength(0);
                                        image.Quality = (uint)(j);
                                        image.Write(ms);
                                        if (ms.Length >= threshold)
                                        {
                                            return previous;
                                        }
                                        previous = ms.ToArray();
                                    }
                                }
                            }
                        }
                    }
                    if (useDownscaling)
                    {
                        image.Resize(originalImage.Width, originalImage.Height);
                        image.CopyPixels(originalImage);
                        image.InterpolativeResize(new Percentage(Math.Pow(0.9, resizesCount + 1) * 100), PixelInterpolateMethod.Spline);
                        image.Settings.SetDefine("webp:method", compression.ToString());
                        resizesCount++;
                    }
                    else
                    {
                        break;
                    }
                }
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
            double[] quality = { 15, 45, 80, 93, 99};
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
            return CubicSpline.InterpolatePchipSorted(quality, size);
        }
    }
}
