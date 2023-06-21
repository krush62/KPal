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
using System.IO;
using System.Text;

namespace KPal
{
    internal class SaveData
    {
        public List<SavePalette> Palettes { get; private set; }
        public List<SaveColorLink> ColorLinks { get; private set; }
        public Dictionary<OPTION_TYPE_GENERAL, byte> Options { get; private set; }

        public enum OPTION_TYPE_GENERAL : byte
        {
            NONE = 0,
            LEFT_VISUALIZER = 1,
            RIGHT_VISUALIZER = 2,
            RAMP_COUNTER = 3
        }

        public enum OPTION_TYPE_PALETTE : byte
        {
            NONE = 0,
            SAT_CURVE_MODE = 1,
            IS_MINIMIZED = 2
        }

        public class SaveConversionData
        {
            public List<PaletteEditor> PaletteEditors { get; private set; }
            public List<ColorLink> ColorLinks { get; private set; }
            public Dictionary<OPTION_TYPE_GENERAL, byte> Options { get; private set; }
            public SaveConversionData(List<PaletteEditor> pEditors, List<ColorLink> cLinks, Dictionary<OPTION_TYPE_GENERAL, byte> options)
            {
                PaletteEditors = pEditors;
                ColorLinks = cLinks;
                Options = options;
            }
        }


        public struct SavePalette
        {
            public string Name;
            public byte ColorCount;
            public ushort BaseHue;
            public ushort BaseSaturation;
            public sbyte HueShift;
            public float HueShiftExponent;
            public sbyte SaturationShift;
            public float SaturationShiftExponent;
            public byte ValueMin;
            public byte ValueMax;
            public List<SaveHSVShift> HSVShifts;
            public Dictionary<OPTION_TYPE_PALETTE, byte> Options;
        }

        public struct SaveHSVShift
        {
            public SaveHSVShift(sbyte hueShift, sbyte satShift, sbyte valShift)
            {
                HueShift = hueShift;
                SaturationShift = satShift;
                ValueShift = valShift;
            }
            public sbyte HueShift;
            public sbyte SaturationShift;
            public sbyte ValueShift;
        }

        public struct SaveColorLink
        {
            public byte SourcePaletteIndex;
            public byte SourceColorIndex;
            public byte TargetPaletteIndex;
            public byte TargetColorIndex;
        }

        public SaveData()
        {
            Palettes = new List<SavePalette>();
            ColorLinks = new List<SaveColorLink>();
            Options = new Dictionary<OPTION_TYPE_GENERAL, byte>();
        }

        public SaveData(SaveConversionData sData) : this()
        {
            SetData(sData);
        }

        public SaveData(string fileName, out bool success) : this()
        {
            success = ReadFromFile(fileName);
        }


        public void SetData(SaveConversionData sData)
        {
            foreach (PaletteEditor pEditor in sData.PaletteEditors)
            {
                SavePalette palette = new()
                {
                    Name = pEditor.Title,
                    ColorCount = Convert.ToByte(pEditor.ColorCountSlider.Value),
                    BaseHue = Convert.ToUInt16(pEditor.BaseHueSlider.Value),
                    BaseSaturation = Convert.ToUInt16(pEditor.BaseSaturationSlider.Value),
                    HueShift = Convert.ToSByte(pEditor.HueShiftSlider.Value),
                    HueShiftExponent = Convert.ToSingle(pEditor.HueShiftExponentSlider.Value),
                    SaturationShift = Convert.ToSByte(pEditor.SaturationShiftSlider.Value),
                    SaturationShiftExponent = Convert.ToSingle(pEditor.SaturationShiftExponentSlider.Value),
                    ValueMin = Convert.ToByte(pEditor.ValueRangeSlider.LowerValue),
                    ValueMax = Convert.ToByte(pEditor.ValueRangeSlider.HigherValue),
                    Options = new Dictionary<OPTION_TYPE_PALETTE, byte>(),
                    HSVShifts = new List<SaveHSVShift>()
                };
                palette.Options.Add(OPTION_TYPE_PALETTE.SAT_CURVE_MODE, Convert.ToByte(pEditor.SatCurveMode));
                palette.Options.Add(OPTION_TYPE_PALETTE.IS_MINIMIZED, Convert.ToByte(pEditor.IsMinimized ? 0 : 1));
                foreach (PaletteColor paletteColor in pEditor.PaletteColorList)
                {
                    SaveHSVShift shift = new(paletteColor.HueShift, paletteColor.SatShift, paletteColor.ValShift);
                    palette.HSVShifts.Add(shift);
                }

                Palettes.Add(palette);
            }

            foreach (ColorLink link in sData.ColorLinks)
            {
                SaveColorLink cLink = new()
                {
                    SourcePaletteIndex = Convert.ToByte(sData.PaletteEditors.IndexOf(link.Source.Editor)),
                    SourceColorIndex = Convert.ToByte(link.Source.Editor.PaletteColorList.IndexOf(link.Source.Color)),
                    TargetPaletteIndex = Convert.ToByte(sData.PaletteEditors.IndexOf(link.Target.Editor)),
                    TargetColorIndex = Convert.ToByte(link.Target.Editor.PaletteColorList.IndexOf(link.Target.Color))
                };
                ColorLinks.Add(cLink);
            }

            Options = sData.Options;
        }


