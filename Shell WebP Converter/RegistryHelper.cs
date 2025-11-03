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
        private const string menuName = "ConvertToWebP";
        private const string iconPath = "imageres.dll,-68";
        internal const string allowedFileExtensions = ".folder.png.bmp.aai.ai.apng.art.arw.avi.avif.avs.bayer.bpg.bmp.bmp2.bmp3.brf.cals.cin.cip.cmyk.cmyka.cr2.crw.cube.cur.cut.dcm.dcr.dcx.dds.debug.dib.djvu.dmr.dng.dot.dpx.emf.epdf.epi.eps.eps2.eps3.epsf.epsi.ept.exr.farbfeld.fax.fits.fl32.flif.fpx.ftxt.gif.gplt.gray.graya.group4.hdr.heic.hpgl.hrz.html.ico.info.isobrl.isobrl6.jbig.jng.jp2.jpt.j2c.j2k.jpeg.jpg.json.jxl.jxr.kernel.man.mat.miff.mono.mng.m2v.mpeg.mpc.mpo.mpr.mrw.msl.mtv.mvg.nef.orf.ora.otb.p7.palm.pam.clipboard.pbm.pcd.pcds.pcl.pcx.pdb.pdf.pef.pes.pfa.pfb.pfm.pgm.phm.picon.pict.pix.png.png8.png00.png24.png32.png48.png64.pnm.pocketmod.ppm.ps.ps2.ps3.psb.psd.ptif.pwp.qoi.rad.raf.raw.rgb.rgb565.rgba.rgf.rla.rle.sct.sfw.sgi.shtml.sid.sparse-color.strimg.sun.svg.text.tga.tiff.tim.ttf.txt.ubrl.ubrl6.uhdr.uil.uyvy.vicar.video.viff.wbmp.wdp.webp.wmf.wpg.x.xbm.xcf.xpm.xwd.x3f.yaml.ycbcr.ycbcra.yuv.";
        private const string centralKeyPath = @"Software\Classes\WebPConverter\ContextMenu";
        private const string folderKeyPath = $@"Software\Classes\Directory\shell\{menuName}";

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        internal static void AddWebPConversionContextMenu(List<string> extensions, List<Preset> presets, string converterPath)
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\ShellWebPConverter\ContextMenu", false); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\WebPConverter", false); } catch { }

            using (RegistryKey shellKey = Registry.CurrentUser.CreateSubKey($"{centralKeyPath}\\shell"))
            {
                int i = 0;
                foreach (var preset in presets)
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
                            else if (preset.Quality > 0 && preset.Quality < 100)
                            {
                                qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.Quality} {preset.Quality}%");
                            }
                            else if (preset.Quality == -1)
                            {
                                qualityKey.SetValue("MUIVerb", $"{Shell_WebP_Converter.Resources.Resources.CustomizableQuality}");
                            }
                        }
                            qualityKey.SetValue("Icon", iconPath);
                        if (preset.PresetMode == PresetMode.ToNQuality || preset.PresetMode == PresetMode.ToNSize)
                        {
                            
                            using (RegistryKey commandKey = qualityKey.CreateSubKey("command"))
                            {
                                string command = $"\"{converterPath}\" -i \"%1\" -q {preset.Quality} -c {preset.Compression} {((preset.DeleteOriginal == true) ? "-d" : "")} -p {preset.Postfix}";
                                commandKey.SetValue("", command);
                            }
                        }
                        else if (preset.PresetMode == PresetMode.Custom)
                        {
                            using (RegistryKey commandKey = qualityKey.CreateSubKey("command"))
                            {
                                string command = $"\"{converterPath}\" -i \"%1\" -q 0 -c 0 -p {preset.Postfix} --custom ";
                                commandKey.SetValue("", command);
                            }
                        }
                    }
                    i++;
                }
            }
            AddCommandToExtensions(extensions);
        }

        internal static void AddCommandToExtensions(List<string> extensions)
        {
            foreach (var ext in extensions)
            {
                if (ext == "folder")
                {
                    using (RegistryKey folderKey = Registry.CurrentUser.CreateSubKey(folderKeyPath))
                    {
                        folderKey.SetValue("", Resources.Resources.ConvertToWebP);
                        folderKey.SetValue("MUIVerb", Resources.Resources.ConvertToWebP);
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
                        extKey.SetValue("", Resources.Resources.ConvertToWebP);
                        extKey.SetValue("MUIVerb", Resources.Resources.ConvertToWebP);
                        extKey.SetValue("Icon", iconPath);
                        extKey.SetValue("ExtendedSubCommandsKey", @"WebPConverter\ContextMenu");
                        extKey.SetValue("SubCommands", "");
                    }
                }
            }
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
        internal static void RemoveWebPConversionContextMenu(List<string> extensions)
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
