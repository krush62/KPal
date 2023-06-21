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

    public partial class SatValView : Visualizer
    {
        public SatValView()
        {
            InitializeComponent();
            Type = VisualizerType.SatVal;
            Selector.SelectedItem = Type;
            _ = MainGrid.Children.Add(Selector);
        }

        public override void Update(List<PaletteEditor> editors, List<ColorLink> links)
        {
            ColorGrid.ColumnDefinitions.Clear();
            ColorGrid.Children.Clear();
            double topHeight = ColorGrid.RowDefinitions[0].ActualHeight;
            double bottomHeight = ColorGrid.RowDefinitions[2].ActualHeight;
            int colCounter = 0;
            for (int i = 0; i < editors.Count; i++)
            {
                for (int j = 0; j < editors[i].PaletteColorList.Count; j++)
                {
                    HSVColor color = editors[i].PaletteColorList[j].HSVColor;
                    double satNorm = Convert.ToDouble(color.Saturation) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                    double valNorm = Convert.ToDouble(color.Brightness) / Convert.ToDouble(HSVColor.MAX_VALUE_VAL_SAT);
                    ColumnDefinition cd = new()
                    {
                        Width = new GridLength(1, GridUnitType.Star)
                    };
                    ColorGrid.ColumnDefinitions.Add(cd);

                    Rectangle rTop = new()
                    {
                        Fill = new SolidColorBrush(color.GetRGBColor()),
                        Height = topHeight * valNorm,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };
                    Grid.SetRow(rTop, 0);
                    Grid.SetColumn(rTop, colCounter);
                    Rectangle rBottom = new()
                    {
                        Fill = new SolidColorBrush(color.GetRGBColor()),
                        Height = bottomHeight * satNorm,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    Grid.SetRow(rBottom, 2);
                    Grid.SetColumn(rBottom, colCounter);
                    _ = ColorGrid.Children.Add(rTop);
                    _ = ColorGrid.Children.Add(rBottom);
                    colCounter++;
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
