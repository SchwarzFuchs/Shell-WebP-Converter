using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell_WebP_Converter.Models
{
    internal class PNG_Preset : LoslessPresetBase
    {
        public byte Filter { get; set; }
        public byte Compression { get; set; }

    }


}
