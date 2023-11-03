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

using Cureos.Numerics.Optimizers;
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
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_HEIGHTMAXIMIZED = "PaletteEditor_DefaultValue_HeightMaximized";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_HEIGHTMINIMIZED = "PaletteEditor_DefaultValue_HeightMinimized";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_COLORCOUNT = "PaletteEditor_DefaultValue_ColorCount";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_BASEHUE = "PaletteEditor_DefaultValue_BaseHue";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_BASESATURATION = "PaletteEditor_DefaultValue_BaseSaturation";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_HUESHIFT = "PaletteEditor_DefaultValue_HueShift";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_HUESHIFTEXPONENT = "PaletteEditor_DefaultValue_HueShiftExponent";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_SATURATIONSHIFT = "PaletteEditor_DefaultValue_SaturationShift";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_SATURATIONSHIFTEXPONENT = "PaletteEditor_DefaultValue_SaturationShiftExponent";
        private const string ID_PALETTEEDITOR_VALUEMIN = "PaletteEditor_ValueMin";
        private const string ID_PALETTEEDITOR_VALUEMAX = "PaletteEditor_ValueMax";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_VALUEMAX = "PaletteEditor_DefaultValue_ValueMax";
        private const string ID_PALETTEEDITOR_DEFAULTVALUE_VALUEMIN = "PaletteEditor_DefaultValue_ValueMin";


        public enum SaturationCurveMode : byte
        {
            ONLY_HIGHER_VALUES = 0,
            HIGHER_AND_LOWER_VALUES = 1,
            LINEAR = 2,
            ONLY_LOWER_VALUES = 3
        }

        public readonly List<PaletteColor> PaletteColorList;
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
                switch (satCurveMode)
                {
                    case SaturationCurveMode.ONLY_HIGHER_VALUES:
                        SatStyleButton1.Visibility = Visibility.Visible;
                        SatStyleButton2.Visibility = Visibility.Hidden;
                        SatStyleButton1.IsChecked = true;
                        break;
                    case SaturationCurveMode.HIGHER_AND_LOWER_VALUES:
                        SatStyleButton1.Visibility = Visibility.Visible;
                        SatStyleButton2.Visibility = Visibility.Hidden;
                        SatStyleButton1.IsChecked = false;
                        break;
                    case SaturationCurveMode.ONLY_LOWER_VALUES:
                        SatStyleButton2.Visibility = Visibility.Visible;
                        SatStyleButton1.Visibility = Visibility.Hidden;
                        SatStyleButton2.IsChecked = true;
                        break;
                    case SaturationCurveMode.LINEAR:
                        SatStyleButton2.Visibility = Visibility.Visible;
                        SatStyleButton1.Visibility = Visibility.Hidden;
                        SatStyleButton2.IsChecked = false;
                        break;
                }
                SatStyleButton1.IsChecked = satCurveMode != SaturationCurveMode.HIGHER_AND_LOWER_VALUES;
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
            PaletteColorList = new();
            InitializeComponent();
            SatCurveMode = SaturationCurveMode.HIGHER_AND_LOWER_VALUES;
            SatStyleButton1.Visibility = Visibility.Visible;
            SatStyleButton2.Visibility = Visibility.Hidden;
            Title = "";
            IsControlled = false;
            IsControlling = false;
            isMinimized = true;
            Double? max = TryFindResource(ID_PALETTEEDITOR_DEFAULTVALUE_HEIGHTMAXIMIZED) as Double?;
            Double? min = TryFindResource(ID_PALETTEEDITOR_DEFAULTVALUE_HEIGHTMINIMIZED) as Double?;
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
            foreach (PaletteColor paletteColor in PaletteColorList)
            {
                paletteColor.LinkSymbol.Visibility = Visibility.Hidden;
                paletteColor.SetControlled(false);
                foreach (ColorLink link in linkList)
                {
                    if (link.Target.Color == paletteColor)
                    {
                        IsControlled = true;
                        paletteColor.SetControlled(true);
                    }
                    else if (link.Source.Color == paletteColor)
                    {
                        paletteColor.LinkSymbol.Visibility = Visibility.Visible;
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
            SetSliderFromResource(ColorCountSlider, ID_PALETTEEDITOR_DEFAULTVALUE_COLORCOUNT);
        }

        private void BaseHueValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(BaseHueSlider, ID_PALETTEEDITOR_DEFAULTVALUE_BASEHUE);
        }

        private void BaseSaturationValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(BaseSaturationSlider, ID_PALETTEEDITOR_DEFAULTVALUE_BASESATURATION);
        }

        private void HueShiftValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(HueShiftSlider, ID_PALETTEEDITOR_DEFAULTVALUE_HUESHIFT);
        }

        private void HueShiftExponentValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(HueShiftExponentSlider, ID_PALETTEEDITOR_DEFAULTVALUE_HUESHIFTEXPONENT);
        }

        private void SaturationShiftValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(SaturationShiftSlider, ID_PALETTEEDITOR_DEFAULTVALUE_SATURATIONSHIFT);
        }

        private void SaturationShiftExponentValueLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SetSliderFromResource(SaturationShiftExponentSlider, ID_PALETTEEDITOR_DEFAULTVALUE_SATURATIONSHIFTEXPONENT);
        }

        private void ColorCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CreateColorList();
        }

        private void CreateColorList()
        {

            int colCount = Convert.ToInt32(ColorCountSlider.Value);
            PaletteColorList.Clear();
            ColorCenterGrid.Children.Clear();
            ColorCenterGrid.ColumnDefinitions.Clear();
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
                pc.AdjustmentChanged += PaletteColor_AdjustmentChanged;
                Grid.SetRow(pc, 0);
                Grid.SetColumn(pc, Convert.ToInt32(i * 2));
                _ = ColorCenterGrid.Children.Add(pc);
                PaletteColorList.Add(pc);
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

        private void PaletteColor_AdjustmentChanged(object? sender, EventArgs e)
        {
            UpdateColors();
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
                ValueRangeSlider.Minimum = Convert.ToInt32(TryFindResource(ID_PALETTEEDITOR_VALUEMIN) as double?);
                ValueRangeSlider.Maximum = Convert.ToInt32(TryFindResource(ID_PALETTEEDITOR_VALUEMAX) as double?);
                OptimizationButton.IsEnabled = true;
                CalculateColors();
                UpdateColors();
            }
        }

        public void PaletteColor_LinkCreated(object? sender, EventArgs e)
        {
            if (e is PaletteColor.LinkCreatedEventArgs lcEventArgs)
            {
                LinkCreated?.Invoke(this, lcEventArgs);
                for (int i = 0; i < PaletteColorList.Count; i++)
                {
                    if (PaletteColorList.ElementAt(i) == lcEventArgs.ColorLink.Target.Color)
                    {
                        if (!lcEventArgs.ColorLink.KeepBrightnessData)
                        {
                            ValueRangeSlider.LowerValue = Convert.ToInt32(TryFindResource(ID_PALETTEEDITOR_VALUEMIN) as double?);
                            ValueRangeSlider.HigherValue = Convert.ToInt32(TryFindResource(ID_PALETTEEDITOR_VALUEMAX) as double?);
                        }
                        DependentColorIndex = i;
                        ControllerColor = lcEventArgs.ColorLink.Source.Color.HSVColor;
                        foreach (PaletteColor paletteColor in PaletteColorList) 
                        {
                            paletteColor.ResetShift();
                        }
                        OptimizationButton.IsEnabled = false;
                        CalculateColors();
                        UpdateColors();
                        break;
                    }
                }
            }
        }

        public void UpdateDependentColor(PaletteColor searchColor, HSVColor color)
        {
            if (searchColor != null)
            {
                foreach (PaletteColor paletteColor in PaletteColorList)
                {
                    if (paletteColor == searchColor)
                    {
                        ControllerColor = color;
                        CalculateColors();
                        UpdateColors();
                        break;
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
            if (ValueRangeSlider != null &&
                BaseHueSlider != null && BaseSaturationSlider != null &&
                HueShiftSlider != null && SaturationShiftSlider != null &&
                HueShiftExponentSlider != null && SaturationShiftExponentSlider != null)
            {
                if (!IsControlled)
                {
                    int centerIndex = PaletteColorList.Count / 2;
                    bool isEven = PaletteColorList.Count % 2 == 0;
                    int valueStepSize = Convert.ToInt32((ValueRangeSlider.HigherValue - ValueRangeSlider.LowerValue) / (PaletteColorList.Count - 1));
                    
                    //setting central (center) color
                    HSVColor centerColor = new()
                    {
                        Hue = Convert.ToInt32(BaseHueSlider.Value),
                        Saturation = Convert.ToInt32(BaseSaturationSlider.Value),
                        Brightness = Convert.ToInt32((ValueRangeSlider.LowerValue + ((ValueRangeSlider.HigherValue - ValueRangeSlider.LowerValue) / 2)))
                    };
                    if (!isEven)
                    { 
                        PaletteColorList[centerIndex].SetColor(centerColor);
                    }

                    //setting brighter colors
                    for (int i = isEven ? (PaletteColorList.Count / 2) : (PaletteColorList.Count / 2 + 1); i < PaletteColorList.Count; i++)
                    {
                        double distanceToCenter = Math.Abs(i - centerIndex);
                        if (isEven)
                        { 
                            distanceToCenter += 0.5;
                        }
                        HSVColor col = new();


                        if (SatCurveMode == SaturationCurveMode.ONLY_LOWER_VALUES)
                        {
                            col.Saturation = centerColor.Saturation;
                        }
                        else if (SatCurveMode == SaturationCurveMode.LINEAR)
                        {
                            col.Saturation = centerColor.Saturation - Convert.ToInt32(SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value));
                        }
                        else
                        {
                            col.Saturation = centerColor.Saturation + Convert.ToInt32(SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value));
                        }

                        col.Hue = centerColor.Hue + Convert.ToInt32(HueShiftExponentSlider.Value * HueShiftSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value));                        
                        col.Brightness = Convert.ToInt32(centerColor.Brightness + (valueStepSize * distanceToCenter));
                        PaletteColorList[i].SetColor(col);
                    }

                    //setting darker colors
                    for (int i = (PaletteColorList.Count / 2 - 1); i >= 0; i--)
                    {
                        double distanceToCenter = Math.Abs(i - centerIndex);
                        if (isEven)
                        {
                            distanceToCenter -= 0.5;
                        }
                        HSVColor col = new();

                        if (SatCurveMode == SaturationCurveMode.ONLY_HIGHER_VALUES)
                        {
                            col.Saturation = centerColor.Saturation;
                        }
                        else
                        {
                            col.Saturation = centerColor.Saturation + Convert.ToInt32(SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value));
                        }

                        col.Hue = centerColor.Hue - Convert.ToInt32(HueShiftSlider.Value * HueShiftExponentSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value));                        
                        col.Brightness = centerColor.Brightness - Convert.ToInt32(valueStepSize * distanceToCenter);
                        PaletteColorList[i].SetColor(col);
                    }
                }
                else if (ControllerColor != null && DependentColorIndex != -1)
                {
                    int MINIMUM_VALUE = Convert.ToInt32(TryFindResource(ID_PALETTEEDITOR_VALUEMIN) as double?);
                    int MAXIMUM_VALUE = Convert.ToInt32(TryFindResource(ID_PALETTEEDITOR_VALUEMAX) as double?);

                    int min_min = MAXIMUM_VALUE;
                    int max_max = MINIMUM_VALUE;

                    //Calculate Limits for Maximum Value
                    if (DependentColorIndex > 0)
                    {
                        for (int i = MINIMUM_VALUE; i <= ControllerColor.Brightness; i++)
                        {
                            double tempMax = (Convert.ToDouble(i) * (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(PaletteColorList.Count) + 1.0) + (Convert.ToDouble(PaletteColorList.Count) - 1.0) * Convert.ToDouble(ControllerColor.Brightness)) /
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
                    if (DependentColorIndex + 1 != PaletteColorList.Count)
                    {
                        for (int i = ControllerColor.Brightness; i <= MAXIMUM_VALUE; i++)
                        {
                            double tempMin = (Convert.ToDouble(i) * Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(PaletteColorList.Count) * Convert.ToDouble(ControllerColor.Brightness) + Convert.ToDouble(ControllerColor.Brightness)) /
                            (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(PaletteColorList.Count) + 1.0);

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
                            ValueRangeSlider.HigherValue = Convert.ToInt32((ValueRangeSlider.LowerValue * (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(PaletteColorList.Count) + 1.0) + (Convert.ToDouble(PaletteColorList.Count) - 1.0) * Convert.ToDouble(ControllerColor.Brightness)) /
                                    Convert.ToDouble(DependentColorIndex));
                        }
                    }
                    else
                    {
                        if (ValueRangeSlider.HigherValue > max_max || DependentColorIndex == PaletteColorList.Count - 1)
                        {
                            ValueRangeSlider.HigherValue = max_max;
                        }
                        if (DependentColorIndex + 1 != PaletteColorList.Count)
                        {
                            ValueRangeSlider.LowerValue = Convert.ToInt32((Convert.ToDouble(ValueRangeSlider.HigherValue) * Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(PaletteColorList.Count) * Convert.ToDouble(ControllerColor.Brightness) + Convert.ToDouble(ControllerColor.Brightness)) /
                            (Convert.ToDouble(DependentColorIndex) - Convert.ToDouble(PaletteColorList.Count) + 1.0));
                        }
                    }

                    int centerIndex = PaletteColorList.Count / 2;
                    bool isEven = PaletteColorList.Count % 2 == 0;
                    int valueStepSize = Convert.ToInt32((ValueRangeSlider.HigherValue - ValueRangeSlider.LowerValue) / (PaletteColorList.Count - 1));

                    //setting dependent color
                    HSVColor depColor = new()
                    {
                        Hue = ControllerColor.Hue,
                        Saturation = ControllerColor.Saturation,
                        Brightness = ControllerColor.Brightness
                    };
                    PaletteColorList[DependentColorIndex].SetColor(depColor);

                    //setting center color                     
                    double distanceToCenter = Math.Abs(DependentColorIndex - centerIndex);                    
                    HSVColor centerColor = new();
                    if (centerIndex < DependentColorIndex)
                    {
                        if (isEven)
                        {
                            distanceToCenter += 0.5;
                        }
                        double hueShift = HueShiftSlider.Value * HueShiftExponentSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value);
                        double satShift = SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value);
                        centerColor.Hue = ControllerColor.Hue - Convert.ToInt32(hueShift);
                        centerColor.Brightness = ControllerColor.Brightness - Convert.ToInt32(distanceToCenter * valueStepSize);

                        if (SatCurveMode == SaturationCurveMode.ONLY_LOWER_VALUES)
                        {
                            centerColor.Saturation = ControllerColor.Saturation;
                        }
                        else if (SatCurveMode == SaturationCurveMode.LINEAR)
                        {
                            centerColor.Saturation = ControllerColor.Saturation + Convert.ToInt32(satShift);
                        }
                        else
                        {
                            centerColor.Saturation = ControllerColor.Saturation - Convert.ToInt32(satShift);
                        }
                    }
                    else
                    {
                        if (isEven)
                        {
                            distanceToCenter -= 0.5;
                        }
                        double hueShift = HueShiftSlider.Value * HueShiftExponentSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value);
                        double satShift = SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value);
                        centerColor.Hue = Convert.ToInt32(ControllerColor.Hue + hueShift);
                        centerColor.Brightness = ControllerColor.Brightness + Convert.ToInt32(distanceToCenter * valueStepSize);

                        if (SatCurveMode == SaturationCurveMode.ONLY_HIGHER_VALUES)
                        {
                            centerColor.Saturation = ControllerColor.Saturation;
                        }
                        else
                        {
                            centerColor.Saturation = ControllerColor.Saturation - Convert.ToInt32(satShift);
                        }
                    }
                    if (!isEven)
                    {
                        PaletteColorList[centerIndex].SetColor(centerColor);
                    }
                    

                    //setting colors
                    for (int i = 0; i < PaletteColorList.Count; i++)
                    {
                        if ((i != centerIndex || isEven) && i != DependentColorIndex)
                        {
                            distanceToCenter = Math.Abs(i - centerIndex); 
                            if (isEven)
                            {
                                if (i < centerIndex)
                                {
                                    distanceToCenter -= 0.5;
                                }
                                else
                                {
                                    distanceToCenter += 0.5;
                                }
                            }
                            double hueShift = HueShiftSlider.Value * HueShiftExponentSlider.Value * Math.Pow(distanceToCenter, HueShiftExponentSlider.Value);
                            double satShift = SaturationShiftSlider.Value * SaturationShiftExponentSlider.Value * Math.Pow(distanceToCenter, SaturationShiftExponentSlider.Value);
                            HSVColor col = new();
                            if (i < centerIndex)
                            {
                                col.Hue = centerColor.Hue - Convert.ToInt32(hueShift);
                                if (SatCurveMode == SaturationCurveMode.ONLY_HIGHER_VALUES)
                                {
                                    col.Saturation = centerColor.Saturation;
                                }
                                else
                                {
                                    col.Saturation = centerColor.Saturation + Convert.ToInt32(satShift);
                                }
                                col.Brightness = Convert.ToInt32(centerColor.Brightness - valueStepSize * distanceToCenter);
                            }
                            else
                            {
                             
                                col.Hue = centerColor.Hue + Convert.ToInt32(hueShift);

                                if (SatCurveMode == SaturationCurveMode.ONLY_LOWER_VALUES)
                                {
                                    col.Saturation = centerColor.Saturation;
                                }
                                else if (SatCurveMode == SaturationCurveMode.LINEAR)
                                {
                                    col.Saturation = centerColor.Saturation - Convert.ToInt32(satShift);
                                }
                                else
                                {
                                    col.Saturation = centerColor.Saturation + Convert.ToInt32(satShift);
                                }

                                col.Brightness = Convert.ToInt32(centerColor.Brightness + valueStepSize * distanceToCenter);
                            }
                            PaletteColorList[i].SetColor(col);
                        }
                    }

                    //setting inactive sliders
                    BaseHueSlider.Value = centerColor.Hue;
                    BaseSaturationSlider.Value = centerColor.Saturation;
                }
            }
        }


        private void UpdateColors()
        {
            if (IsControlling)
            {
                ControllerColorChanged?.Invoke(this, EventArgs.Empty);
            }
            ColorsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void ValueRangeSlider_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Double? higherValue = TryFindResource(ID_PALETTEEDITOR_DEFAULTVALUE_VALUEMAX) as Double?;
            if (higherValue != null)
            {
                ValueRangeSlider.HigherValue = higherValue.Value;
            }

            Double? lowerValue = TryFindResource(ID_PALETTEEDITOR_DEFAULTVALUE_VALUEMIN) as Double?;
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

            foreach (PaletteColor paletteColor in PaletteColorList)
            {
                paletteColor.SetMinimized(IsMinimized);
            }
        }

        private void SatStyleButton_Click(object sender, RoutedEventArgs e)
        {

            if (sender == SatStyleButton1)
            {
                if (SatStyleButton1.IsChecked.GetValueOrDefault())
                {
                    SatCurveMode = PaletteEditor.SaturationCurveMode.ONLY_HIGHER_VALUES;
                }
                else
                {
                    SatStyleButton1.Visibility = Visibility.Hidden;
                    SatStyleButton2.Visibility = Visibility.Visible;
                    SatCurveMode = PaletteEditor.SaturationCurveMode.LINEAR;
                }
            }
            else
            {
                if (SatStyleButton2.IsChecked.GetValueOrDefault())
                {
                    SatCurveMode = PaletteEditor.SaturationCurveMode.ONLY_LOWER_VALUES;
                }
                else
                {
                    SatStyleButton2.Visibility = Visibility.Hidden;
                    SatStyleButton1.Visibility = Visibility.Visible;
                    SatCurveMode = PaletteEditor.SaturationCurveMode.HIGHER_AND_LOWER_VALUES;
                }
            }

            CalculateColors();
            UpdateColors();
        }

        private void OptimizationButton_Click(object sender, RoutedEventArgs e)
        {
            int n = PaletteColorList.Count * 3;
            double[] startPars = new double[n];
            double[] xl = new double[n];
            double[] xu = new double[n];
            double hueShiftMin, hueShiftMax, satShiftMin, satShiftMax, valShiftMin, valShiftMax;
            double? hueShiftMinNullable = TryFindResource("PaletteColor_HueShiftMin") as double?;
            double? hueShiftMaxNullable = TryFindResource("PaletteColor_HueShiftMax") as double?;
            double? satShiftMinNullable = TryFindResource("PaletteColor_SatShiftMin") as double?;
            double? satShiftMaxNullable = TryFindResource("PaletteColor_SatShiftMax") as double?;
            double? valShiftMinNullable = TryFindResource("PaletteColor_ValShiftMin") as double?;
            double? valShiftMaxNullable = TryFindResource("PaletteColor_ValShiftMax") as double?;

            if (hueShiftMinNullable.HasValue)
            {
                hueShiftMin = hueShiftMinNullable.Value;
            }
            else
            {
                hueShiftMin = -Double.MaxValue;
            }
            if (hueShiftMaxNullable.HasValue)
            {
                hueShiftMax = hueShiftMaxNullable.Value;
            }
            else
            {
                hueShiftMax = Double.MaxValue;
            }
            if (satShiftMinNullable.HasValue)
            {
                satShiftMin = satShiftMinNullable.Value;
            }
            else
            {
                satShiftMin = -Double.MaxValue;
            }
            if (satShiftMaxNullable.HasValue)
            {
                satShiftMax = satShiftMaxNullable.Value;
            }
            else
            {
                satShiftMax = Double.MaxValue;
            }
            if (valShiftMinNullable.HasValue)
            {
                valShiftMin = valShiftMinNullable.Value;
            }
            else
            {
                valShiftMin= -Double.MaxValue;
            }
            if (valShiftMaxNullable.HasValue)
            {
                valShiftMax = valShiftMaxNullable.Value;
            }
            else
            {
                valShiftMax = Double.MaxValue;
            }



            for (int i = 0; i < n; i++) 
            {
                startPars[i] = 0;
                
                if (i % 3 == 0) //hue constraint
                {
                    xl[i] = hueShiftMin;
                    xu[i] = hueShiftMax;
                }
                else if (i % 3 == 1) //sat constraint
                {
                    xl[i] = satShiftMin;
                    xu[i] = satShiftMax;
                }
                else //val constraint
                {
                    xl[i] = valShiftMin;
                    xu[i] = valShiftMax;
                }
            }

            Bobyqa bobyqa = new(n, OptimizationFunct, xl, xu)
            {
                InterpolationConditions = 8 * n + 1,
                TrustRegionRadiusStart = 15,
                TrustRegionRadiusEnd = 0.05
            };
            OptimizationSummary summary = bobyqa.FindMinimum(startPars);
            for (int i = 0; i < PaletteColorList.Count; i++)
            {
                PaletteColorList[i].SetShift(Convert.ToSByte(summary.X[i * 3 + 0]), Convert.ToSByte(summary.X[i * 3 + 1]), Convert.ToSByte(summary.X[i * 3 + 2]));
            }
            UpdateColors();
        }

        double OptimizationFunct(int n, double[] x)
        {
            List<HSVColor> hSVColors = new();

            for (int i = 0; i < PaletteColorList.Count; i++) 
            { 
                PaletteColor pc = PaletteColorList[i];
                HSVColor hSVColor = new(pc.OriginalColor.Hue + Convert.ToInt32(x[i * 3 + 0]), pc.OriginalColor.Saturation + Convert.ToInt32(x[i * 3 + 1]), pc.OriginalColor.Brightness + Convert.ToInt32(x[i * 3 + 2]));                
                hSVColors.Add(hSVColor);
            }


            List<double> distances = new();
            for (int i = 0; i < hSVColors.Count - 1; i++)
            {
                HSVColor h1 = hSVColors[i];
                HSVColor h2 = hSVColors[i + 1];
                distances.Add(ColorNames.GetDeltaE(h1.GetRGBColor().R, h1.GetRGBColor().G, h1.GetRGBColor().B, h2.GetRGBColor().R, h2.GetRGBColor().G, h2.GetRGBColor().B));
            }

            return Math.Abs(distances.Max() - distances.Min());
        }

        public void ColorNameChange()
        {
            foreach (PaletteColor palette in PaletteColorList)
            {
                palette.UpdateColorInfo();
            }
        }
    }
}
