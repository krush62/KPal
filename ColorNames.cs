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
    public struct NamedColor
    {
        public NamedColor(string name, byte r, byte g, byte b)
        {
            Name = name;
            R = r;
            G = g;
            B = b;
        }

        public string Name;
        public byte R;
        public byte G;
        public byte B;
    }



    public struct LabColor
    {
        public const float AB_MAX_VALUE = 128.0f;
        public const float L_MAX_VALUE = 100.0f;

        public float L, A, B;

        public LabColor(float l, float a, float b)
        {
            L = l;
            A = a;
            B = b;
        }
    }

    public sealed class ColorNames
    {
        public const string UNKNOWN_COLOR = "UNKNOWN";
        public const string COLOR_FILE_NAME = "colors.csv";
        private const int STRING_POS_R = 0;
        private const int STRING_POS_G = 2;
        private const int STRING_POS_B = 4;
        private const int STRING_LENGTH_PER_CHANNEL = 2;
        private const int HEX_BASE = 16;
        private const char CSV_SEPARATOR = ';';
        private const int BUFFER_SIZE = 128;


        private readonly List<NamedColor> colors;

        private static readonly Lazy<ColorNames> lazy =
            new(() => new ColorNames());

        public static ColorNames Instance { get { return lazy.Value; } }

        private ColorNames()
        {
            colors = ReadColorFile(COLOR_FILE_NAME);
        }


        public string GetColorName(System.Windows.Media.Color color)
        {
            string bestName = UNKNOWN_COLOR;
            float bestDelta = 100f;
            foreach (NamedColor c in colors)
            {
                if (color.R == c.R && color.G == c.G && color.B == c.B)
                {
                    bestName = c.Name;
                    bestDelta = 0.0f;
                    break;
                }
                else
                {
                    float delta = GetDeltaE(color.R, color.G, color.B, c.R, c.G, c.B);
                    if (delta < bestDelta)
                    {
                        bestDelta = delta;
                        bestName = c.Name;
                    }
                }
            }
            return bestName;
        }


        public static LabColor RGB2LAB(byte red, byte green, byte blue)
        {
            float r = Convert.ToSingle(red) / 255.0f, g = Convert.ToSingle(green) / 255.0f, b = Convert.ToSingle(blue) / 255.0f, x, y, z;
            r = (r > 0.04045f) ? Convert.ToSingle(Math.Pow((r + 0.055f) / 1.055f, 2.4f)) : r / 12.92f;
            g = (g > 0.04045f) ? Convert.ToSingle(Math.Pow((g + 0.055f) / 1.055f, 2.4f)) : g / 12.92f;
            b = (b > 0.04045f) ? Convert.ToSingle(Math.Pow((b + 0.055f) / 1.055f, 2.4f)) : b / 12.92f;
            x = (r * 0.4124f + g * 0.3576f + b * 0.1805f) / 0.95047f;
            y = (r * 0.2126f + g * 0.7152f + b * 0.0722f) / 1.00000f;
            z = (r * 0.0193f + g * 0.1192f + b * 0.9505f) / 1.08883f;
            x = (x > 0.008856f) ? Convert.ToSingle(Math.Pow(x, 1.0 / 3.0)) : (7.787f * x) + 16f / 116f;
            y = (y > 0.008856f) ? Convert.ToSingle(Math.Pow(y, 1.0 / 3.0)) : (7.787f * y) + 16f / 116f;
            z = (z > 0.008856f) ? Convert.ToSingle(Math.Pow(z, 1.0 / 3.0)) : (7.787f * z) + 16f / 116f;
            return new LabColor((116f * y) - 16f, 500f * (x - y), 200f * (y - z));
        }

        public static float GetDeltaE(byte redA, byte greenA, byte blueA, byte redB, byte greenB, byte blueB)
        {
            LabColor labA = RGB2LAB(redA, greenA, blueA);
            LabColor labB = RGB2LAB(redB, greenB, blueB);
            float deltaL = labA.L - labB.L;
            float deltaA = labA.A - labB.A;
            float deltaB = labA.B - labB.B;
            float c1 = Convert.ToSingle(Math.Sqrt(labA.A * labA.A + labA.B * labA.B));
            float c2 = Convert.ToSingle(Math.Sqrt(labB.A * labB.A + labB.B * labB.B));
            float deltaC = c1 - c2;
            float deltaH = deltaA * deltaA + deltaB * deltaB - deltaC * deltaC;
            deltaH = deltaH < 0 ? 0 : Convert.ToSingle(Math.Sqrt(deltaH));
            float sc = 1.0f + 0.045f * c1;
            float sh = 1.0f + 0.015f * c1;
            float deltaLKlsl = deltaL / (1.0f);
            float deltaCkcsc = deltaC / (sc);
            float deltaHkhsh = deltaH / (sh);
            float i = deltaLKlsl * deltaLKlsl + deltaCkcsc * deltaCkcsc + deltaHkhsh * deltaHkhsh;
            return i < 0f ? 0f : Convert.ToSingle(Math.Sqrt(i));
        }

        private static List<NamedColor> ReadColorFile(string path)
        {
            List<NamedColor> l = new();
            try
            {
                using FileStream fileStream = File.OpenRead(path);
                {
                    using StreamReader streamReader = new(fileStream, Encoding.UTF8, true, BUFFER_SIZE);
                    string? line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        string[] splits = line.Split(CSV_SEPARATOR);
                        if (splits.Length == 2)
                        {
                            if (splits[1].Length == 6)
                            {
                                //This is to prevent IDE warning, code looks nicer that way
#pragma warning disable IDE0057
                                byte rValue = Convert.ToByte(splits[1].Substring(STRING_POS_R, STRING_LENGTH_PER_CHANNEL), HEX_BASE);
                                byte gValue = Convert.ToByte(splits[1].Substring(STRING_POS_G, STRING_LENGTH_PER_CHANNEL), HEX_BASE);
                                byte bValue = Convert.ToByte(splits[1].Substring(STRING_POS_B, STRING_LENGTH_PER_CHANNEL), HEX_BASE);
#pragma warning restore IDE0057
                                l.Add(new NamedColor(splits[0], rValue, gValue, bValue));
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            return l;
        }
    }
}