        public bool SaveToFile(string fileName)
        {
            bool success = true;
            try
            {
                using FileStream stream = File.Open(fileName, FileMode.Create);
                using BinaryWriter writer = new(stream, Encoding.UTF8, false);
                byte optionsLength = Convert.ToByte(Options.Count);
                byte paletteLength = Convert.ToByte(Palettes.Count);
                byte linksLength = Convert.ToByte(ColorLinks.Count);

                writer.Write(optionsLength);
                foreach (KeyValuePair<OPTION_TYPE_GENERAL, byte> option in Options)
                {
                    writer.Write(Convert.ToByte(option.Key));
                    writer.Write(Convert.ToByte(option.Value));
                }

                writer.Write(paletteLength);
                foreach (SavePalette palette in Palettes)
                {
                    writer.Write(palette.Name);
                    writer.Write(palette.ColorCount);
                    writer.Write(palette.BaseHue);
                    writer.Write(palette.BaseSaturation);
                    writer.Write(palette.HueShift);
                    writer.Write(palette.HueShiftExponent);
                    writer.Write(palette.SaturationShift);
                    writer.Write(palette.SaturationShiftExponent);
                    writer.Write(palette.ValueMin);
                    writer.Write(palette.ValueMax);
                    foreach (SaveHSVShift shift in palette.HSVShifts)
                    {
                        writer.Write(shift.HueShift);
                        writer.Write(shift.SaturationShift);
                        writer.Write(shift.ValueShift);
                    }
                    writer.Write(Convert.ToByte(palette.Options.Count));
                    foreach (KeyValuePair<OPTION_TYPE_PALETTE, byte> option in palette.Options)
                    {
                        writer.Write(Convert.ToByte(option.Key));
                        writer.Write(Convert.ToByte(option.Value));
                    }
                }

                writer.Write(linksLength);
                foreach (SaveColorLink link in ColorLinks)
                {
                    writer.Write(link.SourcePaletteIndex);
                    writer.Write(link.SourceColorIndex);
                    writer.Write(link.TargetPaletteIndex);
                    writer.Write(link.TargetColorIndex);
                }
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }


        public bool ReadFromFile(string fileName)
        {
            bool success = true;
            ColorLinks.Clear();
            Palettes.Clear();
            Options.Clear();
            try
            {
                if (File.Exists(fileName))
                {
                    using FileStream stream = File.Open(fileName, FileMode.Open);
                    using BinaryReader reader = new(stream, Encoding.UTF8, false);

                    byte optionLength = reader.ReadByte();
                    for (int i = 0; i < optionLength; i++)
                    {

                        OPTION_TYPE_GENERAL optionId = (OPTION_TYPE_GENERAL)reader.ReadByte();
                        byte value = reader.ReadByte();
                        Options.Add(optionId, value);
                    }

                    byte paletteLength = reader.ReadByte();
                    for (int i = 0; i < paletteLength; i++)
                    {
                        SavePalette savePalette = new()
                        {
                            Name = reader.ReadString(),
                            ColorCount = reader.ReadByte(),
                            BaseHue = reader.ReadUInt16(),
                            BaseSaturation = reader.ReadUInt16(),
                            HueShift = reader.ReadSByte(),
                            HueShiftExponent = reader.ReadSingle(),
                            SaturationShift = reader.ReadSByte(),
                            SaturationShiftExponent = reader.ReadSingle(),
                            ValueMin = reader.ReadByte(),
                            ValueMax = reader.ReadByte(),
                            Options = new Dictionary<OPTION_TYPE_PALETTE, byte>(),
                            HSVShifts = new List<SaveHSVShift>()
                        };
                        for (int j = 0; j < savePalette.ColorCount; j++)
                        {
                            SaveHSVShift saveHSVShift = new()
                            {
                                HueShift = reader.ReadSByte(),
                                SaturationShift = reader.ReadSByte(),
                                ValueShift = reader.ReadSByte()
                            };
                            savePalette.HSVShifts.Add(saveHSVShift);
                        }
                        byte pOptionsLength = reader.ReadByte();
                        for (int j = 0; j < pOptionsLength; j++)
                        {
                            OPTION_TYPE_PALETTE type = (OPTION_TYPE_PALETTE)reader.ReadByte();
                            byte value = reader.ReadByte();
                            savePalette.Options.Add(type, value);

                        }
                        Palettes.Add(savePalette);
                    }

                    byte linkLength = reader.ReadByte();
                    for (int i = 0; i < linkLength; i++)
                    {
                        SaveColorLink saveColorLink = new()
                        {
                            SourcePaletteIndex = reader.ReadByte(),
                            SourceColorIndex = reader.ReadByte(),
                            TargetPaletteIndex = reader.ReadByte(),
                            TargetColorIndex = reader.ReadByte()
                        };
                        ColorLinks.Add(saveColorLink);
                    }
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
    }
}
