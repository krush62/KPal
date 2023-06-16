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
