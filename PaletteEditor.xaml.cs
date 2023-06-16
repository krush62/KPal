using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;


namespace KPal
{
    public partial class PaletteEditor : UserControl
    {
        public enum SaturationCurveMode : byte
        {
            ONLY_HIGHER_VALUES = 0,
            HIGHER_AND_LOWER_VALUES = 1
        }

        public List<HSVColor> ColorList { get; private set; }
        public string Title { get; private set; }
        public readonly double minSize, maxSize;
        public event EventHandler? CloseButtonPressed;
        public event EventHandler? LinkCreated;
        public event EventHandler? LinkDeleted;
        public event EventHandler? ControllerColorChanged;
        public event EventHandler? ColorHoverIn;
        public event EventHandler? ColorHoverOut;
        public event EventHandler? ColorsUpdated;
        private int DependentColorIndex;
        private HSVColor? ControllerColor;

        public bool IsControlling { get; private set; }
        public bool IsControlled { get; private set; }
        private SaturationCurveMode satCurveMode;
        public SaturationCurveMode SatCurveMode
        {
            get { return satCurveMode; }
            set
            {
                satCurveMode = value;
                SatStyleButton.IsChecked = satCurveMode != SaturationCurveMode.HIGHER_AND_LOWER_VALUES;
                CalculateColors();
                UpdateColors();
            }
        }
        private bool isMinimized;
        public bool IsMinimized
        {
            get { return isMinimized; }
            set
            {
                if (MinimizeToggleButton.IsChecked.GetValueOrDefault() != value && value != isMinimized)
                {
                    MinimizeToggleButton.IsChecked = value;
                    ToggleCollapseState();
                }
            }
        }

        public PaletteEditor()
        {
            InitializeComponent();
            SatCurveMode = SaturationCurveMode.HIGHER_AND_LOWER_VALUES;
            Title = "";
            IsControlled = false;
            IsControlling = false;
            ColorList = new List<HSVColor>();
            isMinimized = true;
            Double? max = TryFindResource("PaletteEditor_DefaultValue_HeightMaximized") as Double?;
            Double? min = TryFindResource("PaletteEditor_DefaultValue_HeightMinimized") as Double?;
            minSize = min != null ? min.Value : 150;
            maxSize = max != null ? max.Value : 250;
            ToggleCollapseState();
            CreateColorList();
            DependentColorIndex = -1;
        }

        private void SetSliderVisibility()
        {
            ColorCountSlider.IsEnabled = (!IsControlling && !IsControlled);
            BaseHueSlider.IsEnabled = !IsControlled;
            BaseSaturationSlider.IsEnabled = !IsControlled;
        }

        public void UpdateLinks(List<ColorLink> linkList)
        {
            IsControlled = false;
            IsControlling = false;
            if (linkList.Where(p => (p.Target.Editor == this)).Any())
            {
                IsControlled = true;
            }
            if (linkList.Where(p => (p.Source.Editor == this)).Any())
            {
                IsControlling = true;
            }

            SetSliderVisibility();

            //Clear all symbols and set link symbols
            foreach (UIElement uIElement in ColorCenterGrid.Children)
            {
                if (uIElement is PaletteColor pc)
                {
                    pc.LinkSymbol.Visibility = Visibility.Hidden;
                    pc.DeleteLinkButton.Visibility = Visibility.Hidden;
                    foreach (ColorLink link in linkList)
                    {
                        if (link.Target.Color == pc)
                        {
                            pc.DeleteLinkButton.Visibility = Visibility.Visible;
                        }
                        else if (link.Source.Color == pc)
                        {
                            pc.LinkSymbol.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        public void SetTitle(string title)
        {
            Title = title;
            PaletteTitleLabel.Content = title;
        }

        private void SetSliderFromResource(Slider slider, string key)
        {
            Double? i = TryFindResource(key) as Double?;
            if (i != null)
            {
                slider.Value = i.Value;
            }
        }

        private void ColorCountValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(ColorCountSlider, "PaletteEditor_DefaultValue_ColorCount");
        }

        private void BaseHueValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(BaseHueSlider, "PaletteEditor_DefaultValue_BaseHue");
        }

        private void BaseSaturationValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(BaseSaturationSlider, "PaletteEditor_DefaultValue_BaseSaturation");
        }

        private void HueShiftValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(HueShiftSlider, "PaletteEditor_DefaultValue_HueShift");
        }

        private void HueShiftExponentValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(HueShiftExponentSlider, "PaletteEditor_DefaultValue_HueShiftExponent");
        }

