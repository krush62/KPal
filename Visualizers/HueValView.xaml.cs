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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KPal
{
    public partial class HueValView : Visualizer
    {
        private const double SCALING_FACTOR = 120;
        private const double CENTER_X = 0.5 * SCALING_FACTOR;
        private const double LEFT_X = 0.0 * SCALING_FACTOR;
        private const double RIGHT_X = 1.0 * SCALING_FACTOR;
        private const double TOP_Y = 0.0 * SCALING_FACTOR;
        private const double BOTTOM_Y = 1.0 * SCALING_FACTOR;
        private const double VIEW_RATIO = 0.4;
        private const double STROKE_THICKNESS = 2.0;
        private const double DASH_INTERVAL_OUTLINE = STROKE_THICKNESS * 2.0;
        private const double DASH_INTERVAL_VERTICAL = STROKE_THICKNESS * 1.0;
        private const double VERTICAL_DASH_OPACITY = 0.25;
        private const double OUTLINE_DASH_OPACITY = 0.5;
        private readonly DoubleCollection DASH_ARRAY_OUTLILNE = new() { DASH_INTERVAL_OUTLINE };
        private readonly DoubleCollection DASH_ARRAY_VERTICAL = new() { DASH_INTERVAL_VERTICAL };
        private readonly Brush OUTLINE_BRUSH = new SolidColorBrush(Color.FromArgb(Convert.ToByte(Convert.ToDouble(byte.MaxValue) * OUTLINE_DASH_OPACITY), 0, 0, 0));

        public HueValView()
        {
            InitializeComponent();
            Type = VisualizerType.HueVal;
            Selector.SelectedItem = Type;
            _ = MainGrid.Children.Add(Selector);
            Canvas1.Width = SCALING_FACTOR;
            Canvas1.Height = SCALING_FACTOR;
            Canvas2.Width = SCALING_FACTOR;
            Canvas2.Height = SCALING_FACTOR;
            Canvas3.Width = SCALING_FACTOR;
            Canvas3.Height = SCALING_FACTOR;
        }

        public override void Update(List<PaletteEditor> editors, List<ColorLink> links)
        {
            if (editors.Count > 0)
            {
                DrawCube(editors, Canvas2);
                DrawCylinder(editors, Canvas1);
                DrawHueValDiagram(editors, Canvas3);
            }
        }

        private static StreamGeometry CreateHalfCircle(double height, SweepDirection direction, double scalingFactor, double ellipseRatio)
        {
            double arcSizeX = scalingFactor / (1.0 * 10.0);
            double arcSizeY = scalingFactor / (ellipseRatio * 10.0);
            StreamGeometry g = new();
            using (StreamGeometryContext gc = g.Open())
            {
                gc.BeginFigure(
                    startPoint: new Point(0, height),
                    isFilled: false,
                    isClosed: false);

                gc.ArcTo(
                    point: new Point(scalingFactor, height),
                    size: new Size(arcSizeX, arcSizeY),
                    rotationAngle: 90d,
                    isLargeArc: false,
                    sweepDirection: direction,
                    isStroked: true,
                    isSmoothJoin: false);
            }
            return g;
        }

        private void DrawHueValDiagram(List<PaletteEditor> editors, Canvas canvas)
        {
            canvas.Children.Clear();

            //TODO REMOVE ANY MAGIC NUMBER (harmonize with other drawings)
            const double POINT_SIZE_MAX = SCALING_FACTOR / 10.0;
            const double POINT_SIZE_MIN = POINT_SIZE_MAX * 0.2;
            const double MARGIN = SCALING_FACTOR / 20.0; ;

            List<HSVColor> hSVColors = GetUniqueColorsFromPalettes(editors);
            hSVColors = hSVColors.OrderByDescending(c => c.Saturation).ToList();
            foreach (HSVColor color in hSVColors)
            {
                double hueNorm = Convert.ToDouble(color.Hue) / Convert.ToDouble(HSVColor.MAX_VALUE_DEGREES);
                double satNorm = Convert.ToDouble(color.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                double valNorm = Convert.ToDouble(color.Brightness) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                Ellipse e = new()
                {
                    Width = POINT_SIZE_MIN + (satNorm * POINT_SIZE_MAX),
                    Height = POINT_SIZE_MIN + (satNorm * POINT_SIZE_MAX)
                };
                Canvas.SetLeft(e, MARGIN + hueNorm * (SCALING_FACTOR - 2.0 * MARGIN));
                Canvas.SetTop(e, (SCALING_FACTOR - MARGIN) - valNorm * (SCALING_FACTOR - (2.0 * MARGIN)));
                e.Fill = new SolidColorBrush(color.GetRGBColor());
                _ = canvas.Children.Add(e);
            }

            Line lineHor = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = LEFT_X,
                X2 = RIGHT_X,
                Y1 = BOTTOM_Y,
                Y2 = BOTTOM_Y,
            };
            _ = canvas.Children.Add(lineHor);

            Line lineVer = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = LEFT_X,
                X2 = LEFT_X,
                Y1 = TOP_Y,
                Y2 = BOTTOM_Y,
            };
            _ = canvas.Children.Add(lineVer);

        }


        private void DrawCylinder(List<PaletteEditor> editors, Canvas canvas)
        {
            canvas.Children.Clear();

            //TODO REMOVE ANY MAGIC NUMBER (harmonize with other drawings)
            const double POINT_MAX_SIZE = SCALING_FACTOR / 30.0;
            const double POINT_MIN_SIZE = POINT_MAX_SIZE * 0.5;

            //drawing top part of the lower ellipse which is behind the points
            Path lowerTopArc = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                StrokeDashArray = DASH_ARRAY_OUTLILNE,
                Data = CreateHalfCircle(SCALING_FACTOR - (SCALING_FACTOR * VIEW_RATIO) / 2, SweepDirection.Clockwise, SCALING_FACTOR, VIEW_RATIO)
            };
            _ = canvas.Children.Add(lowerTopArc);

            //getting color list and sort list by distance (determined by the cosinus of the hue
            List<HSVColor> hSVColors = GetUniqueColorsFromPalettes(editors);
            hSVColors = hSVColors.OrderBy(c => Math.Cos(Deg2Rad(c.Hue)) * (Convert.ToDouble(c.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT))).ToList();
            foreach (HSVColor color in hSVColors)
            {
                //normalizing hsv (0...1)
                double hueNorm = Convert.ToDouble(color.Hue) / Convert.ToDouble(HSVColor.MAX_VALUE_DEGREES);
                double satNorm = Convert.ToDouble(color.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                double valNorm = Convert.ToDouble(color.Brightness) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                //calculating distance (see above)
                double distance = Math.Cos(Deg2Rad(color.Hue)) * satNorm;
                double size = (POINT_MIN_SIZE + (distance + 1.0)) * POINT_MAX_SIZE;
                double pixelDistanceToBottom = (SCALING_FACTOR - (VIEW_RATIO * SCALING_FACTOR)) * valNorm;
                double xPos = CENTER_X + Math.Sin(Deg2Rad(color.Hue)) * satNorm * SCALING_FACTOR / 2.0;
                double yPos = (SCALING_FACTOR - (SCALING_FACTOR * VIEW_RATIO) / 2.0) +
                    distance * (SCALING_FACTOR * (VIEW_RATIO / 2.0)) -
                    pixelDistanceToBottom;

                Ellipse e = new()
                {
                    Width = size,
                    Height = size
                };
                Canvas.SetLeft(e, xPos - size / 2.0);
                Canvas.SetTop(e, yPos - size / 2.0);
                Color rgbCol = color.GetRGBColor();
                e.Fill = new SolidColorBrush(rgbCol);
                _ = canvas.Children.Add(e);

                //drawing marker line
                Line line = new()
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(Convert.ToByte(Convert.ToDouble(Byte.MaxValue) * VERTICAL_DASH_OPACITY), rgbCol.R, rgbCol.G, rgbCol.B)),
                    StrokeThickness = STROKE_THICKNESS,
                    X1 = xPos,
                    X2 = xPos,
                    Y1 = yPos,
                    Y2 = yPos + pixelDistanceToBottom,
                    StrokeDashArray = DASH_ARRAY_VERTICAL
                };
                _ = canvas.Children.Add(line);
            }

            //Drawing remaining ellipse arcs
            Path upperTopArc = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                Data = CreateHalfCircle((SCALING_FACTOR * VIEW_RATIO) / 2.0, SweepDirection.Clockwise, SCALING_FACTOR, VIEW_RATIO)
            };
            _ = canvas.Children.Add(upperTopArc);

            Path upperBottomArc = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                Data = CreateHalfCircle((SCALING_FACTOR * VIEW_RATIO) / 2.0, SweepDirection.Counterclockwise, SCALING_FACTOR, VIEW_RATIO)
            };
            _ = canvas.Children.Add(upperBottomArc);

            Path lowerBottomArc = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                Data = CreateHalfCircle(SCALING_FACTOR - (SCALING_FACTOR * VIEW_RATIO) / 2.0, SweepDirection.Counterclockwise, SCALING_FACTOR, VIEW_RATIO)
            };
            _ = canvas.Children.Add(lowerBottomArc);

            //drawing vertival lines
            Line l1 = new()
            {
                X1 = LEFT_X,
                X2 = LEFT_X,
                Y1 = (SCALING_FACTOR * VIEW_RATIO) / 2,
                Y2 = SCALING_FACTOR - (SCALING_FACTOR * VIEW_RATIO) / 2.0,
                StrokeThickness = STROKE_THICKNESS,
                Stroke = OUTLINE_BRUSH
            };
            _ = canvas.Children.Add(l1);

            Line l2 = new()
            {
                X1 = RIGHT_X,
                X2 = RIGHT_X,
                Y1 = (SCALING_FACTOR * VIEW_RATIO) / 2.0,
                Y2 = SCALING_FACTOR - (SCALING_FACTOR * VIEW_RATIO) / 2.0,
                StrokeThickness = STROKE_THICKNESS,
                Stroke = OUTLINE_BRUSH
            };
            _ = canvas.Children.Add(l2);
        }

        private void DrawCube(List<PaletteEditor> editors, Canvas canvas)
        {
            canvas.Children.Clear();
            const double UPPER_CORNER_Y = (VIEW_RATIO / 2.0) * SCALING_FACTOR;
            const double UPPER_CENTER_CORNER_Y = 2 * UPPER_CORNER_Y;
            const double LOWER_CENTER_CORNER_Y = SCALING_FACTOR - UPPER_CENTER_CORNER_Y;
            const double LOWER_CORNER_Y = SCALING_FACTOR - UPPER_CORNER_Y;
            //TODO REMOVE ANY MAGIC NUMBER (harmonize with other drawings)
            const double POINT_MAX_SIZE = SCALING_FACTOR / 10.0;
            const double POINT_MIN_SIZE = POINT_MAX_SIZE * 0.5;

            //drawing lines which are behind the color dots
            Line line1 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CENTER_X,
                X2 = CENTER_X,
                Y1 = TOP_Y,
                Y2 = UPPER_CENTER_CORNER_Y,
                StrokeDashArray = DASH_ARRAY_OUTLILNE
            };
            _ = canvas.Children.Add(line1);

            Line line2 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CENTER_X,
                X2 = RIGHT_X,
                Y1 = LOWER_CENTER_CORNER_Y,
                Y2 = LOWER_CORNER_Y,
                StrokeDashArray = DASH_ARRAY_OUTLILNE
            };
            _ = canvas.Children.Add(line2);

            Line line3 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CENTER_X,
                X2 = LEFT_X,
                Y1 = LOWER_CENTER_CORNER_Y,
                Y2 = LOWER_CORNER_Y,
                StrokeDashArray = DASH_ARRAY_OUTLILNE
            };
            _ = canvas.Children.Add(line3);

            //getting unique color list and sort by distance (determined by saturation + hue)
            List<HSVColor> hSVColors = GetUniqueColorsFromPalettes(editors);
            hSVColors = hSVColors.OrderByDescending(c => Convert.ToDouble(c.Hue) / Convert.ToDouble(HSVColor.MAX_VALUE_DEGREES) + Convert.ToDouble(c.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT)).ToList();
            foreach (HSVColor color in hSVColors)
            {
                //normalizing hsv (0...1)
                double hueNorm = Convert.ToDouble(color.Hue) / Convert.ToDouble(HSVColor.MAX_VALUE_DEGREES);
                double satNorm = Convert.ToDouble(color.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                double valNorm = Convert.ToDouble(color.Brightness) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                //calculating distance (see above)
                double distance = (Convert.ToDouble(color.Hue) / Convert.ToDouble(HSVColor.MAX_VALUE_DEGREES) + Convert.ToDouble(color.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT)) / 2.0;
                double size = POINT_MAX_SIZE - (distance * (POINT_MAX_SIZE - POINT_MIN_SIZE));
                double xPos = CENTER_X + (hueNorm * CENTER_X) - (satNorm * CENTER_X);
                double yPos = BOTTOM_Y - (hueNorm * (BOTTOM_Y - LOWER_CORNER_Y)) - (satNorm * (BOTTOM_Y - LOWER_CORNER_Y)) - (valNorm * (BOTTOM_Y - UPPER_CENTER_CORNER_Y));
                Ellipse e = new()
                {
                    Width = size,
                    Height = size
                };
                Canvas.SetLeft(e, xPos - size / 2.0);
                Canvas.SetTop(e, yPos - size / 2.0);
                Color rgbCol = color.GetRGBColor();
                e.Fill = new SolidColorBrush(rgbCol);
                _ = canvas.Children.Add(e);
                Line line = new()
                {
                    Stroke = new SolidColorBrush(Color.FromArgb(Convert.ToByte(Convert.ToDouble(Byte.MaxValue) * VERTICAL_DASH_OPACITY), rgbCol.R, rgbCol.G, rgbCol.B)),
                    StrokeThickness = STROKE_THICKNESS,
                    X1 = xPos,
                    X2 = xPos,
                    Y1 = yPos,
                    Y2 = yPos + (valNorm * (BOTTOM_Y - UPPER_CENTER_CORNER_Y)),
                    StrokeDashArray = DASH_ARRAY_VERTICAL
                };
                _ = canvas.Children.Add(line);
            }

            //drawing remaining lines
            Line line4 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = LEFT_X,
                X2 = CENTER_X,
                Y1 = UPPER_CORNER_Y,
                Y2 = UPPER_CENTER_CORNER_Y
            };
            _ = canvas.Children.Add(line4);

            Line line5 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CENTER_X,
                X2 = RIGHT_X,
                Y1 = UPPER_CENTER_CORNER_Y,
                Y2 = UPPER_CORNER_Y
            };
            _ = canvas.Children.Add(line5);

            Line line6 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CENTER_X,
                X2 = CENTER_X,
                Y1 = UPPER_CENTER_CORNER_Y,
                Y2 = BOTTOM_Y
            };
            _ = canvas.Children.Add(line6);

            Polygon p = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                Points = new()
            {
                new Point(CENTER_X, TOP_Y),
                new Point(RIGHT_X, UPPER_CORNER_Y),
                new Point(RIGHT_X, LOWER_CORNER_Y),
                new Point(CENTER_X, BOTTOM_Y),
                new Point(LEFT_X, LOWER_CORNER_Y),
                new Point(LEFT_X, UPPER_CORNER_Y)
            }
            };
            _ = canvas.Children.Add(p);
        }

        public static double Deg2Rad(double angle)
        {
            return (Math.PI / 180) * angle;
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
