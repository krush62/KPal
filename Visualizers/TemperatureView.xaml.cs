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
    public partial class TemperatureView : Visualizer
    {
        public TemperatureView()
        {
            InitializeComponent();
            Type = VisualizerType.Temperature;
            Selector.SelectedItem = Type;
            _ = MainGrid.Children.Add(Selector);
        }

        public override void Update()
        {
            List<Rectangle> toRemove = new();
            foreach (UIElement uIElement in TemperatureGrid.Children)
            {
                if (uIElement is Rectangle rect)
                {
                    toRemove.Add(rect);
                }
            }
            foreach (Rectangle rectangle1 in toRemove)
            {
                TemperatureGrid.Children.Remove(rectangle1);
            }

            int rowCount = TemperatureGrid.RowDefinitions.Count;
            for (int i = 1; i < rowCount; i++)
            {
                TemperatureGrid.RowDefinitions.RemoveAt(1);
            }


            if (Editors != null)
            {
                List<HSVColor> hSVColors = GetUniqueColorsFromPalettes(Editors);
                List<List<Rectangle>> drawList = new();
                List<double> temperatureList = new();
                //create lists
                for (int i = 0; i < TemperatureGrid.ColumnDefinitions.Count; i++)
                {
                    drawList.Add(new List<Rectangle>());
                    if (TemperatureGrid.Children[i] is Viewbox vb)
                    {
                        if (vb.Child is Label label)
                        {
                            if (label.Content is string tempNumberString)
                            {
                                if (!Double.TryParse(tempNumberString, out double result))
                                {
                                    result = (i + 1) * 1000;
                                }
                                temperatureList.Add(result);
                            }
                        }
                    }
                }

                //sort colors into list
                foreach (HSVColor color in hSVColors)
                {
                    //calculate colors using Planckian locus approximation
                    Color rgbColor = color.GetRGBColor();
                    //normalize
                    double r = rgbColor.R / 255.0;
                    double g = rgbColor.G / 255.0;
                    double b = rgbColor.B / 255.0;

                    // Convert RGB to XYZ
                    double x = r * 0.664511 + g * 0.154324 + b * 0.162028;
                    double y = r * 0.283881 + g * 0.668433 + b * 0.047685;
                    double z = r * 0.000088 + g * 0.072310 + b * 0.986039;

                    // Calculate xy chromaticity coordinates
                    double sum = x + y + z;
                    double xc = x / sum;
                    double yc = y / sum;

                    // Convert xy to correlated color temperature (Kelvin)
                    double n = (xc - 0.3320) / (0.1858 - yc);
                    double kelvin = 449.0 * Math.Pow(n, 3.0) + 3525.0 * Math.Pow(n, 2.0) + 6823.3 * n + 5520.33;

                    //find correct index for color
                    int closestIndex = -1;
                    double closestDistance = -1;
                    for (int i = 0; i < temperatureList.Count; i++)
                    {
                        double distance = Math.Abs(temperatureList[i] - kelvin);
                        if (i == 0 || distance < closestDistance)
                        {
                            closestIndex = i;
                            closestDistance = distance;
                        }
                    }
                    Rectangle rectangle = new()
                    {
                        Fill = new SolidColorBrush(rgbColor)
                    };
                    drawList[closestIndex].Add(rectangle);
                }

                int maxRows = drawList.Max(x => x.Count);
                for (int i = 0; i < maxRows; i++)
                {
                    RowDefinition rd = new()
                    {
                        Height = new(1, GridUnitType.Star)
                    };
                    TemperatureGrid.RowDefinitions.Add(rd);
                }

                for (int i = 0; i < drawList.Count; i++)
                {
                    for (int j = 0; j < drawList[i].Count; j++)
                    {
                        Rectangle r = drawList[i][j];
                        _ = TemperatureGrid.Children.Add(r);
                        Grid.SetColumn(r, i);
                        Grid.SetRow(r, j + 1);
                    }
                }
            }
        }

        protected override void UpdateSize(double width, double height)
        {

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
