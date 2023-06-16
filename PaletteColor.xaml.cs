using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KPal
{
    public partial class PaletteColor : UserControl
    {
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


        public HSVColor ColorHSV { get; private set; }
        private string ColorName;
        private readonly PaletteEditor ParentPalette;
        public event EventHandler? LinkCreated;
        public event EventHandler? LinkDeleted;
        public event EventHandler? ColorHoverIn;
        public event EventHandler? ColorHoverOut;

        public PaletteColor(PaletteEditor parent)
        {
            InitializeComponent();
            ColorHSV = new HSVColor();
            ColorName = "UNKNOWN";
            ParentPalette = parent;
        }

        public void SetColor(HSVColor hsvColor)
        {
            ColorHSV = hsvColor;
            UpdateColorInfo();
        }

        private void UpdateColorInfo()
        {
            Color c = ColorHSV.GetRGBColor();
            ColorRectangle.Fill = new SolidColorBrush(c);
            HexValueLabel.Content = "HEX: #" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
            HSVValueLabel.Content = ColorHSV.Hue.ToString() + "° | " + ColorHSV.Saturation.ToString() + "% | " + ColorHSV.Brightness.ToString() + "%";
            //Code for displaying the delta offset to the named color:
            //float delta;
            //(colorName, delta) = ColorNames.Instance.GetColorName(c);
            //ColorNameLabelText.Text = colorName + " [" + delta.ToString("N1") + "]";
            (ColorName, _) = ColorNames.Instance.GetColorName(c);
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


        private void DeleteLinkButton_Click(object sender, RoutedEventArgs e)
        {
            ColorLink.ColorLinkPartner partner = new(ParentPalette, this);
            LinkDeleted?.Invoke(this, new LinkDeletedEventArgs(partner));
        }

        private void ColorRectangle_MouseEnter(object sender, MouseEventArgs e)
        {
            ColorHoverIn?.Invoke(this, EventArgs.Empty);
        }

        private void ColorRectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            ColorHoverOut?.Invoke(this, EventArgs.Empty);
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
    }
}
