using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KPal
{
    public partial class PaletteColor : UserControl
    {
        public sbyte HueShift { get; private set; }
        public sbyte SatShift { get; private set; }
        public sbyte ValShift { get; private set; }
        private bool IsControlled;
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
        private string ColorName;
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
            ColorName = "UNKNOWN";
            ParentPalette = parent;
            ControlGrid.Visibility = Visibility.Hidden;
            HueShift = ValShift = SatShift = 0;
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

        private void AddShift()
        {
            HSVColor.Hue = OriginalColor.Hue + Convert.ToInt32(HueShift);
            HSVColor.Saturation = OriginalColor.Saturation + Convert.ToInt32(SatShift);
            HSVColor.Brightness = OriginalColor.Brightness + Convert.ToInt32(ValShift);
        }

        private void UpdateColorInfo()
        {
            Color c = HSVColor.GetRGBColor();
            ColorRectangle.Fill = new SolidColorBrush(c);
            HexValueLabel.Content = "HEX: #" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
            HSVValueLabel.Content = HSVColor.Hue.ToString() + "° | " + HSVColor.Saturation.ToString() + "% | " + HSVColor.Brightness.ToString() + "%";
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
            if (HueShift != TryFindResource("PaletteColor_DefaultValue_HueShift") as double? ||
                SatShift != TryFindResource("PaletteColor_DefaultValue_SatShift") as double? ||
                ValShift != TryFindResource("PaletteColor_DefaultValue_ValShift") as double?)
            {
                EditSymbol.Visibility = Visibility.Visible;
            }
            else
            {
                EditSymbol.Visibility= Visibility.Hidden;
            }

            AddShift();
            UpdateColorInfo();
            AdjustmentChanged?.Invoke(this, EventArgs.Empty);
        }

        private void EditSymbol_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HueShiftSlider.Value = Convert.ToDouble(TryFindResource("PaletteColor_DefaultValue_HueShift") as double?);
            SatShiftSlider.Value =  Convert.ToDouble(TryFindResource("PaletteColor_DefaultValue_SatShift") as double?);
            ValShiftSlider.Value = Convert.ToDouble(TryFindResource("PaletteColor_DefaultValue_ValShift") as double?);
        }

        private void Label_Hue_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            HueShiftSlider.Value = Convert.ToDouble(TryFindResource("PaletteColor_DefaultValue_HueShift") as double?);
        }

        private void Label_Sat_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SatShiftSlider.Value = Convert.ToDouble(TryFindResource("PaletteColor_DefaultValue_SatShift") as double?);
        }

        private void Label_Val_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ValShiftSlider.Value = Convert.ToDouble(TryFindResource("PaletteColor_DefaultValue_ValShift") as double?);
        }
    }
}
