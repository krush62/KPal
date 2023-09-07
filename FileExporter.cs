/*
This file is part of the KPal distribution (https://github.com/krush62/KPal).
Copyright(c) 2023 Andreas Kruschinski.

This program is free software: you can redistribute it and/or modify  
it under the terms of the GNU General Public License as published by  
the Free Software Foundation, version 3.

This program is distributed in the hope that it will be useful, but 
WITHOUT ANY WARRANTY; without even the implied warranty of 
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
General Public License for more details.
You should have received a copy of the GNU General Public License 
long with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Security;

namespace KPal
{
    internal class FileExporter
    {
        private const string HEX_FORMAT = "X2";

        public enum ExportType
        {
            PNG_1 = 0,
            PNG_8 = 1,
            PNG_32 = 2,
            ASEPRITE = 3,
            GIMP = 4,
            PAINT_NET = 5,
            ADOBE = 6,
            JASC = 7,
            COREL = 8,
            SOC = 9
        }


        public struct ExportFilter
        {
            public delegate void ExportFunction(string fileName, SaveData.SaveConversionData saveData, out bool success);
            public ExportType Type { get; private set; }
            public String Name { get; private set; }
            public String Extension { get; private set; }
            public ExportFunction Efunction { get; private set; }
            public ExportFilter(ExportType type, String name, String extension, ExportFunction callback)
            {
                Type = type;
                Name = name;
                Extension = extension;
                Efunction = callback;
            }
        }

        public static List<ExportFilter> GetAvailableFormats()
        {
            List<ExportFilter> formats = new()
            {
                new ExportFilter(ExportType.PNG_1, "png 1px", "png", WritePNGFile1),
                new ExportFilter(ExportType.PNG_8, "png 8px", "png", WritePNGFile8),
                new ExportFilter(ExportType.PNG_32, "png 32px", "png", WritePNGFile32),
                new ExportFilter(ExportType.ASEPRITE, "aseprite", "aseprite", WriteAsepriteFile),
                new ExportFilter(ExportType.GIMP, "gimp gpl", "gpl", WriteGplFile),
                new ExportFilter(ExportType.PAINT_NET, "paint.net txt", "txt", WriteTxtFile),
                new ExportFilter(ExportType.ADOBE, "adobe ase", "ase", WriteColorSwatchFile),
                new ExportFilter(ExportType.JASC, "jasc pal", "pal", WritePalFile),
                new ExportFilter(ExportType.COREL, "corel xml", "xml", WriteCorelFile),
                new ExportFilter(ExportType.SOC, "openoffice soc", "soc", WriteSOCFile)
            };
            return formats;
        }


        private static bool IsDependentColor(PaletteEditor editor, PaletteColor color, List<ColorLink> links)
        {
            return links.Where(x => x.Target.Color == color && x.Target.Editor == editor).Any();
        }

        private static List<HSVColor> GetColors(SaveData.SaveConversionData saveData)
        {
            List<HSVColor> colorList = new();
            foreach (PaletteEditor editor in saveData.PaletteEditors)
            {
                for (int i = 0; i < editor.PaletteColorList.Count; i++)
                {
                    PaletteColor currentColor = editor.PaletteColorList[i];
                    if (!IsDependentColor(editor, currentColor, saveData.ColorLinks))
                    {
                        colorList.Add(currentColor.HSVColor);
                    }
                }
            }
            colorList = colorList.Distinct().ToList();
            //TODO find and remove remaining duplicates
            

            return colorList;
        }

        private static void WritePNGFile1(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = WritePNGFile(fileName, saveData, 1);
        }

        private static void WritePNGFile8(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = WritePNGFile(fileName, saveData, 8);
        }

        private static void WritePNGFile32(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = WritePNGFile(fileName, saveData, 32);
        }


        public static bool WritePNGFile(string fileName, SaveData.SaveConversionData saveData, int pixelSize)
        {
            bool success = true;
            try
            {
                List<HSVColor> colorList = GetColors(saveData);
                if (colorList.Count > 0)
                {
                    using Bitmap b = new(colorList.Count * pixelSize, pixelSize);
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        for (int i = 0; i < colorList.Count; i++)
                        {
                            System.Windows.Media.Color currentColor = colorList[i].GetRGBColor();
                            System.Drawing.Brush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(currentColor.R, currentColor.G, currentColor.B));
                            g.FillRectangle(brush, i * pixelSize, 0, pixelSize, pixelSize);
                        }
                    }
                    b.Save(fileName, ImageFormat.Png);
                }
                else
                {
                    success = false;
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        private static void WriteGplFile(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = true;
            List<HSVColor> colorList = GetColors(saveData);
            try
            {
                using StreamWriter sw = new(fileName);
                sw.WriteLine("GIMP Palette");
                sw.WriteLine("Name: {0}_{1}", Properties.Resources.Application_Title, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                sw.WriteLine("Columns: 16");
                sw.WriteLine("#");
                foreach (HSVColor color in colorList)
                {
                    System.Windows.Media.Color rgbColor = color.GetRGBColor();
                    string colorName;
                    colorName = ColorNames.Instance.GetColorName(rgbColor);
                    sw.WriteLine("{0} {1} {2} {3}", rgbColor.R.ToString(), rgbColor.G.ToString(), rgbColor.B.ToString(), colorName);
                }
            }
            catch (Exception)
            {
                success = false;
            }
        }

        private static void WriteTxtFile(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = true;
            List<HSVColor> colorList = GetColors(saveData);
            try
            {
                using StreamWriter sw = new(fileName);                
                sw.WriteLine("; {0}_{1}", Properties.Resources.Application_Title, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                int index = 0;
                foreach (HSVColor color in colorList)
                {
                    System.Windows.Media.Color rgbColor = color.GetRGBColor();
                    string colorName;
                    colorName = ColorNames.Instance.GetColorName(rgbColor);
                    sw.WriteLine("{0}{1}{2} ; {3}-{4}", rgbColor.R.ToString(HEX_FORMAT), rgbColor.G.ToString(HEX_FORMAT), rgbColor.B.ToString(HEX_FORMAT), (index++).ToString(), colorName);
                }
            }
            catch (Exception)
            {
                success = false;
            }
        }

        private static void WriteAsepriteFile(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = true;
            List<HSVColor> colorList = GetColors(saveData);

            UInt32 header_00_fileSize = 0;
            UInt16 header_01_magicNumber = 0xA5E0;
            UInt16 header_02_frames = 1;
            UInt16 header_03_width = Convert.ToUInt16(colorList.Count);
            UInt16 header_04_height = 1;
            UInt16 header_05_colorDepth = 8;
            UInt32 header_06_flags = 1;
            UInt16 header_07_speed = 100;
            UInt32 header_08_empty = 0;
            UInt32 header_09_empty = 0;
            Byte header_10_transparentIndex = 0;
            Byte[] header_11_empty = new byte[3];
            UInt16 header_12_colorCount = Convert.ToUInt16(colorList.Count);
            Byte header_13_pixelWidth = 1;
            Byte header_14_pixelHeight = 1;
            Int16 header_15_xPosGrid = 0;
            Int16 header_16_yPosGrid = 0;
            UInt16 header_17_gridWidth = 16;
            UInt16 header_18_gridHeight = 16;
            Byte[] header_19_empty = new byte[84];

            UInt32 frame_00_bytes = 0;
            UInt16 frame_01_magicNumber = 0xF1FA;
            UInt16 frame_02_chunkCount = 5;
            UInt16 frame_03_duration = 100;
            Byte[] frame_04_empty = new byte[2];
            UInt32 frame_05_chunkCount = 5;

            UInt32 colorProfile_00_size = 22;
            UInt16 colorProfile_01_type = 0x2007;
            UInt16 colorProfile_02_profileType = 1;
            UInt16 colorProfile_03_flags = 0;
            UInt32 colorProfile_04_gamma = 0;
            Byte[] colorProfile_05_reserved = new byte[8];

            UInt32 palette_00_size = 26 + Convert.ToUInt32(colorList.Count) * 6;
            UInt16 palette_01_type = 0x2019;
            UInt32 palette_02_colorCount = Convert.ToUInt32(colorList.Count);
            UInt32 palette_03_firstColorIndex = 0;
            UInt32 palette_04_lastColorIndex = Convert.ToUInt32(colorList.Count - 1);
            Byte[] palette_05_reserved = new byte[8];

            UInt32 paletteOld_00_size = 10 + Convert.ToUInt32(colorList.Count) * 3;
            UInt16 paletteOld_01_type = 0x0004;
            UInt16 paletteOld_02_packetCount = 1;
            Byte paletteOld_03_skipEntries = 0;
            Byte paletteOld_04_colorCount = Convert.ToByte(colorList.Count);

            UInt32 layer_00_size = 34;
            UInt16 layer_01_type = 0x2004;
            UInt16 layer_02_flags = 15;
            UInt16 layer_03_type = 0;
            UInt16 layer_04_childLevel = 0;
            UInt16 layer_05_layerWidth = 0;
            UInt16 layer_06_layerHeight = 0;
            UInt16 layer_07_blendMode = 0;
            Byte layer_08_opacity = 255;
            Byte[] layer_09_reserved = new byte[3];
            string layer_11_name = "Background";
            UInt16 layer_10_nameLength = Convert.ToUInt16(layer_11_name.Length);

            UInt32 cel_00_size = 0;
            UInt16 cel_01_type = 0x2005;
            UInt16 cel_02_layerIndex = 0;
            Int16 cel_03_xPos = 0;
            Int16 cel_04_yPos = 0;
            Byte cel_05_opacity = 255;
            UInt16 cel_06_cel_type = 2;
            Int16 cel_07_zIndex = 0;
            Byte[] cel_08_reserved = new byte[5];
            UInt16 cel_09_width = Convert.ToUInt16(colorList.Count);
            UInt16 cel_10_height = 1;

            try
            {
                using FileStream stream = File.Open(fileName, FileMode.Create);
                using BinaryWriter writer = new(stream, Encoding.UTF8, false);
                long headerFileSizePosition = stream.Position;
                //HEADER
                writer.Write(header_00_fileSize);
                writer.Write(header_01_magicNumber);
                writer.Write(header_02_frames);
                writer.Write(header_03_width);
                writer.Write(header_04_height);
                writer.Write(header_05_colorDepth);
                writer.Write(header_06_flags);
                writer.Write(header_07_speed);
                writer.Write(header_08_empty);
                writer.Write(header_09_empty);
                writer.Write(header_10_transparentIndex);
                writer.Write(header_11_empty);
                writer.Write(header_12_colorCount);
                writer.Write(header_13_pixelWidth);
                writer.Write(header_14_pixelHeight);
                writer.Write(header_15_xPosGrid);
                writer.Write(header_16_yPosGrid);
                writer.Write(header_17_gridWidth);
                writer.Write(header_18_gridHeight);
                writer.Write(header_19_empty);

                long frameSizePosition = stream.Position;
                //FRAME HEADER
                writer.Write(frame_00_bytes);
                writer.Write(frame_01_magicNumber);
                writer.Write(frame_02_chunkCount);
                writer.Write(frame_03_duration);
                writer.Write(frame_04_empty);
                writer.Write(frame_05_chunkCount);

                //Color Profile Chunk (0x2007)
                writer.Write(colorProfile_00_size);
                writer.Write(colorProfile_01_type);
                writer.Write(colorProfile_02_profileType);
                writer.Write(colorProfile_03_flags);
                writer.Write(colorProfile_04_gamma);
                writer.Write(colorProfile_05_reserved);

                //Palette Chunk (0x2019)
                writer.Write(palette_00_size);
                writer.Write(palette_01_type);
                writer.Write(palette_02_colorCount);
                writer.Write(palette_03_firstColorIndex);
                writer.Write(palette_04_lastColorIndex);
                writer.Write(palette_05_reserved);
                UInt16 paletteHasName = 0;
                foreach (HSVColor color in colorList)
                {
                    System.Windows.Media.Color c = color.GetRGBColor();
                    writer.Write(paletteHasName);
                    writer.Write(c.R);
                    writer.Write(c.G);
                    writer.Write(c.B);
                    writer.Write(c.A);
                }

                //Old palette chunk (0x0004) ignored
                if (colorList.Count < 256)
                {
                    writer.Write(paletteOld_00_size);
                    writer.Write(paletteOld_01_type);
                    writer.Write(paletteOld_02_packetCount);
                    writer.Write(paletteOld_03_skipEntries);
                    writer.Write(paletteOld_04_colorCount);
                    foreach (HSVColor color in colorList)
                    {
                        System.Windows.Media.Color c = color.GetRGBColor();
                        writer.Write(c.R);
                        writer.Write(c.G);
                        writer.Write(c.B);
                    }
                }

                //Layer Chunk (0x2004)
                writer.Write(layer_00_size);
                writer.Write(layer_01_type);
                writer.Write(layer_02_flags);
                writer.Write(layer_03_type);
                writer.Write(layer_04_childLevel);
                writer.Write(layer_05_layerWidth);
                writer.Write(layer_06_layerHeight);
                writer.Write(layer_07_blendMode);
                writer.Write(layer_08_opacity);
                writer.Write(layer_09_reserved);
                writer.Write(layer_10_nameLength);
                for (int i = 0; i < layer_11_name.Length; i++)
                {
                    char b = layer_11_name[i];
                    writer.Write(b);
                }

                long celSizePosition = stream.Position;
                //Cel Chunk (0x2005)
                writer.Write(cel_00_size);
                writer.Write(cel_01_type);
                writer.Write(cel_02_layerIndex);
                writer.Write(cel_03_xPos);
                writer.Write(cel_04_yPos);
                writer.Write(cel_05_opacity);
                writer.Write(cel_06_cel_type);
                writer.Write(cel_07_zIndex);
                writer.Write(cel_08_reserved);
                writer.Write(cel_09_width);
                writer.Write(cel_10_height);

                byte[] outBytes;
                byte[] colorBytes = new byte[colorList.Count];
                byte a = 0;
                foreach (HSVColor color in colorList)
                {
                    colorBytes[a] = a;
                    a++;
                }

                using (MemoryStream memoryStream = new(colorBytes))
                {
                    using MemoryStream compressStream = new();
                    using ZLibStream compressor = new(compressStream, CompressionMode.Compress);
                    memoryStream.CopyTo(compressor);
                    outBytes = new byte[compressStream.Length];
                    compressor.Close();
                    outBytes = compressStream.ToArray();
                }
                writer.Write(outBytes);

                UInt32 fileSize = Convert.ToUInt32(stream.Position);
                UInt32 frameSize = Convert.ToUInt32(stream.Position - frameSizePosition);
                UInt32 celSize = Convert.ToUInt32(stream.Position - celSizePosition);
                _ = stream.Seek(0, SeekOrigin.Begin);
                writer.Write(fileSize);
                _ = stream.Seek(frameSizePosition, SeekOrigin.Begin);
                writer.Write(frameSize);
                _ = stream.Seek(celSizePosition, SeekOrigin.Begin);
                writer.Write(celSize);
            }
            catch (Exception)
            {
                success = false;
            }
        }

        private static void WriteColorSwatchFile(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = true;
            List<HSVColor> colorList = GetColors(saveData);
            try
            {
                using FileStream stream = File.Open(fileName, FileMode.Create);
                using BinaryWriter writer = new(stream, Encoding.UTF8, false);
                //HEADER
                writer.Write(0x46455341); //file signature
                writer.Write(0x00000100); //version
                writer.Write(BitConverter.GetBytes(Convert.ToInt32(colorList.Count)).Reverse().ToArray()); //block count

                foreach (HSVColor hSVColor in colorList)
                {
                    System.Windows.Media.Color rgbColor = hSVColor.GetRGBColor();
                    string colorName = ColorNames.Instance.GetColorName(rgbColor);
                    writer.Write(Convert.ToInt16(0x0100)); //color start
                    writer.Write(BitConverter.GetBytes(Convert.ToInt32(22 + (colorName.Length * 2))).Reverse().ToArray()); //block size
                    writer.Write(BitConverter.GetBytes(Convert.ToUInt16(colorName.Length + 1)).Reverse().ToArray());//string length

                    foreach (char letter in colorName)
                    {
                        if (letter != '-')
                        {
                            writer.Write(BitConverter.GetBytes(Convert.ToUInt16(letter)).Reverse().ToArray());
                        }
                    }
                    writer.Write(BitConverter.GetBytes(Convert.ToUInt16(0)).Reverse().ToArray()); //string termination
                    writer.Write(Convert.ToByte(0x52));//Color model
                    writer.Write(Convert.ToByte(0x47));
                    writer.Write(Convert.ToByte(0x42));
                    writer.Write(Convert.ToByte(0x20));
                    writer.Write(BitConverter.GetBytes(rgbColor.ScR).Reverse().ToArray());//Color values
                    writer.Write(BitConverter.GetBytes(rgbColor.ScG).Reverse().ToArray());//Color values
                    writer.Write(BitConverter.GetBytes(rgbColor.ScB).Reverse().ToArray());//Color values
                    writer.Write(BitConverter.GetBytes(Convert.ToUInt16(0)).Reverse().ToArray());//Color type
                }
            }
            catch (Exception)
            {
                success = false;
            }
        }

        private static void WritePalFile(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = true;
            List<HSVColor> colorList = GetColors(saveData);
            try
            {
                using StreamWriter sw = new(fileName);
                sw.WriteLine("JASC-PAL");
                sw.WriteLine("0100");
                sw.WriteLine(colorList.Count.ToString());
                foreach (HSVColor color in colorList)
                {
                    System.Windows.Media.Color rgbColor = color.GetRGBColor();
                    sw.WriteLine("{0} {1} {2}", rgbColor.R.ToString(), rgbColor.G.ToString(), rgbColor.B.ToString());
                }
            }
            catch (Exception)
            {
                success = false;
            }
        }

        private static void WriteCorelFile(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = true;
            List<HSVColor> colorList = GetColors(saveData);
            try
            {
                using StreamWriter sw = new(fileName);
                sw.WriteLine("<? version = \"1.0\" ?>");
                sw.WriteLine("<palette name=\"\" guid=\"\">");
                sw.WriteLine("\t<colors>");
                sw.WriteLine("\t\t<page>");
                foreach (HSVColor color in colorList)
                {
                    System.Windows.Media.Color rgbColor = color.GetRGBColor();
                    sw.WriteLine("\t\t\t<color cs=\"RGB\" tints=\"{0},{1},{2}\" name=\"{3}\" />",
                        rgbColor.ScR.ToString(CultureInfo.InvariantCulture),
                        rgbColor.ScG.ToString(CultureInfo.InvariantCulture),
                        rgbColor.ScB.ToString(CultureInfo.InvariantCulture),
                         SecurityElement.Escape(ColorNames.Instance.GetColorName(rgbColor)));
                }
                sw.WriteLine("\t\t</page>");
                sw.WriteLine("\t</colors>");
                sw.WriteLine("</palette>");
            }
            catch (Exception)
            {
                success = false;
            }
        }

        private static void WriteSOCFile(string fileName, SaveData.SaveConversionData saveData, out bool success)
        {
            success = true;
            List<HSVColor> colorList = GetColors(saveData);
            try
            {
                using StreamWriter sw = new(fileName);
                sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                sw.WriteLine("<office:color-table xmlns:office=\"http://openoffice.org/2000/office\" xmlns:style=\"http://openoffice.org/2000/style\" xmlns:text=\"http://openoffice.org/2000/text\" xmlns:table=\"http://openoffice.org/2000/table\" xmlns:draw=\"http://openoffice.org/2000/drawing\" xmlns:fo=\"http://www.w3.org/1999/XSL/Format\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:meta=\"http://openoffice.org/2000/meta\" xmlns:number=\"http://openoffice.org/2000/datastyle\" xmlns:svg=\"http://www.w3.org/2000/svg\" xmlns:chart=\"http://openoffice.org/2000/chart\" xmlns:dr3d=\"http://openoffice.org/2000/dr3d\" xmlns:math=\"http://www.w3.org/1998/Math/MathML\" xmlns:form=\"http://openoffice.org/2000/form\" xmlns:script=\"http://openoffice.org/2000/script\" xmlns:config=\"http://openoffice.org/2001/config\">");
                foreach (HSVColor color in colorList)
                {
                    System.Windows.Media.Color rgbColor = color.GetRGBColor();
                    string colorHex = string.Format("{0}{1}{2}", 
                        rgbColor.R.ToString(HEX_FORMAT).ToLower(),
                        rgbColor.G.ToString(HEX_FORMAT).ToLower(),
                        rgbColor.B.ToString(HEX_FORMAT).ToLower());                    
                    sw.WriteLine("\t<draw:color draw:name=\"{0}\" draw:color=\"#{1}\"/>", SecurityElement.Escape(ColorNames.Instance.GetColorName(rgbColor)), colorHex);
                }
                sw.WriteLine("</office:color-table>");
            }
            catch (Exception)
            {
                success = false;
            }
        }
    }
}
