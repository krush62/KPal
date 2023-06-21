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

namespace KPal
{
    public class HSVColor
    {
        public const int MIN_VALUE = 0;
        public const int MAX_VALUE_VAL_SAT = 100;
        public const int MAX_VALUE_DEGREES = 360;

        public HSVColor(int hue, int sat, int val)
        {
            Hue = hue;
            Saturation = sat;
            Brightness = val;
        }

        public HSVColor()
        {
            hue = MIN_VALUE;
            saturation = MIN_VALUE;
            brightness = MIN_VALUE;
        }

        private int hue;

        public int Hue
        {
            get { return hue; }
            set
            {
                hue = (value % MAX_VALUE_DEGREES + MAX_VALUE_DEGREES) % MAX_VALUE_DEGREES;
            }
        }

        private int saturation;

        public int Saturation
        {
            get { return saturation; }
            set
            {
                if (value < MIN_VALUE)
                {
                    saturation = MIN_VALUE;
                }
                else if (value > MAX_VALUE_VAL_SAT)
                {
                    saturation = MAX_VALUE_VAL_SAT;
                }
                else
                {
                    saturation = value;
                }
            }
        }

        private int brightness;

        public int Brightness
        {
            get { return brightness; }
            set
            {
                if (value < MIN_VALUE)
                {
                    brightness = MIN_VALUE;
                }
                else if (value > MAX_VALUE_VAL_SAT)
                {
                    brightness = MAX_VALUE_VAL_SAT;
                }
                else
                {
                    brightness = value;
                }
            }
        }

        public System.Windows.Media.Color GetRGBColor()
        {
            return HSVToRGB(hue, saturation, brightness);
        }

        public static System.Windows.Media.Color HSVToRGB(int hue, int saturation, int brightness)
        {
            double r, g, b;
            double h = Convert.ToDouble(hue);
            double s = Convert.ToDouble(saturation) / Convert.ToDouble(MAX_VALUE_VAL_SAT);
            double v = Convert.ToDouble(brightness) / Convert.ToDouble(MAX_VALUE_VAL_SAT);

            if (s == MIN_VALUE)
            {
                r = v;
                g = v;
                b = v;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (h == Convert.ToDouble(MAX_VALUE_DEGREES))
                {
                    h = 0;
                }
                else
                {
                    h /= (Convert.ToDouble(MAX_VALUE_DEGREES) / 6.0);
                }

                i = Convert.ToInt32(Math.Truncate(h));
                f = h - i;

                p = v * (1.0 - s);
                q = v * (1.0 - (s * f));
                t = v * (1.0 - (s * (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = v;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = v;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = v;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = v;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = v;
                        break;

                    default:
                        r = v;
                        g = p;
                        b = q;
                        break;
                }

            }
            return System.Windows.Media.Color.FromRgb(Convert.ToByte(r * byte.MaxValue), Convert.ToByte(g * byte.MaxValue), Convert.ToByte(b * byte.MaxValue));
        }



    }
}
