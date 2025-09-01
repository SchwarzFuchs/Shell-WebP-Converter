using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Shell_WebP_Converter
{
    public static class RegistryHelper
    {
        private const string menuName = "ConvertToWebP";
        private const string menuText = "Convert to WebP";
        private const string iconPath = "imageres.dll,-68";
        private const string allowedFileExtensions = ".folder.png.bmp.aai.ai.apng.art.arw.avi.avif.avs.bayer.bpg.bmp.bmp2.bmp3.brf.cals.cin.cip.cmyk.cmyka.cr2.crw.cube.cur.cut.dcm.dcr.dcx.dds.debug.dib.djvu.dmr.dng.dot.dpx.emf.epdf.epi.eps.eps2.eps3.epsf.epsi.ept.exr.farbfeld.fax.fits.fl32.flif.fpx.ftxt.gif.gplt.gray.graya.group4.hdr.heic.hpgl.hrz.html.ico.info.isobrl.isobrl6.jbig.jng.jp2.jpt.j2c.j2k.jpeg.jpg.json.jxl.jxr.kernel.man.mat.miff.mono.mng.m2v.mpeg.mpc.mpo.mpr.mrw.msl.mtv.mvg.nef.orf.ora.otb.p7.palm.pam.clipboard.pbm.pcd.pcds.pcl.pcx.pdb.pdf.pef.pes.pfa.pfb.pfm.pgm.phm.picon.pict.pix.png.png8.png00.png24.png32.png48.png64.pnm.pocketmod.ppm.ps.ps2.ps3.psb.psd.ptif.pwp.qoi.rad.raf.raw.rgb.rgb565.rgba.rgf.rla.rle.sct.sfw.sgi.shtml.sid.sparse-color.strimg.sun.svg.text.tga.tiff.tim.ttf.txt.ubrl.ubrl6.uhdr.uil.uyvy.vicar.video.viff.wbmp.wdp.webp.wmf.wpg.x.xbm.xcf.xpm.xwd.x3f.yaml.ycbcr.ycbcra.yuv.";
        private const string centralKeyPath = @"Software\Classes\WebPConverter\ContextMenu";
        private const string folderKeyPath = $@"Software\Classes\Directory\shell\{menuName}";

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public static List<int> ParsePresets(string presetsString)
        {
            List<int> presets = new List<int>();
            if (string.IsNullOrWhiteSpace(presetsString))
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.EmptyPresetsList}");
            }
            var stringValues = presetsString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var str in stringValues)
            {
                if (int.TryParse(str.Trim(), out int number))
                {
                    if (number >= -1 && number <= 100)
                    {
                        presets.Add(number);
                    }
                    else
                    {
                        throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.InvalidValue}: '{str}'");
                    }
                }
                else
                {
                    throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.InvalidValue}: '{str}'");
                }
            }
            if (presets.Count == 0)
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.Presets}");
            }
            return presets;
        }

        public static List<string> ParseExtensions(string extensionsString, bool addMenuEntryForFolders = false)
        {
            List<string> extensions = new List<string>();
            if (string.IsNullOrWhiteSpace(extensionsString))
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.EmptyExtensionsList}");
            }
            var stringValues = extensionsString.Replace(" ", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var str in stringValues)
            {
                if (allowedFileExtensions.Contains($".{str}."))
                {
                    extensions.Add(str);
                }
                else
                {
                    throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.UnsupportedExtension}: '{str}'");
                }
            }
            if (addMenuEntryForFolders == true && !extensions.Contains("folder"))
            {
                extensions.Add("folder");
            }
            if (extensions.Count == 0)
            {
                throw new ArgumentException($"{Shell_WebP_Converter.Resources.Resources.EmptyExtensionsList}");
            }
            return extensions;
        }

        public static void AddWebPConversionContextMenu(List<string> extensions, List<int> presets, byte compressionLevel, bool deleteOriginal, string converterPath)
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\ShellWebPConverter\ContextMenu", false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\WebPConverter", false); } catch { }

            using (RegistryKey shellKey = Registry.CurrentUser.CreateSubKey($"{centralKeyPath}\\shell"))
            {
                int i = 0;
                foreach (var preset in presets)
                {
                    if (preset != -1)
                    {
                        string qualityKeyName = $"{i:D2}_Quality_{preset}";
                        using (RegistryKey qualityKey = shellKey.CreateSubKey(qualityKeyName))
                        {
                            if (preset == 100)
                            {
                                qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.Quality}: {Shell_WebP_Converter.Resources.Resources.Lossless}");
                            }
                            else
                            {
                                qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.Quality}: {preset}%");
                            }
                            qualityKey.SetValue("Icon", iconPath);
                            using (RegistryKey commandKey = qualityKey.CreateSubKey("command"))
                            {
                                string command = $"\"{converterPath}\" -i \"%1\" -q {preset} -c {compressionLevel} {((deleteOriginal == true) ? "-d" : "")}";
                                commandKey.SetValue("", command);
                            }
                        }
                    }
                    else
                    {
                        string qualityKeyName = $"{i:D2}_Custom";
                        using (RegistryKey qualityKey = shellKey.CreateSubKey(qualityKeyName))
                        {
                            qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.CustomizableQuality}");
                            qualityKey.SetValue("Icon", iconPath);
                            using (RegistryKey commandKey = qualityKey.CreateSubKey("command"))
                            {
                                string command = $"\"{converterPath}\" -i \"%1\" -q 0 -c 0 --custom";
                                commandKey.SetValue("", command);
                            }
                        }
                    }
                    i++;
                }
            }

            foreach (var ext in extensions)
            {
                if (ext == "folder")
                {
                    using (RegistryKey folderKey = Registry.CurrentUser.CreateSubKey(folderKeyPath))
                    {
                        folderKey.SetValue("", menuText);
                        folderKey.SetValue("MUIVerb", menuText);
                        folderKey.SetValue("Icon", iconPath);
                        folderKey.SetValue("ExtendedSubCommandsKey", @"WebPConverter\ContextMenu");
                        folderKey.SetValue("SubCommands", "");
                    }
                }
                else
                {
                    string extKeyPath = $@"Software\Classes\SystemFileAssociations\.{ext}\shell\{menuName}";
                    using (RegistryKey extKey = Registry.CurrentUser.CreateSubKey(extKeyPath))
                    {
                        extKey.SetValue("", menuText);
                        extKey.SetValue("MUIVerb", menuText);
                        extKey.SetValue("Icon", iconPath);
                        extKey.SetValue("ExtendedSubCommandsKey", @"WebPConverter\ContextMenu");
                        extKey.SetValue("SubCommands", "");
                    }
                }
            }

            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }

        public static void RemoveWebPConversionContextMenu(List<string> extensions)
        {
            foreach (var ext in extensions)
            {
                if (ext == "folder")
                {
                    string folderKeyPath = $@"Software\Classes\Directory\shell\{menuName}";
                    try { Registry.CurrentUser.DeleteSubKeyTree(folderKeyPath, false); } catch { }
                }
                else
                {
                    string extKeyPath = $@"Software\Classes\SystemFileAssociations\.{ext}\shell\{menuName}";
                    try { Registry.CurrentUser.DeleteSubKeyTree(extKeyPath, false); } catch { }
                }
            }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\WebPConverter", false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\ShellWebPConverter\ContextMenu", false); } catch { }
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
