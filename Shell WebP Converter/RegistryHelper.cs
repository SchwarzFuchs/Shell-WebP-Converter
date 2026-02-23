using Microsoft.Win32;
using Shell_WebP_Converter.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Shell_WebP_Converter
{
    internal static class RegistryHelper
    {
        private const string MenuName = "ConvertToWebP";
        private const string JPG_PNG_MenuName = "ConvertToJPG_PNG";
        private const string IconPath = "imageres.dll,-68";
        internal const string AllowedFileExtensions = "folder.png.bmp.aai.ai.apng.art.arw.avi.avif.avs.bayer.bpg.bmp.bmp2.bmp3.brf.cals.cin.cip.cmyk.cmyka.cr2.crw.cube.cur.cut.dcm.dcr.dcx.dds.debug.dib.djvu.dmr.dng.dot.dpx.emf.epdf.epi.eps.eps2.eps3.epsf.epsi.ept.exr.farbfeld.fax.fits.fl32.flif.fpx.ftxt.gif.gplt.gray.graya.group4.hdr.heic.hpgl.hrz.html.ico.info.isobrl.isobrl6.jbig.jng.jp2.jpt.j2c.j2k.jpeg.jpg.json.jxl.jxr.kernel.man.mat.miff.mono.mng.m2v.mpeg.mpc.mpo.mpr.mrw.msl.mtv.mvg.nef.orf.ora.otb.p7.palm.pam.clipboard.pbm.pcd.pcds.pcl.pcx.pdb.pdf.pef.pes.pfa.pfb.pfm.pgm.phm.picon.pict.pix.png.png8.png00.png24.png32.png48.png64.pnm.pocketmod.ppm.ps.ps2.ps3.psb.psd.ptif.pwp.qoi.rad.raf.raw.rgb.rgb565.rgba.rgf.rla.rle.sct.sfw.sgi.shtml.sid.sparse-color.strimg.sun.svg.text.tga.tiff.tim.ttf.txt.ubrl.ubrl6.uhdr.uil.uyvy.vicar.video.viff.wbmp.wdp.webp.wmf.wpg.x.xbm.xcf.xpm.xwd.x3f.yaml.ycbcr.ycbcra.yuv.webm.mp4.";
        internal const string PotentiallyAnimatedFileExtensions = ".apng.avi.avif.gif.heic.jng.jxl.miff.mng.mpc.msl.m2v.mpeg.pdf.tiff.video.webp.ico.";
        private const string CentralWebP_KeyPath = @"Software\Classes\WebPConverter\ContextMenu";
        private const string CentralJPG_PNG_KeyPath = @"Software\Classes\WebPConverter\ContextMenuForJPG_PNG";
        private const string FolderKeyPath = $@"Software\Classes\Directory\shell\{MenuName}";

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        internal static void AddConversionContextMenu(List<string> extensions, List<Preset> presets, string converterPath, bool notifyWhenFolderProcessingEnds, bool overwiteFiles, bool addConversionToJPG_PNG_Option)
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(CentralWebP_KeyPath, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(CentralJPG_PNG_KeyPath, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\WebPConverter", false); } catch { }

            using (RegistryKey shellKey = Registry.CurrentUser.CreateSubKey($"{CentralWebP_KeyPath}\\shell"))
            {
                int i = 0;
                foreach (Preset preset in presets)
                {
                    string qualityKeyName = $"{i:D2}_{preset.PresetMode.ToString()}_{preset.Quality}";
                    using (RegistryKey qualityKey = shellKey.CreateSubKey(qualityKeyName))
                    {
                        if (preset.Name.Length > 0)
                        {
                            qualityKey.SetValue("MUIVerb", $"{preset.Name}");
                        }
                        else
                        {
                            if (preset.Quality == 100 && preset.PresetMode == PresetMode.ToNQuality)
                            {
                                qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.Quality} {Shell_WebP_Converter.Resources.Resources.Lossless}");
                            }
                            else if (preset.Quality >= 0 && preset.Quality < 100)
                            {
                                qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.Quality} {preset.Quality}%");
                            }
                            else if (preset.Quality == -1)
                            {
                                qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.CustomizableQuality}");
                            }
                            else if (preset.Quality == 100 && preset.PresetMode == PresetMode.ToN_SSIM)
                            {
                                qualityKey.SetValue("MUIVerb", $"ToSameQuality");
                            }
                        }
                        using (RegistryKey commandKey = qualityKey.CreateSubKey("command"))
                        {
                            string command = $"\"{converterPath}\" --direction {ConverterCommon.ConversionDirection.AnyToWebP} -i \"%1\" -q {preset.Quality} -c {preset.Compression} -m {(int)preset.PresetMode} {((preset.DeleteOriginal == true) ? "-d" : "")} -p {preset.Postfix}" +
                                $"{((notifyWhenFolderProcessingEnds == true) ? " -n" : "")}" +
                                $"{((overwiteFiles == true) ? " --overwrite" : "")}";
                            commandKey.SetValue("", command);
                        }
                        qualityKey.SetValue("Icon", IconPath);
                    }
                    i++;
                }
            }
            if (addConversionToJPG_PNG_Option == true)
            {
                using (RegistryKey shellKey = Registry.CurrentUser.CreateSubKey($"{CentralJPG_PNG_KeyPath}\\shell"))
                {
                    foreach (ConverterCommon.JPG_PNG_ComboConversionPreset preset in ConverterCommon.JPG_PNG_ComboConversionPreset.Presets)
                    {
                        using (RegistryKey qualityKey = shellKey.CreateSubKey(preset.Codename))
                        {
                            if (preset.Direction == ConverterCommon.ConversionDirection.AnyToJPG)
                            {
                                qualityKey.SetValue("MUIVerb", $"JPG, {preset.DisplayName}");
                            }
                            else if (preset.Direction == ConverterCommon.ConversionDirection.AnyToPNG)
                            {
                                qualityKey.SetValue("MUIVerb", $"{preset.DisplayName}");
                            }
                            using (RegistryKey commandKey = qualityKey.CreateSubKey("command"))
                            {
                                string command = "";
                                if (preset.Direction == ConverterCommon.ConversionDirection.AnyToJPG)
                                {
                                    command = $"\"{converterPath}\" --direction {ConverterCommon.ConversionDirection.AnyToJPG} -i \"%1\" -q {preset.JpgQuality} {((overwiteFiles == true) ? " --overwrite" : "")}";
                                }
                                else if (preset.Direction == ConverterCommon.ConversionDirection.AnyToPNG)
                                {
                                    command = $"\"{converterPath}\" --direction {ConverterCommon.ConversionDirection.AnyToPNG} -i \"%1\" -c {preset.PNG_CompressionLevel} -f {preset.PNG_Filter} {((overwiteFiles == true) ? " --overwrite" : "")}";
                                }
                                commandKey.SetValue("", command);
                            }
                            qualityKey.SetValue("Icon", IconPath);
                        }

                    }
                }
            }

            AddCommandToExtensions(extensions, addConversionToJPG_PNG_Option);
        }

        internal static void AddCommandToExtensions(List<string> extensions, bool addConversionToJPG_PNG_Option)
        {
            foreach (string ext in extensions)
            {
                if (ext == "folder")
                {
                    using (RegistryKey folderKey = Registry.CurrentUser.CreateSubKey(FolderKeyPath))
                    {
                        folderKey.SetValue("", Resources.Resources.ConvertToWebP);
                        folderKey.SetValue("MUIVerb", Resources.Resources.ConvertToWebP);
                        folderKey.SetValue("Icon", IconPath);
                        folderKey.SetValue("ExtendedSubCommandsKey", @"WebPConverter\ContextMenu");
                        folderKey.SetValue("SubCommands", "");
                    }
                }
                else
                {
                    string extKeyPath = $@"Software\Classes\SystemFileAssociations\.{ext}\shell\{MenuName}";
                    using (RegistryKey extKey = Registry.CurrentUser.CreateSubKey(extKeyPath))
                    {
                        extKey.SetValue("", Resources.Resources.ConvertToWebP);
                        extKey.SetValue("MUIVerb", Resources.Resources.ConvertToWebP);
                        extKey.SetValue("Icon", IconPath);
                        extKey.SetValue("ExtendedSubCommandsKey", @"WebPConverter\ContextMenu");
                        extKey.SetValue("SubCommands", "");
                    }
                }
                if (ext == "webp" && addConversionToJPG_PNG_Option == true)
                {
                    string extKeyPath = $@"Software\Classes\SystemFileAssociations\.{ext}\shell\{JPG_PNG_MenuName}";
                    using (RegistryKey extKey = Registry.CurrentUser.CreateSubKey(extKeyPath))
                    {
                        extKey.SetValue("", Resources.Resources.ConvertToJPG_PNG);
                        extKey.SetValue("MUIVerb", Resources.Resources.ConvertToJPG_PNG);
                        extKey.SetValue("Icon", IconPath);
                        extKey.SetValue("ExtendedSubCommandsKey", @"WebPConverter\ContextMenuForJPG_PNG");
                        extKey.SetValue("SubCommands", "");
                    }
                }
            }
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
        internal static void RemoveAllConversionContextMenus(List<string> extensions)
        {
            foreach (string ext in extensions)
            {
                if (ext == "folder")
                {
                    string folderKeyPath = $@"Software\Classes\Directory\shell\{MenuName}";
                    try { Registry.CurrentUser.DeleteSubKeyTree(folderKeyPath, false); } catch { }
                }
                else
                {
                    string extKeyPath = $@"Software\Classes\SystemFileAssociations\.{ext}\shell\{MenuName}";
                    try { Registry.CurrentUser.DeleteSubKeyTree(extKeyPath, false); } catch { }
                }
                if (ext == "webp")
                {
                    string jpgPngExtKeyPath = $@"Software\Classes\SystemFileAssociations\.{ext}\shell\{JPG_PNG_MenuName}";
                    try { Registry.CurrentUser.DeleteSubKeyTree(jpgPngExtKeyPath, false); } catch { }
                }
            }
            try { Registry.CurrentUser.DeleteSubKeyTree(CentralWebP_KeyPath, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(CentralJPG_PNG_KeyPath, false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\WebPConverter", false); } catch { }
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
