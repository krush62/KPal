﻿/*
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace KPal
{
    public partial class PaletteColor : UserControl
    {
        private const string ID_PALETTECOLOR_DEFAULTVALUE_HUESHIFT = "PaletteColor_DefaultValue_HueShift";
        private const string ID_PALETTECOLOR_DEFAULTVALUE_SATSHIFT = "PaletteColor_DefaultValue_SatShift";
        private const string ID_PALETTECOLOR_DEFAULTVALUE_VALSHIFT = "PaletteColor_DefaultValue_ValShift";

        private const string HEX_FORMAT = "X2";
        private const int HEX_COLOR_LENGTH = 6;

        private const int COPY_FLASH_LENGTH_MS = 200;

        public sbyte HueShift { get; private set; }
        public sbyte SatShift { get; private set; }
        public sbyte ValShift { get; private set; }
        private bool IsControlled;

        private readonly DispatcherTimer CopyBackgroundTimer;

        public class LinkCreatedEventArgs : EventArgs
        {
            public ColorLink ColorLink { get; private set; }
            public LinkCreatedEventArgs(ColorLink colorLink)
            {
                ColorLink = colorLink;
            }
        }

        public class LinkDeletedEventArgs : EventArgs
        {
            public ColorLink.ColorLinkPartner ColorLinkPartner { get; private set; }
            public LinkDeletedEventArgs(ColorLink.ColorLinkPartner colorLinkPartner)
            {
                ColorLinkPartner = colorLinkPartner;
            }
        }

        public HSVColor OriginalColor { get; private set; }
        public HSVColor HSVColor { get; private set; }
        public string ColorName { get; private set; }
        private readonly PaletteEditor ParentPalette;
        public event EventHandler? LinkCreated;
        public event EventHandler? LinkDeleted;
        public event EventHandler? ColorHoverIn;
        public event EventHandler? ColorHoverOut;
        public event EventHandler? AdjustmentChanged;

        public PaletteColor(PaletteEditor parent)
        {
            InitializeComponent();
            OriginalColor = new HSVColor();
            HSVColor = new HSVColor();
            ColorName = Properties.Resources.Color_Unknown;
            ParentPalette = parent;
            ControlGrid.Visibility = Visibility.Hidden;
            HueShift = ValShift = SatShift = 0;
            CopyBackgroundTimer = new DispatcherTimer();
            CopyBackgroundTimer.Tick += CopyBackgroundTimer_Tick;
            CopyBackgroundTimer.Interval = new TimeSpan(0, 0, 0, 0, COPY_FLASH_LENGTH_MS);
        }

        public void SetColor(HSVColor hsvColor)
        {
            OriginalColor = hsvColor;
            AddShift();
            UpdateColorInfo();
        }

        public void SetShift(sbyte hShift, sbyte sShift, sbyte vShift)
        {
            HueShiftSlider.Value = hShift;
            SatShiftSlider.Value = sShift;
            ValShiftSlider.Value = vShift;
        }

        public void ResetShift()
        {
            sbyte hShift = Convert.ToSByte(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_HUESHIFT) as double?);
            sbyte sShift = Convert.ToSByte(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_SATSHIFT) as double?);
            sbyte vShift = Convert.ToSByte(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_VALSHIFT) as double?);
            SetShift(hShift, sShift, vShift);
        }

        private void AddShift()
        {
            HSVColor.Hue = OriginalColor.Hue + HueShift;
            HSVColor.Saturation = OriginalColor.Saturation + SatShift;
            HSVColor.Brightness = OriginalColor.Brightness + ValShift;
        }

        public void UpdateColorInfo()
        {
            Color c = HSVColor.GetRGBColor();
            ColorRectangle.Fill = new SolidColorBrush(c);
            HexValueLabel.Content = string.Format("HEX: #{0}{1}{2}", c.R.ToString(HEX_FORMAT), c.G.ToString(HEX_FORMAT), c.B.ToString(HEX_FORMAT));
            HSVValueLabel.Content = string.Format("{0}° | {1}% | {2}%", Math.Round(HSVColor.Hue, 0), Math.Round(HSVColor.Saturation, 0), Math.Round(HSVColor.Brightness, 0));
            ColorName = ColorNames.Instance.GetColorName(c);
            ColorNameLabelText.Text = ColorName;
        }

        public void SetMinimized(bool minimized)
        {
            if (minimized)
            {
                ColorNameLabelText.Visibility = Visibility.Collapsed;
                LowerDataGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                ColorNameLabelText.Visibility = Visibility.Visible;
                LowerDataGrid.Visibility = Visibility.Visible;
            }
        }

        private void ColorRectangle_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DataObject data = new();
                ColorLink.ColorLinkPartner cPartner = new(ParentPalette, this);
                data.SetData("Object", cPartner);
                data.SetData(DataFormats.StringFormat, ColorName);
                _ = DragDrop.DoDragDrop(ColorRectangle, data, DragDropEffects.Move);
            }
        }

        private bool IsDropTargetValid(DragEventArgs e)
        {
            bool isDropTargetValid = false;
            if (e.Data.GetDataPresent("Object") && //data contains object type
                e.Data.GetData("Object") is ColorLink.ColorLinkPartner linkPartner && // data object is of correct type
                linkPartner.Editor != ParentPalette && //target editor is not source editor
                (!ParentPalette.IsControlled && !ParentPalette.IsControlling)) //target editor is of normal state
            {
                isDropTargetValid = true;
            }
            return isDropTargetValid;

        }

        private void ColorRectangle_DragOver(object sender, DragEventArgs e)
        {
            base.OnDragOver(e);
            if (IsDropTargetValid(e))
            {
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ColorRectangle_Drop(object sender, DragEventArgs e)
        {
            base.OnDrop(e);
            if (IsDropTargetValid(e) && e.Data.GetData("Object") is ColorLink.ColorLinkPartner sourcePartner)
            {
                ColorLink.ColorLinkPartner targetPartner = new(ParentPalette, this);
                ColorLink cLink = new(sourcePartner, targetPartner);
                LinkCreated?.Invoke(this, new LinkCreatedEventArgs(cLink));
            }
            e.Handled = true;
        }

        public void SetControlled(bool controlled)
        {
            IsControlled = controlled;
            if (IsControlled)
            {
                DeleteLinkButton.Visibility = Visibility.Visible;
                HueShift = ValShift = SatShift = 0;
            }
            else
            {
                DeleteLinkButton.Visibility = Visibility.Hidden;
            }
        }


        private void DeleteLinkButton_Click(object sender, RoutedEventArgs e)
        {
            ColorHoverOut?.Invoke(this, EventArgs.Empty);
            ColorLink.ColorLinkPartner partner = new(ParentPalette, this);
            LinkDeleted?.Invoke(this, new LinkDeletedEventArgs(partner));
            ControlGrid.Visibility = Visibility.Visible;

        }

        private void ColorRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            ColorHoverIn?.Invoke(this, EventArgs.Empty);
            if (!ParentPalette.IsMinimized && !IsControlled)
            {
                ControlGrid.Visibility = Visibility.Visible;
            }
            //TODO MAGIC NUMBER!!
            if (HSVColor.Brightness < 15)
            {
                OverlayRectangle.Visibility = Visibility.Visible;
            }
        }

        private void ColorRectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            ColorHoverOut?.Invoke(this, EventArgs.Empty);
            ControlGrid.Visibility = Visibility.Hidden;
            OverlayRectangle.Visibility = Visibility.Collapsed;
        }

        public void SetHighlightBorder(bool visible)
        {
            if (visible)
            {
                HighlightBorder.Visibility = Visibility.Visible;
            }
            else
            {
                HighlightBorder.Visibility = Visibility.Hidden;
            }
        }

        private void HueShiftSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            HueShift = Convert.ToSByte(e.NewValue);
            AdjustmentSliderChanged();
        }

        private void SatShiftSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SatShift = Convert.ToSByte(e.NewValue);
            AdjustmentSliderChanged();
        }

        private void ValShiftSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ValShift = Convert.ToSByte(e.NewValue);
            AdjustmentSliderChanged();
            if (HSVColor.Brightness < 15)
            {
                OverlayRectangle.Visibility = Visibility.Visible;
            }
            else
            {
                OverlayRectangle.Visibility = Visibility.Collapsed;
            }
        }

        private void AdjustmentSliderChanged()
        {
            if (HueShift != TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_HUESHIFT) as double? ||
                SatShift != TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_SATSHIFT) as double? ||
                ValShift != TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_VALSHIFT) as double?)
            {
                EditSymbol.Visibility = Visibility.Visible;
            }
            else
            {
                EditSymbol.Visibility = Visibility.Hidden;
            }

            AddShift();
            UpdateColorInfo();
            AdjustmentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EditSymbol_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HueShiftSlider.Value = Convert.ToDouble(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_HUESHIFT) as double?);
            SatShiftSlider.Value = Convert.ToDouble(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_SATSHIFT) as double?);
            ValShiftSlider.Value = Convert.ToDouble(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_VALSHIFT) as double?);
        }

        private void Label_Hue_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            HueShiftSlider.Value = Convert.ToDouble(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_HUESHIFT) as double?);
        }

        private void Label_Sat_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SatShiftSlider.Value = Convert.ToDouble(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_SATSHIFT) as double?);
        }

        private void Label_Val_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ValShiftSlider.Value = Convert.ToDouble(TryFindResource(ID_PALETTECOLOR_DEFAULTVALUE_VALSHIFT) as double?);
        }

        private void HexValueLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender == HexValueLabel)
            {
                string? hexString = HexValueLabel.Content.ToString();
                if (hexString != null && hexString.Length >= HEX_COLOR_LENGTH) 
                {
                    Clipboard.SetText(hexString.Substring(hexString.Length - HEX_COLOR_LENGTH, HEX_COLOR_LENGTH));
                    HexValueLabel.Background = new SolidColorBrush(Colors.White);
                    CopyBackgroundTimer.Start();
                }
            }
        }

        private void CopyBackgroundTimer_Tick(object? sender, EventArgs e)
        {
            HexValueLabel.Background = HSVValueLabel.Background;
            CopyBackgroundTimer.Stop();
        }
    }
}
