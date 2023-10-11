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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KPal
{
    public partial class ShadingPreview : Visualizer
    {
        private const int CUBE_COUNT = 3;
        private static readonly int[,] CUBE_POINTS_0 = { { 8, 0 }, { 6, 1 }, { 7, 1 }, { 9, 1 }, { 10, 1 }, { 4, 2 }, { 5, 2 }, { 11, 2 }, { 12, 2 }, { 2, 3 }, { 3, 3 }, { 13, 3 }, { 14, 3 }, { 1, 4 }, { 15, 4 }, { 0, 5 }, { 0, 6 }, { 0, 7 }, { 0, 8 }, { 0, 9 }, { 0, 10 }, { 0, 11 }, { 0, 12 }, { 0, 13 }, { 16, 5 }, { 16, 6 }, { 16, 7 }, { 16, 8 }, { 16, 9 }, { 16, 10 }, { 16, 11 }, { 16, 12 }, { 16, 13 }, { 1, 14 }, { 15, 14 }, { 2, 15 }, { 3, 15 }, { 13, 15 }, { 14, 15 }, { 4, 16 }, { 5, 16 }, { 11, 16 }, { 12, 16 }, { 6, 17 }, { 7, 17 }, { 9, 17 }, { 10, 17 }, { 8, 18 } };
        private static readonly int[,] CUBE_POINTS_1 = { { 1, 6 }, { 1, 7 }, { 2, 7 }, { 3, 7 }, { 1, 8 }, { 2, 8 }, { 3, 8 }, { 4, 8 }, { 5, 8 }, { 1, 9 }, { 2, 9 }, { 3, 9 }, { 4, 9 }, { 5, 9 }, { 6, 9 }, { 7, 9 }, { 1, 10 }, { 2, 10 }, { 3, 10 }, { 4, 10 }, { 5, 10 }, { 6, 10 }, { 7, 10 }, { 1, 11 }, { 2, 11 }, { 3, 11 }, { 4, 11 }, { 5, 11 }, { 6, 11 }, { 7, 11 }, { 1, 12 }, { 2, 12 }, { 3, 12 }, { 4, 12 }, { 5, 12 }, { 6, 12 }, { 7, 12 }, { 1, 13 }, { 2, 13 }, { 3, 13 }, { 4, 13 }, { 5, 13 }, { 6, 13 }, { 7, 13 }, { 2, 14 }, { 3, 14 }, { 4, 14 }, { 5, 14 }, { 6, 14 }, { 7, 14 }, { 4, 15 }, { 5, 15 }, { 6, 15 }, { 7, 15 }, { 6, 16 }, { 7, 16 } };
        private static readonly int[,] CUBE_POINTS_2 = { { 15, 6 }, { 13, 7 }, { 14, 7 }, { 15, 7 }, { 11, 8 }, { 12, 8 }, { 13, 8 }, { 14, 8 }, { 15, 8 }, { 9, 9 }, { 10, 9 }, { 11, 9 }, { 12, 9 }, { 13, 9 }, { 14, 9 }, { 15, 9 }, { 9, 10 }, { 10, 10 }, { 11, 10 }, { 12, 10 }, { 13, 10 }, { 14, 10 }, { 15, 10 }, { 9, 11 }, { 10, 11 }, { 11, 11 }, { 12, 11 }, { 13, 11 }, { 14, 11 }, { 15, 11 }, { 9, 12 }, { 10, 12 }, { 11, 12 }, { 12, 12 }, { 13, 12 }, { 14, 12 }, { 15, 12 }, { 9, 13 }, { 10, 13 }, { 11, 13 }, { 12, 13 }, { 13, 13 }, { 14, 13 }, { 15, 13 }, { 9, 14 }, { 10, 14 }, { 11, 14 }, { 12, 14 }, { 13, 14 }, { 14, 14 }, { 9, 15 }, { 10, 15 }, { 11, 15 }, { 12, 15 }, { 9, 16 }, { 10, 16 } };
        private static readonly int[,] CUBE_POINTS_3 = { { 8, 1 }, { 6, 2 }, { 7, 2 }, { 8, 2 }, { 9, 2 }, { 10, 2 }, { 4, 3 }, { 5, 3 }, { 6, 3 }, { 7, 3 }, { 8, 3 }, { 9, 3 }, { 10, 3 }, { 11, 3 }, { 12, 3 }, { 2, 4 }, { 3, 4 }, { 4, 4 }, { 5, 4 }, { 6, 4 }, { 7, 4 }, { 8, 4 }, { 9, 4 }, { 10, 4 }, { 11, 4 }, { 12, 4 }, { 13, 4 }, { 14, 4 }, { 2, 5 }, { 3, 5 }, { 4, 5 }, { 5, 5 }, { 6, 5 }, { 7, 5 }, { 8, 5 }, { 9, 5 }, { 10, 5 }, { 11, 5 }, { 12, 5 }, { 13, 5 }, { 14, 5 }, { 4, 6 }, { 5, 6 }, { 6, 6 }, { 7, 6 }, { 8, 6 }, { 9, 6 }, { 10, 6 }, { 11, 6 }, { 12, 6 }, { 6, 7 }, { 7, 7 }, { 8, 7 }, { 9, 7 }, { 10, 7 }, { 8, 8 } };
        private static readonly int[,] CUBE_POINTS_4 = { { 1, 5 }, { 15, 5 }, { 2, 6 }, { 3, 6 }, { 13, 6 }, { 14, 6 }, { 4, 7 }, { 5, 7 }, { 11, 7 }, { 12, 7 }, { 6, 8 }, { 7, 8 }, { 9, 8 }, { 10, 8 }, { 8, 9 }, { 8, 10 }, { 8, 11 }, { 8, 12 }, { 8, 13 }, { 8, 14 }, { 8, 15 }, { 8, 16 }, { 8, 17 } };

        private static readonly List<int[,]> POINT_LIST = new();
        private const double PIXEL_SCALE_FACTOR = 1.2;
        private double PixelSize;
        private int OffsetVertical;
        private int OffsetHorizontal;

        public ShadingPreview()
        {
            InitializeComponent();
            POINT_LIST.Add(CUBE_POINTS_0);
            POINT_LIST.Add(CUBE_POINTS_1);
            POINT_LIST.Add(CUBE_POINTS_2);
            POINT_LIST.Add(CUBE_POINTS_3);
            POINT_LIST.Add(CUBE_POINTS_4);
            CalculateScaling(120);
            Type = VisualizerType.ShadingCube;
            Selector.SelectedItem = Type;
            _ = MainGrid.Children.Add(Selector);
        }

        private void CalculateScaling(double size)
        {
            PixelSize = size / 60;
            OffsetVertical = Convert.ToInt32(size / CUBE_COUNT);
            OffsetHorizontal = Convert.ToInt32(size / CUBE_COUNT);
        }


        public override void Update()
        {
            MainCanvas.Children.Clear();
            if (Editors != null)
            {
                for (int i = 0; i < Editors.Count; i++)
                {
                    int colorCount = Editors[i].PaletteColorList.Count;
                    List<List<Color>> drawingColors = new();

                    if (colorCount == 3)
                    {
                        List<Color> fiveColors = new()
                        {
                            Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[1].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor()
                        };
                        drawingColors.Add(fiveColors);
                        drawingColors.Add(fiveColors);
                        drawingColors.Add(fiveColors);
                    }
                    else if (colorCount == 4)
                    {
                        List<Color> fiveColors = new()
                        {
                            Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[1].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor()
                        };
                        List<Color> fiveColors2 = new()
                        {
                            Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[1].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[3].HSVColor.GetRGBColor()
                        };
                        List<Color> fiveColors3 = new()
                        {
                            Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[1].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[3].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[3].HSVColor.GetRGBColor()
                        };
                        drawingColors.Add(fiveColors);
                        drawingColors.Add(fiveColors2);
                        drawingColors.Add(fiveColors3);
                    }
                    else if (colorCount >= 5)
                    {
                        List<Color> fiveColors = new()
                        {
                            Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[1].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[3].HSVColor.GetRGBColor(),
                            Editors[i].PaletteColorList[4].HSVColor.GetRGBColor()
                        };
                        drawingColors.Add(fiveColors);

                        if (colorCount == 5)
                        {
                            List<Color> fiveColors2 = new()
                            {
                                Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[0].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[1].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[2].HSVColor.GetRGBColor()
                            };
                            drawingColors.Insert(0, fiveColors2);

                            List<Color> fiveColors3 = new()
                            {
                                Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[2].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[3].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[4].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[4].HSVColor.GetRGBColor()
                            };
                            drawingColors.Add(fiveColors3);
                        }
                        else
                        {
                            int centerColorIndex = colorCount / 2;
                            List<Color> fiveColors2 = new()
                            {
                                Editors[i].PaletteColorList[centerColorIndex - 2].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[centerColorIndex - 1].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[centerColorIndex].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[centerColorIndex + 1].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[centerColorIndex + 2].HSVColor.GetRGBColor()
                            };
                            drawingColors.Add(fiveColors2);

                            List<Color> fiveColors3 = new()
                            {
                                Editors[i].PaletteColorList[colorCount - 5].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[colorCount - 4].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[colorCount - 3].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[colorCount - 2].HSVColor.GetRGBColor(),
                                Editors[i].PaletteColorList[colorCount - 1].HSVColor.GetRGBColor()
                            };
                            drawingColors.Add(fiveColors3);
                        }
                    }
                    for (int j = 0; j < drawingColors.Count; j++)
                    {
                        DrawCube(OffsetHorizontal * i, OffsetVertical * j, drawingColors[j]);
                    }
                }
            }
        }

        private void DrawCube(int offsetX, int offsetY, List<Color> fiveColors)
        {
            for (int i = 0; i < fiveColors.Count; i++)
            {
                for (int j = 0; j < POINT_LIST[i].Length / 2; j++)
                {
                    int x = POINT_LIST[i][j, 0];
                    int y = POINT_LIST[i][j, 1];
                    Rectangle r = new()
                    {
                        Fill = new SolidColorBrush(fiveColors[i]),
                        Width = PixelSize * PIXEL_SCALE_FACTOR,
                        Height = PixelSize * PIXEL_SCALE_FACTOR
                    };
                    Canvas.SetLeft(r, PixelSize * x + offsetX);
                    Canvas.SetTop(r, PixelSize * y + offsetY);
                    _ = MainCanvas.Children.Add(r);
                }
            }
        }

        protected override void UpdateSize(double width, double height)
        {
            double size = height;
            if (Editors != null && Editors.Count > 0 && width / Editors.Count * 3 < height)
            {
                size = width / Editors.Count * CUBE_COUNT;
            }


            CalculateScaling(size);
            Update();
        }

        private void MainGrid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Selector.Visibility = System.Windows.Visibility.Visible;
        }

        private void MainGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Selector.Visibility = System.Windows.Visibility.Hidden;
        }
    }
}
