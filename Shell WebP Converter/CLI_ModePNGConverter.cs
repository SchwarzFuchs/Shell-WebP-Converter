using CommandLine;
using Hardcodet.Wpf.TaskbarNotification;
using ImageMagick;
using ImageMagick.Formats;
using Shell_WebP_Converter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shell_WebP_Converter.CLI
{
    public class PNGConversionOptions
    {
        [Option('i', "input", Required = true, HelpText = "Path to the input file")]
        public string Input { get; set; } = "";

        [Option('o', "output", Required = false, HelpText = "Path to the output directory. Default value — directory of the input file")]
        public string Output { get; set; } = "";

        [Option('p', "Postfix", Required = false, HelpText = "String added after the original file name")]
        public string Postfix { get; set; } = "";

        [Option('c', "compression", Required = true, HelpText = "Compression, 0-9. Higher value decreases file size and increases conversion time.")]
        public byte Compression { get; set; }

        [Option('f', "filter", Required = true, HelpText = "Filter type, 0-5. Higher value decreases file size and increases conversion time.")]
        public byte Filter { get; set; }

        [Option('m', "mode", Required = false, HelpText = "Mode, 0-1. 0 — ToNCompression, 1 — Custom")]
        public LoslessPresetModes Mode { get; set; } = LoslessPresetModes.ToNCompression;

        [Option('d', "deleteOriginal", Required = false, HelpText = "Delete original file, True/False")]
        public bool DeleteOriginal { get; set; } = false;

        [Option("overwrite", Required = false, HelpText = "Overwrite files with the same names")]
        public bool OverwriteFiles { get; set; }

    }
    internal class CLI_ModePNGConverter : CLIConverterBase
    {
        PNGConversionOptions Options;
        
        public CLI_ModePNGConverter(PNGConversionOptions options)
            : base(options.Input, options.Output, options.DeleteOriginal, options.OverwriteFiles)
        {
            this.Options = options;
        }

        protected override string GenerateDefaultOutputPath(string inputFile)
        {
            string fileName = Path.GetFileNameWithoutExtension(inputFile) + Options.Postfix;
            return Path.Combine(Path.GetDirectoryName(inputFile) ?? "", fileName + ".png");
        }

        protected override MemoryStream ConvertFile(string inputFile)
        {
            return ConvertSingleFile(inputFile, Options.Compression, Options.Filter);
        }

        MemoryStream ConvertSingleFile(string inputFile, byte compression, byte filter)
        {
            using (MagickImage image = new MagickImage(inputFile))
            {
                MemoryStream ms = new MemoryStream();
                image.Format = MagickFormat.Png;
                image.Quality = (uint)compression * 10 + (uint)filter;
                image.Write(ms);
                ms.Position = 0;
                return (ms);
            }
        }
    }
}
