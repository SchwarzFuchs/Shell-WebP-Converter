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

        [Option('c', "compression", Required = true, HelpText = "Compression, 0-9. Higher value decreases file size and increases conversion time.")]
        public byte Compression { get; set; }

        [Option('f', "filter", Required = true, HelpText = "Filter type, 0-5. Higher value decreases file size and increases conversion time.")]
        public byte Filter { get; set; }

        [Option('d', "deleteOriginal", Required = false, HelpText = "Delete original file, True/False")]
        public bool DeleteOriginal { get; set; } = false;

        [Option("overwrite", Required = false, HelpText = "Overwrite files with the same names")]
        public bool OverwriteFiles { get; set; }

    }
    internal class CLI_ModePNGConverter
    {
        PNGConversionOptions Options;
        public CLI_ModePNGConverter(PNGConversionOptions options)
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
                    Options.Output = Path.Combine(Path.GetDirectoryName(Options.Input) ?? "", fileName + ".png");
                }

                if (!Options.OverwriteFiles)
                {
                    Options.Output = ConverterCommon.GetUniqueFilePath(Options.Output);
                }

                try
                {
                    using (MemoryStream ms = ConvertSingleFile(Options.Input, Options.Compression, Options.Filter))
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
