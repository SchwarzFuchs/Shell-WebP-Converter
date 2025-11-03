using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell_WebP_Converter.Models
{
    internal class Preset
    {
        public string Name { get; set; }
        public string Postfix { get; set; }
        public PresetMode PresetMode { get; set; }
        public int Quality { get; set; }
        public byte Compression { get; set; }
        public bool DeleteOriginal { get; set; }
        public bool UseDownscaling { get; set; }

    }
    internal enum PresetMode
    {
        ToNQuality,
        ToNSize,
        Custom
    }
}
