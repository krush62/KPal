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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KPal
{
    public partial class VoronoiView : Visualizer
    {
        private enum VoronoiMode
        {
            HUE_VAL,
            HUE_SAT,
            SAT_VAL
        }

        private const double MARGIN_FACTOR = 0.9;
        private const int NUM_ELEMENTS = 3;
        private double ScalingFactor;
        private double LeftX;
        private double RightX;
        private double TopY;
        private double BottomY;
        private double StepSize;

        public VoronoiView()
        {
            InitializeComponent();
            Type = VisualizerType.Voronoi;
            Selector.SelectedItem = Type;
            _ = MainGrid.Children.Add(Selector);
            UpdateSize(ActualWidth, ActualHeight);
        }

        public override void Update()
        {
            if (Editors != null)
            {
                DrawDiagram(Canvas1, Editors, VoronoiMode.HUE_SAT);
                DrawDiagram(Canvas2, Editors, VoronoiMode.HUE_VAL);
                DrawDiagram(Canvas3, Editors, VoronoiMode.SAT_VAL);
            }
        }

        private void SetScaling(double size)
        {
            ScalingFactor = size * MARGIN_FACTOR;
            Canvas1.Width = ScalingFactor;
            Canvas1.Height = ScalingFactor;
            Canvas2.Width = ScalingFactor;
            Canvas2.Height = ScalingFactor;
            Canvas3.Width = ScalingFactor;
            Canvas3.Height = ScalingFactor;
            LeftX = 0.0 * ScalingFactor;
            RightX = 1.0 * ScalingFactor;
            TopY = 0.0 * ScalingFactor;
            BottomY = 1.0 * ScalingFactor;
            StepSize = ScalingFactor / 40.0;
        }


        private void DrawDiagram(Canvas canvas, List<PaletteEditor> editors, VoronoiMode mode)
        {
            canvas.Children.Clear();
            List<HSVColor> hSVColors = GetUniqueColorsFromPalettes(editors);
            if (hSVColors != null && hSVColors.Count > 0)
            {
                for (double x = LeftX; x < RightX; x += StepSize)
                {
                    for (double y = TopY; y < BottomY; y += StepSize)
                    {
                        double xNorm = x / (RightX - LeftX);
                        double yNorm = y / (BottomY - TopY);
                        HSVColor color = GetClosestColor(hSVColors, xNorm, yNorm, mode);
                        Ellipse e = new()
                        {
                            Width = StepSize,
                            Height = StepSize
                        };
                        Canvas.SetLeft(e, x);
                        Canvas.SetTop(e, y);
                        e.Fill = new SolidColorBrush(color.GetRGBColor());
                        _ = canvas.Children.Add(e);
                    }
                }
            }
        }

        private static HSVColor GetClosestColor(List<HSVColor> colors, double xNorm, double yNorm, VoronoiMode mode)
        {
            HSVColor foundColor = new();
            double? minDistance = null;
            foreach (HSVColor color in colors)
            {
                double hueNorm = Convert.ToDouble(color.Hue) / Convert.ToDouble(HSVColor.MAX_VALUE_DEGREES);
                double satNorm = Convert.ToDouble(color.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                double valNorm = Convert.ToDouble(color.Brightness) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT); ;
                double distance;
                if (mode == VoronoiMode.HUE_VAL)
                {
                    distance = Math.Sqrt(((hueNorm - xNorm) * (hueNorm - xNorm)) + ((valNorm - yNorm) * (valNorm - yNorm)));
                }
                else if (mode == VoronoiMode.HUE_SAT)
                {
                    distance = Math.Sqrt(((hueNorm - xNorm) * (hueNorm - xNorm)) + ((satNorm - yNorm) * (satNorm - yNorm)));
                }
                else// if (mode == VoronoiMode.SAT_VAL)
                {
                    distance = Math.Sqrt(((satNorm - xNorm) * (satNorm - xNorm)) + ((valNorm - yNorm) * (valNorm - yNorm)));
                }

                if (minDistance == null || distance < minDistance)
                {
                    foundColor = color;
                    minDistance = distance;
                }
            }
            return foundColor;
        }

        protected override void UpdateSize(double width, double height)
        {
            double size = height < width / NUM_ELEMENTS ? height : width / NUM_ELEMENTS;
            SetScaling(size);
            Update();
        }

        private void MainGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Selector.Visibility = Visibility.Visible;
            Label1.Visibility = Visibility.Visible;
            Label2.Visibility = Visibility.Visible;
            Label3.Visibility = Visibility.Visible;
        }

        private void MainGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Selector.Visibility = Visibility.Hidden;
            Label1.Visibility = Visibility.Hidden;
            Label2.Visibility = Visibility.Hidden;
            Label3.Visibility = Visibility.Hidden;
        }
    }
}
