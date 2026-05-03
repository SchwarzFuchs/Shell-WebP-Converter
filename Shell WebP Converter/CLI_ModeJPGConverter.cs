using CommandLine;
using ImageMagick;
using ImageMagick.Formats;
using ImageMagick.ImageOptimizers;
using MathNet.Numerics.Interpolation;
using Shell_WebP_Converter.CLI;
using Shell_WebP_Converter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shell_WebP_Converter
{
    public class JPGConversionOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to the input file")]
        public string Input { get; set; } = "";

        [Option('o', "output", Required = false, HelpText = "Path to the output directory. Default value — directory of the input file")]
        public string Output { get; set; } = "";

        [Option('p', "Postfix", Required = false, HelpText = "String added after the original file name")]
        public string Postfix { get; set; } = "";

        [Option('q', "quality", Required = true, HelpText = "Quality 0-100. In custom size threshold mode used for size threshold (in bytes)")]
        public int Quality { get; set; }

        [Option('d', "deleteOriginal", Required = false, HelpText = "Delete original file, True/False")]
        public bool DeleteOriginal { get; set; } = false;

        [Option('m', "mode", Required = false, HelpText = "Mode, 0-2. 0 — ToNQuality, 1 — ToNSize, 2 — ToN_SSIM")]
        public LossyPresetModes Mode { get; set; } = LossyPresetModes.ToNQuality;

        [Option("useDownscaling", Required = false, HelpText = "Use downscaling when it's not possible to achieve designated size threshold in custom mode")]
        public bool UseDownscaling { get; set; } = false;

        [Option("overwrite", Required = false, HelpText = "Overwrite files with the same names")]
        public bool OverwriteFiles { get; set; }

    }
    internal class CLI_ModeJPGConverter : Shell_WebP_Converter.CLI.CLIConverterBase
    {
        JPGConversionOptions Options;

        public CLI_ModeJPGConverter(JPGConversionOptions options)
            : base(options.Input, options.Output, options.DeleteOriginal, options.OverwriteFiles)
        {
            this.Options = options;
        }

        protected override string GenerateDefaultOutputPath(string inputFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFile) + Options.Postfix;
            return Path.Combine(Path.GetDirectoryName(inputFile) ?? "", fileName + ".jpg");
        }

        protected override MemoryStream ConvertFile(string inputFile)
        {
            return Options.Mode switch
            {
                LossyPresetModes.ToNSize => CompressToThreshold(Options.Quality, inputFile, Options.UseDownscaling),
                LossyPresetModes.ToNQuality => ConvertSingleFile(inputFile, Options.Quality),
                LossyPresetModes.ToN_SSIM => ConvertToN_SSIM(inputFile, (double)Options.Quality / 10000.0),
                _ => throw new NotSupportedException()
            };
        }

        private MemoryStream ConvertSingleFile(string inputFile, int quality)
        {
            using (MagickImage image = new MagickImage(inputFile))
            {
                JpegWriteDefines defines = new JpegWriteDefines
                {
                    OptimizeCoding = true,
                    DctMethod = JpegDctMethod.Slow
                };
                MemoryStream ms = new MemoryStream();
                image.Format = MagickFormat.Jpg;
                image.Quality = (uint)quality;
                image.Write(ms, defines);
                ms.Position = 0;
                return ms;
            }
        }

        private MemoryStream CompressToThreshold(int threshold, string inputFile, bool useDownscaling)
        {
            using (MagickImage image = new MagickImage(inputFile))
            {
                CubicSpline sizePredictor = GetSizePredictor(image);
                if (sizePredictor.Interpolate(99) * 2 < threshold)
                {
                    JpegWriteDefines defines = new JpegWriteDefines
                    {
                        OptimizeCoding = true,
                        DctMethod = JpegDctMethod.Slow
                    };
                    image.Quality = 100;
                    MemoryStream resultMs = new MemoryStream();
                    try
                    {
                        image.Write(resultMs, defines);
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

                MagickImage originalImage = null;
                try
                {
                    if (useDownscaling)
                    {
                        originalImage = (MagickImage)image.Clone();
                    }

                    for (int i = 99; i >= 15; i--)
                    {
                        if (sizePredictor.Interpolate(i) > threshold)
                        {
                            continue;
                        }
                        image.Quality = (uint)i;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            JpegWriteDefines defines = new JpegWriteDefines
                            {
                                OptimizeCoding = true,
                                DctMethod = JpegDctMethod.Slow
                            };
                            image.Write(ms, defines);

                            if (ms.Length <= threshold || i == 15)
                            {
                                MemoryStream resultMs = new MemoryStream();
                                image.Write(resultMs, defines);
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
                                            image.Quality = (uint)j;
                                            image.Write(resultMs, defines);
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
                                resultMs.Dispose();
                            }
                        }
                    }
                }
                finally
                {
                    if (originalImage != null) originalImage.Dispose();
                }
                throw new Exception("Failed to compress image to designated size");
            }
        }

        private MemoryStream ConvertToN_SSIM(string inputFile, double SSIM_Threshold)
        {
            using (MagickImage image = new MagickImage(inputFile))
            {
                image.Format = MagickFormat.Jpg;
                CubicSpline spline = GetQualityPredictor(image);
                for (int i = 15; i <= 99; i++)
                {
                    if (spline.Interpolate(i) < Math.Max(0, SSIM_Threshold - 0.005))
                    {
                        continue;
                    }
                    image.Quality = (uint)i;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        JpegWriteDefines defines = new JpegWriteDefines
                        {
                            OptimizeCoding = true,
                            DctMethod = JpegDctMethod.Slow
                        };
                        image.Write(ms, defines);
                        ms.Position = 0;
                        using (MagickImage newImage = new MagickImage(ms))
                        {
                            newImage.ColorSpace = ColorSpace.sRGB;
                            if ((1 - 2 * image.Compare(newImage, ErrorMetric.StructuralSimilarity)) > SSIM_Threshold)
                            {
                                MemoryStream resultMs = new MemoryStream();
                                image.Write(resultMs, defines);
                                resultMs.Position = 0;
                                return resultMs;
                            }
                        }
                    }
                }
                throw new Exception("Failed to convert image to the same quality");
            }
        }

        private CubicSpline GetSizePredictor(MagickImage image)
        {
            double[] quality = { 15, 45, 80, 93, 99 };
            double[] size = new double[quality.Length];

            for (int i = 0; i < quality.Length; i++)
            {
                using (MagickImage localImage = (MagickImage)image.Clone())
                {
                    localImage.Quality = (uint)quality[i];
                    using (MemoryStream resultMs = new MemoryStream())
                    {
                        JpegWriteDefines defines = new JpegWriteDefines
                        {
                            OptimizeCoding = true,
                            DctMethod = JpegDctMethod.Slow
                        };
                        localImage.Write(resultMs, defines);
                        size[i] = resultMs.Length;
                    }
                }
            }
            return CubicSpline.InterpolatePchipSorted(quality, size);
        }

        private CubicSpline GetQualityPredictor(MagickImage image)
        {
            double[] quality = { 15, 45, 80, 93, 99 };
            double[] SSIM = new double[quality.Length];

            for (int i = 0; i < quality.Length; i++)
            {
                using (MagickImage localImage = (MagickImage)image.Clone())
                {
                    localImage.ColorSpace = ColorSpace.sRGB;
                    localImage.Quality = (uint)quality[i];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        JpegWriteDefines defines = new JpegWriteDefines
                        {
                            OptimizeCoding = true,
                            DctMethod = JpegDctMethod.Slow
                        };
                        localImage.Write(ms, defines);
                        ms.Position = 0;
                        using (MagickImage newImage = new MagickImage(ms))
                        {
                            newImage.ColorSpace = ColorSpace.sRGB;
                            SSIM[i] = 1 - 2 * localImage.Compare(newImage, ErrorMetric.StructuralSimilarity);
                        }
                    }
                }
            }
            return CubicSpline.InterpolatePchipSorted(quality, SSIM);
        }
    }
}
