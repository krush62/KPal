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
    public struct ColorNameCollection
    {
        public string name;
        public List<NamedColor> colors;
        public ColorNameCollection(string n, List<NamedColor> l) 
        {
            name = n;
            colors = l;
        }
    }

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
        public const double AB_MAX_VALUE = 128.0;
        public const double L_MAX_VALUE = 100.0;

        public double L, A, B;

        public LabColor(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }
    }

    public sealed class ColorNames
    {
        public const string COLOR_FILE_PATH = "ColorNames";
        private const int STRING_POS_R = 0;
        private const int STRING_POS_G = 2;
        private const int STRING_POS_B = 4;
        private const int STRING_LENGTH_PER_CHANNEL = 2;
        private const int HEX_BASE = 16;
        private const char CSV_SEPARATOR = ';';
        private const int BUFFER_SIZE = 128;
        private int SelectionIndex = 0;


        private readonly List<ColorNameCollection> colors;

        private static readonly Lazy<ColorNames> lazy =
            new(() => new ColorNames());

        public static ColorNames Instance { get { return lazy.Value; } }

        private ColorNames()
        {
            string[] csvFiles = Directory.GetFiles(COLOR_FILE_PATH, "*.csv", SearchOption.AllDirectories);

            colors = new List<ColorNameCollection>();
            foreach (string csvFile in csvFiles)
            {
                List<NamedColor> cNames = ReadColorFile(csvFile);
                if (cNames != null && cNames.Count  > 0)
                { 
                    colors.Add(new ColorNameCollection(Path.GetFileNameWithoutExtension(csvFile), cNames));
                }
            }
        }

        public void SetSelectionIndex(int newSelIndex)
        {
            if (newSelIndex < 0)
            {
                SelectionIndex = 0;
            }
            else if (newSelIndex >= colors.Count)
            {
                SelectionIndex = colors.Count - 1;
            }
            else
            {
                SelectionIndex = newSelIndex;
            }
        }

        public int GetSelectionIndex() 
        {
            return SelectionIndex;
        }

        public List<string> GetGroupNames()
        {
            List<string> names = new();
            foreach (ColorNameCollection collection in colors)
            {
                names.Add(collection.name);
            }
            return names;
        }


        public string GetColorName(System.Windows.Media.Color color)
        {
            string bestName = Properties.Resources.Color_Unknown;
            double bestDelta = 100;
            foreach (NamedColor c in colors[SelectionIndex].colors)
            {
                if (color.R == c.R && color.G == c.G && color.B == c.B)
                {
                    bestName = c.Name;
                    bestDelta = 0.0;
                    break;
                }
                else
                {
                    double delta = GetDeltaE(color.R, color.G, color.B, c.R, c.G, c.B);
                    if (delta < bestDelta)
                    {
                        bestDelta = delta;
                        bestName = c.Name;
                    }
                }
            }
            return bestName;
        }


        public bool HasData()
        {
            return colors != null && colors.Count > 0;
        }


        public static LabColor RGB2LAB(byte red, byte green, byte blue)
        {
            double r = Convert.ToDouble(red) / 255.0, g = Convert.ToDouble(green) / 255.0, b = Convert.ToDouble(blue) / 255.0, x, y, z;
            r = (r > 0.04045) ? Convert.ToDouble(Math.Pow((r + 0.055) / 1.055, 2.4)) : r / 12.92;
            g = (g > 0.04045) ? Convert.ToDouble(Math.Pow((g + 0.055) / 1.055, 2.4)) : g / 12.92;
            b = (b > 0.04045) ? Convert.ToDouble(Math.Pow((b + 0.055) / 1.055, 2.4)) : b / 12.92;
            x = (r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047;
            y = (r * 0.2126 + g * 0.7152 + b * 0.0722) / 1.00000;
            z = (r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883;
            x = (x > 0.008856) ? Convert.ToDouble(Math.Pow(x, 1.0 / 3.0)) : (7.787 * x) + 16.0 / 116.0;
            y = (y > 0.008856) ? Convert.ToDouble(Math.Pow(y, 1.0 / 3.0)) : (7.787 * y) + 16.0 / 116.0;
            z = (z > 0.008856) ? Convert.ToDouble(Math.Pow(z, 1.0 / 3.0)) : (7.787 * z) + 16.0 / 116.0;
            return new LabColor((116.0 * y) - 16.0, 500.0 * (x - y), 200.0 * (y - z));
        }

        public static double GetDeltaE(byte redA, byte greenA, byte blueA, byte redB, byte greenB, byte blueB)
        {
            LabColor labA = RGB2LAB(redA, greenA, blueA);
            LabColor labB = RGB2LAB(redB, greenB, blueB);
            double deltaL = labA.L - labB.L;
            double deltaA = labA.A - labB.A;
            double deltaB = labA.B - labB.B;
            double c1 = Math.Sqrt(labA.A * labA.A + labA.B * labA.B);
            double c2 = Math.Sqrt(labB.A * labB.A + labB.B * labB.B);
            double deltaC = c1 - c2;
            double deltaH = deltaA * deltaA + deltaB * deltaB - deltaC * deltaC;
            deltaH = deltaH < 0 ? 0 : Math.Sqrt(deltaH);
            double sc = 1.0 + 0.045 * c1;
            double sh = 1.0 + 0.015 * c1;
            double deltaLKlsl = deltaL / 1.0;
            double deltaCkcsc = deltaC / sc;
            double deltaHkhsh = deltaH / sh;
            double i = deltaLKlsl * deltaLKlsl + deltaCkcsc * deltaCkcsc + deltaHkhsh * deltaHkhsh;
            return i < 0.0 ? 0.0 : Math.Sqrt(i);
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
