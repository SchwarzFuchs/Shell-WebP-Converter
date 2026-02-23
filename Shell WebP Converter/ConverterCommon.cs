using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shell_WebP_Converter
{
    internal static class ConverterCommon
    {
        internal static string GetUniqueFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 2;
            string newFilePath;

            do
            {
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension} ({counter}){extension}");
                counter++;
            } while (File.Exists(newFilePath));

            return newFilePath;
        }

        public class ConversionDirectionSetting
        {
            [Option("direction", Required = false, HelpText = "Direction of the conversion, from what file format to what.")]
            public ConversionDirection ConversionDirection { get; set; } = ConversionDirection.AnyToWebP;
        }
        public enum ConversionDirection
        {
            AnyToWebP,
            AnyToPNG,
            AnyToJPG
        }

        public readonly record struct JPG_PNG_ComboConversionPreset(
        string Codename,
        string DisplayName,
        ConversionDirection Direction,
        int JpgQuality,
        int PNG_CompressionLevel,
        int PNG_Filter
        )
        {
            public static readonly JPG_PNG_ComboConversionPreset[] Presets =
            [
            new("00_JPG_MaxQuality",        Resources.Resources.MaximumQuality,   ConversionDirection.AnyToJPG, 100, 0, 0),
            new("01_JPG_HighQuality",       Resources.Resources.HighQuality,      ConversionDirection.AnyToJPG,  90, 0, 0),
            new("02_JPG_MidQuality",        Resources.Resources.MediumQuality,    ConversionDirection.AnyToJPG,  60, 0, 0),
            new("03_PNG_StandardFileSize",  Resources.Resources.PNG_StandardFileSize, ConversionDirection.AnyToPNG, 0, 1, 2),
            new("04_PNG_SmallerFileSize",   Resources.Resources.PNG_SmallerFileSize,  ConversionDirection.AnyToPNG, 0, 9, 5),
    ];
        }
    }

}
