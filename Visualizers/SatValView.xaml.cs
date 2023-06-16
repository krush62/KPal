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
            Type = VisualizerType.HueVal;
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
                for (int j = 0; j < editors[i].ColorList.Count; j++)
                {
                    HSVColor color = editors[i].ColorList[j];
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
