using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shell_WebP_Converter.Models
{
    public class PresetBase
    {
        public string Name { get; set; }
        public string Postfix { get; set; }
        public bool DeleteOriginal { get; set; } = false;
        public bool UseDownscaling { get; set; } = false;

    }
}
