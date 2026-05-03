using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell_WebP_Converter.Models
{
    internal class LoslessPresetBase : PresetBase
    {
        public LoslessPresetModes PresetMode;
    }

    public enum LoslessPresetModes
    {
        ToNCompression,
        Custom,
    }

}
