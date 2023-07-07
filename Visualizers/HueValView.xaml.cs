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
        private double ScalingFactor;
        private double CenterX;
        private double LeftX;
        private double RightX;
        private double TopY;
        private double BottomY;
        private double PointSizeHueValMax;
        private double PointSizeHueValMin;
        private double PointSizeCylinderMax;
        private double PointSizeCylinderMin;
        private double PointSizeCubeMax;
        private double PointSizeCubeMin;
        private double UpperCornerY;
        private double UpperCenterCornerY;
        private double LowerCenterCornerY;
        private double LowerCornerY;
        private double CanvasMargin;
        private const double MARGIN_FACTOR = 20.0;
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
            UpdateSize(ActualWidth, ActualHeight);
        }

        public override void Update()
        {
            if (Editors != null && Editors.Count > 0)
            {
                DrawCylinder(Editors, Canvas1);
                DrawCube(Editors, Canvas2);
                DrawHueValDiagram(Editors, Canvas3);
            }
        }

        protected override void UpdateSize(double width, double height)
        {
            double size = height < width / 3 ? height : width / 3;
            SetScaling(size - (size / MARGIN_FACTOR));
            Update();
        }

        private void SetScaling(double size)
        {
            ScalingFactor = size;
            Canvas1.Width = ScalingFactor;
            Canvas1.Height = ScalingFactor;
            Canvas2.Width = ScalingFactor;
            Canvas2.Height = ScalingFactor;
            Canvas3.Width = ScalingFactor;
            Canvas3.Height = ScalingFactor;
            CenterX = 0.5 * ScalingFactor;
            LeftX = 0.0 * ScalingFactor;
            RightX = 1.0 * ScalingFactor;
            TopY = 0.0 * ScalingFactor;
            BottomY = 1.0 * ScalingFactor;
            PointSizeHueValMax = ScalingFactor / 10.0;
            PointSizeHueValMin = PointSizeHueValMax * 0.2;
            PointSizeCylinderMax = ScalingFactor / 30.0;
            PointSizeCylinderMin = PointSizeCylinderMax * 0.5;
            PointSizeCubeMax = ScalingFactor / 10.0;
            PointSizeCubeMin = PointSizeCubeMax * 0.5;
            CanvasMargin = ScalingFactor / MARGIN_FACTOR;
            UpperCornerY = (VIEW_RATIO / 2.0) * ScalingFactor;
            UpperCenterCornerY = 2 * UpperCornerY;
            LowerCenterCornerY = ScalingFactor - UpperCenterCornerY;
            LowerCornerY = ScalingFactor - UpperCornerY;
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


            List<HSVColor> hSVColors = GetUniqueColorsFromPalettes(editors);
            hSVColors = hSVColors.OrderByDescending(c => c.Saturation).ToList();
            foreach (HSVColor color in hSVColors)
            {
                double hueNorm = Convert.ToDouble(color.Hue) / Convert.ToDouble(HSVColor.MAX_VALUE_DEGREES);
                double satNorm = Convert.ToDouble(color.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                double valNorm = Convert.ToDouble(color.Brightness) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                Ellipse e = new()
                {
                    Width = PointSizeHueValMin + (satNorm * PointSizeHueValMax),
                    Height = PointSizeHueValMin + (satNorm * PointSizeHueValMax)
                };
                Canvas.SetLeft(e, CanvasMargin + hueNorm * (ScalingFactor - 2.0 * CanvasMargin));
                Canvas.SetTop(e, (ScalingFactor - CanvasMargin) - valNorm * (ScalingFactor - (2.0 * CanvasMargin)));
                e.Fill = new SolidColorBrush(color.GetRGBColor());
                _ = canvas.Children.Add(e);
            }

            Line lineHor = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = LeftX,
                X2 = RightX,
                Y1 = BottomY,
                Y2 = BottomY,
            };
            _ = canvas.Children.Add(lineHor);

            Line lineVer = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = LeftX,
                X2 = LeftX,
                Y1 = TopY,
                Y2 = BottomY,
            };
            _ = canvas.Children.Add(lineVer);

        }


        private void DrawCylinder(List<PaletteEditor> editors, Canvas canvas)
        {
            canvas.Children.Clear();

            //drawing top part of the lower ellipse which is behind the points
            Path lowerTopArc = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                StrokeDashArray = DASH_ARRAY_OUTLILNE,
                Data = CreateHalfCircle(ScalingFactor - (ScalingFactor * VIEW_RATIO) / 2, SweepDirection.Clockwise, ScalingFactor, VIEW_RATIO)
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
                double size = (PointSizeCylinderMin + (distance + 1.0)) * PointSizeCylinderMax;
                double pixelDistanceToBottom = (ScalingFactor - (VIEW_RATIO * ScalingFactor)) * valNorm;
                double xPos = CenterX + Math.Sin(Deg2Rad(color.Hue)) * satNorm * ScalingFactor / 2.0;
                double yPos = (ScalingFactor - (ScalingFactor * VIEW_RATIO) / 2.0) +
                    distance * (ScalingFactor * (VIEW_RATIO / 2.0)) -
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
                Data = CreateHalfCircle((ScalingFactor * VIEW_RATIO) / 2.0, SweepDirection.Clockwise, ScalingFactor, VIEW_RATIO)
            };
            _ = canvas.Children.Add(upperTopArc);

            Path upperBottomArc = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                Data = CreateHalfCircle((ScalingFactor * VIEW_RATIO) / 2.0, SweepDirection.Counterclockwise, ScalingFactor, VIEW_RATIO)
            };
            _ = canvas.Children.Add(upperBottomArc);

            Path lowerBottomArc = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                Data = CreateHalfCircle(ScalingFactor - (ScalingFactor * VIEW_RATIO) / 2.0, SweepDirection.Counterclockwise, ScalingFactor, VIEW_RATIO)
            };
            _ = canvas.Children.Add(lowerBottomArc);

            //drawing vertival lines
            Line l1 = new()
            {
                X1 = LeftX,
                X2 = LeftX,
                Y1 = (ScalingFactor * VIEW_RATIO) / 2,
                Y2 = ScalingFactor - (ScalingFactor * VIEW_RATIO) / 2.0,
                StrokeThickness = STROKE_THICKNESS,
                Stroke = OUTLINE_BRUSH
            };
            _ = canvas.Children.Add(l1);

            Line l2 = new()
            {
                X1 = RightX,
                X2 = RightX,
                Y1 = (ScalingFactor * VIEW_RATIO) / 2.0,
                Y2 = ScalingFactor - (ScalingFactor * VIEW_RATIO) / 2.0,
                StrokeThickness = STROKE_THICKNESS,
                Stroke = OUTLINE_BRUSH
            };
            _ = canvas.Children.Add(l2);
        }

        private void DrawCube(List<PaletteEditor> editors, Canvas canvas)
        {
            canvas.Children.Clear();

            //drawing lines which are behind the color dots
            Line line1 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CenterX,
                X2 = CenterX,
                Y1 = TopY,
                Y2 = UpperCenterCornerY,
                StrokeDashArray = DASH_ARRAY_OUTLILNE
            };
            _ = canvas.Children.Add(line1);

            Line line2 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CenterX,
                X2 = RightX,
                Y1 = LowerCenterCornerY,
                Y2 = LowerCornerY,
                StrokeDashArray = DASH_ARRAY_OUTLILNE
            };
            _ = canvas.Children.Add(line2);

            Line line3 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CenterX,
                X2 = LeftX,
                Y1 = LowerCenterCornerY,
                Y2 = LowerCornerY,
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
                double size = PointSizeCubeMax - (distance * (PointSizeCubeMax - PointSizeCubeMin));
                double xPos = CenterX + (hueNorm * CenterX) - (satNorm * CenterX);
                double yPos = BottomY - (hueNorm * (BottomY - LowerCornerY)) - (satNorm * (BottomY - LowerCornerY)) - (valNorm * (BottomY - UpperCenterCornerY));
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
                    Y2 = yPos + (valNorm * (BottomY - UpperCenterCornerY)),
                    StrokeDashArray = DASH_ARRAY_VERTICAL
                };
                _ = canvas.Children.Add(line);
            }

            //drawing remaining lines
            Line line4 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = LeftX,
                X2 = CenterX,
                Y1 = UpperCornerY,
                Y2 = UpperCenterCornerY
            };
            _ = canvas.Children.Add(line4);

            Line line5 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CenterX,
                X2 = RightX,
                Y1 = UpperCenterCornerY,
                Y2 = UpperCornerY
            };
            _ = canvas.Children.Add(line5);

            Line line6 = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                X1 = CenterX,
                X2 = CenterX,
                Y1 = UpperCenterCornerY,
                Y2 = BottomY
            };
            _ = canvas.Children.Add(line6);

            Polygon p = new()
            {
                Stroke = OUTLINE_BRUSH,
                StrokeThickness = STROKE_THICKNESS,
                Points = new()
            {
                new Point(CenterX, TopY),
                new Point(RightX, UpperCornerY),
                new Point(RightX, LowerCornerY),
                new Point(CenterX, BottomY),
                new Point(LeftX, LowerCornerY),
                new Point(LeftX, UpperCornerY)
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
