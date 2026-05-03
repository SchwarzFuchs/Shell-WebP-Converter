using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell_WebP_Converter.Models
{
    internal class LossyPresetBase : PresetBase
    {
        public LossyPresetModes PresetMode { get; set; }
        public int Quality { get; set; }
    }
    public enum LossyPresetModes
    {
        ToNQuality,
        ToNSize,
        Custom,
        ToN_SSIM
    }
}
