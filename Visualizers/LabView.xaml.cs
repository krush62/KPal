using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KPal
{
    public partial class LabView : Visualizer
    {
        private readonly PointCollection LAB_POLYGON_POINTS;
        private const double SCALING_FACTOR = 120;
        public LabView()
        {
            InitializeComponent();
            Type = VisualizerType.LabView;
            Selector.SelectedItem = Type;
            Grid.SetColumn(Selector, 1);
            _ = MainGrid.Children.Add(Selector);

            LAB_POLYGON_POINTS = new();
            for (int i = 0; i < 360; i += 24)
            {
                Color color = new HSVColor(i, HSVColor.MAX_VALUE_VAL_SAT, HSVColor.MAX_VALUE_VAL_SAT).GetRGBColor();
                LabColor lColor = ColorNames.RGB2LAB(color.R, color.G, color.B);
                Point p = new();
                p.X = (lColor.A / LabColor.AB_MAX_VALUE) * (SCALING_FACTOR / 2.0);
                p.Y = - (lColor.B / LabColor.AB_MAX_VALUE) * (SCALING_FACTOR / 2.0);
                LAB_POLYGON_POINTS.Add(p);
            }
            
        }

        public override void Update(List<PaletteEditor> editors, List<ColorLink> links)
        {
            DrawingCanvas.Children.Clear();
            ColorGrid.Children.Clear();
            ColorGrid.ColumnDefinitions.Clear();
            ColorGrid.RowDefinitions.Clear();
            DistanceGrid.Children.Clear();
            DistanceGrid.ColumnDefinitions.Clear();
            DistanceGrid.RowDefinitions.Clear();

            if (editors.Count > 0)
            {
                DrawCircle(editors);
                DrawDistances(editors);
            }            
        }

        private void DrawCircle(List<PaletteEditor> editors)
        {
            double strokeWidth = 1.0;
            SolidColorBrush strokeBrush = new SolidColorBrush(Colors.Black);
            double xOffset = (DrawingCanvas.ActualWidth - SCALING_FACTOR) / 2.0;
            double yOffset = (DrawingCanvas.ActualHeight - SCALING_FACTOR) / 2.0;
            double centerX = xOffset + (SCALING_FACTOR / 2.0);
            double centerY = yOffset + (SCALING_FACTOR / 2.0);
            const double MAX_POINT_SIZE = 16;
            const double MIN_POINT_SIZE = MAX_POINT_SIZE / 4;

            Polygon polygon = new()
            {
                Points = LAB_POLYGON_POINTS,
                Stroke = strokeBrush,
                StrokeThickness = strokeWidth
            };
            Canvas.SetLeft(polygon, centerX);
            Canvas.SetTop(polygon, centerY);
            DrawingCanvas.Children.Add(polygon);

            Line vertLine = new()
            {
                Stroke = strokeBrush,
                StrokeThickness = strokeWidth,
                X1 = centerX,
                X2 = centerX,
                Y1 = yOffset,
                Y2 = yOffset + SCALING_FACTOR
            };
            DrawingCanvas.Children.Add(vertLine);

            Line horLine = new()
            {
                Stroke = strokeBrush,
                StrokeThickness = strokeWidth,
                X1 = xOffset,
                X2 = xOffset + SCALING_FACTOR,
                Y1 = centerY,
                Y2 = centerY
            };
            DrawingCanvas.Children.Add(horLine);

            List<HSVColor> hsvColors = GetUniqueColorsFromPalettes(editors);
            hsvColors = hsvColors.OrderBy(x => x.Brightness).ToList();
            
            foreach (HSVColor color in hsvColors)
            {
                double currentSize = MIN_POINT_SIZE + (MAX_POINT_SIZE - MIN_POINT_SIZE) * (Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT - color.Brightness) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT));
                Color rgbColor = color.GetRGBColor();
                LabColor labColor = ColorNames.RGB2LAB(rgbColor.R, rgbColor.G, rgbColor.B);
                Ellipse ellipse = new()
                {
                    Fill = new SolidColorBrush(rgbColor),
                    Width = currentSize,
                    Height = currentSize
                };
                Canvas.SetLeft(ellipse, (centerX - (currentSize / 2))  + ((labColor.A / LabColor.AB_MAX_VALUE) * (SCALING_FACTOR / 2.0)));
                Canvas.SetTop(ellipse, (centerY - (currentSize / 2)) - ((labColor.B / LabColor.AB_MAX_VALUE) * (SCALING_FACTOR / 2.0)));
                DrawingCanvas.Children.Add(ellipse);
            }
        }

        private void DrawDistances(List<PaletteEditor> editors)
        {
            const double PADDING_WIDTH = 2;
            SolidColorBrush BACKGROUND_BRUSH = new SolidColorBrush(Colors.White);
            const double FONT_SIZE = 15;
            const double LABEL_OPACITY = 0.5;
            int maxLength = editors.Max(x => x.PaletteColorList.Count);
            for (int col = 0; col < maxLength; col++)
            {
                ColumnDefinition colDef = new()
                {
                    Width = new GridLength(1, GridUnitType.Star)
                };
                ColorGrid.ColumnDefinitions.Add(colDef);
            }
            ColumnDefinition cd = new()
            {
                Width = new GridLength(1, GridUnitType.Star)
            };
            DistanceGrid.ColumnDefinitions.Add(cd);

            for (int i = 0; i < maxLength - 1; i++)
            {
                cd = new()
                {
                    Width = new GridLength(2, GridUnitType.Star)
                };
                DistanceGrid.ColumnDefinitions.Add(cd);
            }

            cd = new()
            {
                Width = new GridLength(1, GridUnitType.Star)
            };
            DistanceGrid.ColumnDefinitions.Add(cd);

            for (int row = 0; row < editors.Count; row++)
            {
                int offset = (maxLength - editors[row].PaletteColorList.Count) / 2;
                RowDefinition rd = new()
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                ColorGrid.RowDefinitions.Add(rd);
                for (int i = 0; i < editors[row].PaletteColorList.Count; i++)
                {
                    Rectangle rect = new()
                    {
                        Fill = new SolidColorBrush(editors[row].PaletteColorList[i].HSVColor.GetRGBColor())
                    };
                    Grid.SetColumn(rect, i + offset);
                    Grid.SetRow(rect, row);
                    ColorGrid.Children.Add(rect);
                }
                rd = new()
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                DistanceGrid.RowDefinitions.Add(rd);
                

                List<Color> colors = editors[row].PaletteColorList.Select(x => x.HSVColor.GetRGBColor()).ToList();
                for (int i = 0; i < colors.Count - 1; i++)
                {
                    double distance = ColorNames.GetDeltaE(colors[i].R, colors[i].G, colors[i].B, colors[i+1].R, colors[i+1].G, colors[i+1].B);
                    Label l = new()
                    {
                        Content = distance.ToString("N1"),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Padding = new Thickness(PADDING_WIDTH),
                        Background = BACKGROUND_BRUSH,
                        FontSize = FONT_SIZE,
                        Opacity = LABEL_OPACITY,
                    };
                    Grid.SetRow(l, row);
                    Grid.SetColumn(l, i + offset + 1);
                    DistanceGrid.Children.Add(l);
                }
            }
        }

        private void MainGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Selector.Visibility = Visibility.Visible;
        }

        private void MainGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Selector.Visibility = Visibility.Hidden;
        }
    }
}
