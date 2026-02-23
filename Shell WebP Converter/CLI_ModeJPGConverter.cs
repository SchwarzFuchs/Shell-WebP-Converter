using CommandLine;
using ImageMagick;
using ImageMagick.Formats;
using ImageMagick.ImageOptimizers;
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

        [Option('q', "quality", Required = true, HelpText = "Quality 0-100, 100 = lossless. In custom size threshold mode used for size threshold (in bytes)")]
        public int Quality { get; set; }

        [Option('d', "deleteOriginal", Required = false, HelpText = "Delete original file, True/False")]
        public bool DeleteOriginal { get; set; } = false;

        [Option("overwrite", Required = false, HelpText = "Overwrite files with the same names")]
        public bool OverwriteFiles { get; set; }

    }
    internal class CLI_ModeJPGConverter
    {
        JPGConversionOptions Options;
        public CLI_ModeJPGConverter(JPGConversionOptions options)
        {
            this.Options = options;
        }

        public void Run()
        {

            if (File.Exists(Options.Input))
            {
                if (Options.Output.Length == 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(Options.Input);
                    Options.Output = Path.Combine(Path.GetDirectoryName(Options.Input) ?? "", fileName + ".jpg");
                }

                if (!Options.OverwriteFiles)
                {
                    Options.Output = ConverterCommon.GetUniqueFilePath(Options.Output);
                }

                try
                {
                    using (MemoryStream ms = ConvertSingleFile(Options.Input))
                    using (FileStream fs = File.Create(Options.Output))
                    {
                        ms.CopyTo(fs);
                    }

                    if (Options.DeleteOriginal == true && Options.Input != Options.Output)
                    {
                        File.Delete(Options.Input);
                    }
                }
                catch (Exception ex)
                {
                    App.Log(Options.Input + " | " + ex.Message);
                    throw ex;
                }
            }
            else
            {
                throw new Exception("Input does not exist");
            }
        }
        MemoryStream ConvertSingleFile(string inputFile)
        {
            using (MagickImage image = new MagickImage(inputFile))
            {
                JpegOptimizer optimizer = new JpegOptimizer();
                JpegWriteDefines defines = new JpegWriteDefines
                {
                    OptimizeCoding = true,
                    DctMethod = JpegDctMethod.Slow
                };
                MemoryStream ms = new MemoryStream();
                image.Format = MagickFormat.Jpg;
                image.Quality = (uint)Options.Quality;
                image.Write(ms, defines);
                ms.Position = 0;
                return (ms);
            }
        }
    }
}