        private void SaturationShiftValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(SaturationShiftSlider, "PaletteEditor_DefaultValue_SaturationShift");
        }

        private void SaturationShiftExponentValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(SaturationShiftExponentSlider, "PaletteEditor_DefaultValue_SaturationShiftExponent");
        }

        private void ColorCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CreateColorList();
        }

        private void CreateColorList()
        {
            if (ColorList != null)
            {
                int colCount = Convert.ToInt32(ColorCountSlider.Value);
                ColorCenterGrid.Children.Clear();
                ColorCenterGrid.ColumnDefinitions.Clear();
                ColorList.Clear();
                for (int i = 0; i < colCount; i++)
                {
                    ColumnDefinition columnDefinition = new()
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    };
                    ColorCenterGrid.ColumnDefinitions.Add(columnDefinition);
                    PaletteColor pc = new(this);
                    pc.LinkCreated += PaletteColor_LinkCreated;
                    pc.LinkDeleted += PaletteColor_LinkDeleted;
                    pc.ColorHoverIn += PaletteColor_ColorHoverIn;
                    pc.ColorHoverOut += PaletteColor_ColorHoverOut;
                    Grid.SetRow(pc, 0);
                    Grid.SetColumn(pc, Convert.ToInt32(i * 2));
                    _ = ColorCenterGrid.Children.Add(pc);
                    ColorList.Add(new HSVColor());
                    Rectangle separator = new()
                    {
                        Fill = new SolidColorBrush(Colors.Black)
                    };
                    ColumnDefinition cdSeparator = new()
                    {
                        Width = new GridLength(2, GridUnitType.Pixel)
                    };
                    ColorCenterGrid.ColumnDefinitions.Add(cdSeparator);
                    Grid.SetRow(separator, 0);
                    Grid.SetColumn(separator, Convert.ToInt32(i * 2 + 1));
                    _ = ColorCenterGrid.Children.Add(separator);
                }
                CalculateColors();
                UpdateColors();
            }
        }


        private void PaletteColor_ColorHoverIn(object? sender, EventArgs e)
        {
            if (IsControlled || IsControlling)
            {
                ColorHoverIn?.Invoke(sender, e);
            }
        }

        private void PaletteColor_ColorHoverOut(object? sender, EventArgs e)
        {
            if (IsControlled || IsControlling)
            {
                ColorHoverOut?.Invoke(sender, e);
            }
        }

        private void PaletteColor_LinkDeleted(object? sender, EventArgs e)
        {
            if (e is PaletteColor.LinkDeletedEventArgs ldEventArgs)
            {
                DependentColorIndex = -1;
                ControllerColor = null;
                LinkDeleted?.Invoke(this, ldEventArgs);
                ValueRangeSlider.Minimum = Convert.ToInt32(TryFindResource("PaletteEditor_ValueMin") as double?);
                ValueRangeSlider.Maximum = Convert.ToInt32(TryFindResource("PaletteEditor_ValueMax") as double?);
                CalculateColors();
                UpdateColors();
            }
        }

        public void PaletteColor_LinkCreated(object? sender, EventArgs e)
        {
            if (e is PaletteColor.LinkCreatedEventArgs lcEventArgs)
            {
                LinkCreated?.Invoke(this, lcEventArgs);

                IEnumerable<PaletteColor> paletteColors = ColorCenterGrid.Children.OfType<PaletteColor>();
                if (ColorList != null && ColorList.Count > 0 && paletteColors.Count() == ColorList.Count)
                {
                    for (int i = 0; i < ColorList.Count; ++i)
                    {
                        if (paletteColors.ElementAt(i) == lcEventArgs.ColorLink.Target.Color)
                        {
                            ValueRangeSlider.LowerValue = Convert.ToInt32(TryFindResource("PaletteEditor_ValueMin") as double?);
                            ValueRangeSlider.HigherValue = Convert.ToInt32(TryFindResource("PaletteEditor_ValueMax") as double?);
                            DependentColorIndex = i;
                            ControllerColor = lcEventArgs.ColorLink.Source.Color.ColorHSV;
                            CalculateColors();
                            UpdateColors();
                            break;
                        }
                    }
                }
            }
        }

        public void UpdateDependentColor(PaletteColor paletteColor, HSVColor color)
        {
            if (paletteColor != null)
            {
                IEnumerable<PaletteColor> paletteColors = ColorCenterGrid.Children.OfType<PaletteColor>();
                if (ColorList != null && ColorList.Count > 0 && paletteColors.Count() == ColorList.Count)
                {
                    for (int i = 0; i < ColorList.Count; ++i)
                    {
                        if (paletteColors.ElementAt(i) == paletteColor)
                        {
                            ControllerColor = color;
                            CalculateColors();
                            UpdateColors();
                            break;
                        }
                    }
                }
            }
        }

        private void PaletteSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CalculateColors();
            UpdateColors();
        }

        private void ValueRangeSlider_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            CalculateColors(false);
            UpdateColors();
        }

        private void ValueRangeSlider_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            CalculateColors(true);
            UpdateColors();
        }

        private void CalculateColors(bool controlledByLowerRangeValue = true)
        {
            if (ColorList != null &&
                ValueRangeSlider != null &&
                BaseHueSlider != null && BaseSaturationSlider != null &&
                HueShiftSlider != null && SaturationShiftSlider != null &&
                HueShiftExponentSlider != null && SaturationShiftExponentSlider != null)
            {
                if (!IsControlled)
                {
                    int centerIndex = ColorList.Count / 2;
                    int valueStepSize = Convert.ToInt32((ValueRangeSlider.HigherValue - ValueRangeSlider.LowerValue) / (ColorList.Count - 1));

                    //setting central (center) color
                    ColorList[centerIndex].Hue = Convert.ToInt32(BaseHueSlider.Value);
                    ColorList[centerIndex].Saturation = Convert.ToInt32(BaseSaturationSlider.Value);
                    ColorList[centerIndex].Brightness = Convert.ToInt32((ValueRangeSlider.LowerValue + ((ValueRangeSlider.HigherValue - ValueRangeSlider.LowerValue) / 2)));

                    //setting brighter colors
                    for (int i = (ColorList.Count / 2 + 1); i < ColorList.Count; i++)
                    {
                        double distanceToCenter = Math.Abs(i - centerIndex);
                        ColorList[i].Hue = ColorList[centerIndex].Hue + Convert.ToInt32(HueShiftExponentSlider.Value * HueShiftSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value));
                        ColorList[i].Saturation = ColorList[centerIndex].Saturation + Convert.ToInt32(SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value));
                        ColorList[i].Brightness = ColorList[centerIndex].Brightness + Convert.ToInt32(valueStepSize * distanceToCenter);
                    }

                    //setting darker colors
                    for (int i = (ColorList.Count / 2 - 1); i >= 0; i--)
                    {
                        double distanceToCenter = Math.Abs(i - centerIndex);
                        ColorList[i].Hue = ColorList[centerIndex].Hue - Convert.ToInt32(HueShiftSlider.Value * HueShiftExponentSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value));
                        if (SatCurveMode == SaturationCurveMode.HIGHER_AND_LOWER_VALUES)
                        {
                            ColorList[i].Saturation = ColorList[centerIndex].Saturation + Convert.ToInt32(SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value));
                        }
                        else
                        {
                            ColorList[i].Saturation = ColorList[centerIndex].Saturation;
                        }
                        ColorList[i].Brightness = ColorList[centerIndex].Brightness - Convert.ToInt32(valueStepSize * distanceToCenter);
                    }
                }
                else if (ControllerColor != null && DependentColorIndex != -1)
                {
                    int MINIMUM_VALUE = Convert.ToInt32(TryFindResource("PaletteEditor_ValueMin") as double?);
                    int MAXIMUM_VALUE = Convert.ToInt32(TryFindResource("PaletteEditor_ValueMax") as double?);

                    int min_min = MAXIMUM_VALUE;
                    int max_max = MINIMUM_VALUE;

                    //Calculate Limits for Maximum Value
                    if (DependentColorIndex > 0)
                    {
                        for (int i = MINIMUM_VALUE; i <= ControllerColor.Brightness; i++)
                        {
                            double tempMax = (Convert.ToDouble(i) * (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(ColorList.Count) + 1.0) + (Convert.ToDouble(ColorList.Count) - 1.0) * Convert.ToDouble(ControllerColor.Brightness)) /
                                Convert.ToDouble(DependentColorIndex);
                            if (tempMax <= MAXIMUM_VALUE && tempMax >= MINIMUM_VALUE && tempMax > max_max)
                            {
                                max_max = Convert.ToInt32(tempMax);
                            }
                        }
                    }
                    else
                    {
                        max_max = MAXIMUM_VALUE;
                    }

                    //Calculate Limits for Minimum Value
                    if (DependentColorIndex + 1 != ColorList.Count)
                    {
                        for (int i = ControllerColor.Brightness; i <= MAXIMUM_VALUE; i++)
                        {
                            double tempMin = (Convert.ToDouble(i) * Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(ColorList.Count) * Convert.ToDouble(ControllerColor.Brightness) + Convert.ToDouble(ControllerColor.Brightness)) /
                            (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(ColorList.Count) + 1.0);

                            if (tempMin <= MAXIMUM_VALUE && tempMin >= MINIMUM_VALUE && tempMin < min_min)
                            {
                                min_min = Convert.ToInt32(tempMin);
                            }
                        }
                    }
                    else
                    {
                        min_min = MINIMUM_VALUE;
                    }
                    ValueRangeSlider.Minimum = min_min;
                    ValueRangeSlider.Maximum = max_max;

                    //check if sliders are in an allowed position
                    if (controlledByLowerRangeValue)
                    {
                        if (ValueRangeSlider.LowerValue < min_min || DependentColorIndex == 0)
                        {
                            ValueRangeSlider.LowerValue = min_min;
                        }
                        if (DependentColorIndex > 0)
                        {
                            ValueRangeSlider.HigherValue = Convert.ToInt32((ValueRangeSlider.LowerValue * (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(ColorList.Count) + 1.0) + (Convert.ToDouble(ColorList.Count) - 1.0) * Convert.ToDouble(ControllerColor.Brightness)) /
                                    Convert.ToDouble(DependentColorIndex));
                        }
                    }
                    else
                    {
                        if (ValueRangeSlider.HigherValue > max_max || DependentColorIndex == ColorList.Count - 1)
                        {
                            ValueRangeSlider.HigherValue = max_max;
                        }
                        if (DependentColorIndex + 1 != ColorList.Count)
                        {
                            ValueRangeSlider.LowerValue = Convert.ToInt32((Convert.ToDouble(ValueRangeSlider.HigherValue) * Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(ColorList.Count) * Convert.ToDouble(ControllerColor.Brightness) + Convert.ToDouble(ControllerColor.Brightness)) /
                            (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(ColorList.Count) + 1.0));
                        }
                    }

                    int centerIndex = ColorList.Count / 2;
                    int valueStepSize = Convert.ToInt32((ValueRangeSlider.HigherValue - ValueRangeSlider.LowerValue) / (ColorList.Count - 1));

                    //setting dependent color
                    ColorList[DependentColorIndex].Hue = ControllerColor.Hue;
                    ColorList[DependentColorIndex].Saturation = ControllerColor.Saturation;
                    ColorList[DependentColorIndex].Brightness = ControllerColor.Brightness;

                    //setting center color 
                    double distanceToCenter = Math.Abs(DependentColorIndex - centerIndex);
                    double hueShift = HueShiftSlider.Value * HueShiftExponentSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value);
                    double satShift = SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value);
                    if (centerIndex < DependentColorIndex)
                    {
                        ColorList[centerIndex].Hue = ControllerColor.Hue - Convert.ToInt32(hueShift);
                        ColorList[centerIndex].Brightness = ControllerColor.Brightness - Convert.ToInt32(distanceToCenter * valueStepSize);
                        ColorList[centerIndex].Saturation = ControllerColor.Saturation - Convert.ToInt32(satShift);
                    }
                    else
                    {
                        ColorList[centerIndex].Hue = Convert.ToInt32(ControllerColor.Hue + hueShift);
                        ColorList[centerIndex].Brightness = ControllerColor.Brightness + Convert.ToInt32(distanceToCenter * valueStepSize);
                        if (SatCurveMode == SaturationCurveMode.HIGHER_AND_LOWER_VALUES)
                        {
                            ColorList[centerIndex].Saturation = ControllerColor.Saturation - Convert.ToInt32(satShift);
                        }
                        else
                        {
                            ColorList[centerIndex].Saturation = ControllerColor.Saturation;
                        }
                    }


                    //setting colors
                    for (int i = 0; i < ColorList.Count; i++)
                    {
                        if (i != centerIndex && i != DependentColorIndex)
                        {
                            distanceToCenter = Math.Abs(i - centerIndex);
                            hueShift = HueShiftSlider.Value * HueShiftExponentSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value);
                            satShift = SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value);
                            if (i < centerIndex)
                            {
                                ColorList[i].Hue = ColorList[centerIndex].Hue - Convert.ToInt32(hueShift);
                                if (SatCurveMode == SaturationCurveMode.HIGHER_AND_LOWER_VALUES)
                                {
                                    ColorList[i].Saturation = ColorList[centerIndex].Saturation + Convert.ToInt32(SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value));
                                }
                                else
                                {
                                    ColorList[i].Saturation = ColorList[centerIndex].Saturation;
                                }
                                ColorList[i].Brightness = ColorList[centerIndex].Brightness - Convert.ToInt32(valueStepSize * distanceToCenter);
                            }
                            else
                            {
                                ColorList[i].Hue = ColorList[centerIndex].Hue + Convert.ToInt32(hueShift);
                                ColorList[i].Saturation = ColorList[centerIndex].Saturation + Convert.ToInt32(satShift);
                                ColorList[i].Brightness = ColorList[centerIndex].Brightness + Convert.ToInt32(valueStepSize * distanceToCenter);
                            }
                        }
                    }

                    //setting inactive sliders
                    BaseHueSlider.Value = ColorList[centerIndex].Hue;
                    BaseSaturationSlider.Value = ColorList[centerIndex].Saturation;
                }
            }
        }
        private void UpdateColors()
        {
            IEnumerable<PaletteColor> paletteColors = ColorCenterGrid.Children.OfType<PaletteColor>();
            if (ColorList != null && ColorList.Count > 0 && paletteColors.Count() == ColorList.Count)
            {
                for (int i = 0; i < ColorList.Count; ++i)
                {
                    paletteColors.ElementAt(i).SetColor(ColorList[i]);
                }
                if (IsControlling)
                {
                    ControllerColorChanged?.Invoke(this, EventArgs.Empty);
                }
                ColorsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ValueRangeSlider_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Double? higherValue = TryFindResource("PaletteEditor_DefaultValue_ValueMax") as Double?;
            if (higherValue != null)
            {
                ValueRangeSlider.HigherValue = higherValue.Value;
            }

            Double? lowerValue = TryFindResource("PaletteEditor_DefaultValue_ValueMin") as Double?;
            if (lowerValue != null)
            {
                ValueRangeSlider.LowerValue = lowerValue.Value;
            }
        }

        private void MinimizeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleCollapseState();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseButtonPressed?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleCollapseState()
        {
            if (IsMinimized)
            {
                Height = maxSize;
                BorderRight.Visibility = Visibility.Visible;
                ColorCenterGrid.SetValue(Grid.ColumnSpanProperty, 1);
                SettingsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                Height = minSize;
                BorderRight.Visibility = Visibility.Collapsed;
                ColorCenterGrid.SetValue(Grid.ColumnSpanProperty, 2);
                SettingsGrid.Visibility = Visibility.Collapsed;
            }

            isMinimized = !IsMinimized;

            for (int i = 0; i < ColorCenterGrid.Children.Count; ++i)
            {
                if (ColorCenterGrid.Children[i] is PaletteColor pc)
                {
                    pc.SetMinimized(IsMinimized);
                }
            }
        }

        public PaletteColor GetColorForIndex(int index)
        {
            List<PaletteColor> paletteColors = ColorCenterGrid.Children.OfType<PaletteColor>().ToList();
            return paletteColors[index];
        }

        public bool ContainsColor(PaletteColor color)
        {
            List<PaletteColor> paletteColors = ColorCenterGrid.Children.OfType<PaletteColor>().ToList();
            return paletteColors.Contains(color);
        }

        private void SatStyleButton_Click(object sender, RoutedEventArgs e)
        {
            SatCurveMode = SatStyleButton.IsChecked.GetValueOrDefault() ? PaletteEditor.SaturationCurveMode.ONLY_HIGHER_VALUES : PaletteEditor.SaturationCurveMode.HIGHER_AND_LOWER_VALUES;
            CalculateColors();
            UpdateColors();
        }

        public int GetIndexForColor(PaletteColor color)
        {
            int index = -1;
            IEnumerable<PaletteColor> paletteColors = ColorCenterGrid.Children.OfType<PaletteColor>();
            if (ColorList != null && ColorList.Count > 0 && paletteColors.Count() == ColorList.Count)
            {
                for (int i = 0; i < ColorList.Count; ++i)
                {
                    if (paletteColors.ElementAt(i) == color)
                    {
                        index = i;
                        break;
                    }
                }
            }
            return index;
        }
    }
}
