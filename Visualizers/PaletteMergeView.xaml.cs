﻿/*
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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KPal
{

    public partial class PaletteMergeView : Visualizer
    {
        public PaletteMergeView()
        {
            InitializeComponent();
            Type = VisualizerType.PaletteMerge;
            Selector.SelectedItem = Type;
            _ = MainGrid.Children.Add(Selector);
        }

        private struct PalettePaletteLink
        {
            public PaletteLink? PLink { get; private set; }
            public PaletteEditor Editor { get; private set; }
            public PalettePaletteLink(PaletteLink? pLink, PaletteEditor editor)
            {
                PLink = pLink;
                Editor = editor;
            }
        }

        public struct PaletteLink
        {
            public int SourceColorIndex { get; private set; }
            public int TargetColorIndex { get; private set; }
            public PaletteLink(int sourceColorIndex, int targetColorIndex)
            {
                SourceColorIndex = sourceColorIndex;
                TargetColorIndex = targetColorIndex;
            }
        }


        public override void Update()
        {
            if (Editors != null && Editors.Count > 0 && Links != null)
            {
                int totalMaxOffsetMinus = 0;
                int totalMinOffsetMinus = 0;

                List<PalettePaletteLink> displayList = new();
                if (CheckIntegrity(Editors, Links))
                {

                    for (int i = 0; i < Editors.Count; i++)
                    {
                        PaletteEditor palette = Editors[i];
                        bool isSource = Links.Where(p => p.Source.Editor == palette).Any();
                        bool isTarget = Links.Where(p => p.Target.Editor == palette).Any();

                        if (!isSource && !isTarget)
                        {
                            displayList.Add(new PalettePaletteLink(null, palette));
                        }
                        else if (!displayList.Where(x => x.Editor == palette).Any())
                        {
                            if (isTarget)
                            {
                                //search root source
                                PaletteEditor rootSource = palette;
                                ColorLink? ancestorFound = null;
                                do
                                {
                                    ancestorFound = Links.Where(p => p.Target.Editor == rootSource).FirstOrDefault();
                                    if (ancestorFound != null)
                                    {
                                        rootSource = ancestorFound.Source.Editor;
                                    }
                                }
                                while (ancestorFound != null);

                                //add tree to list
                                do
                                {
                                    ancestorFound = Links.Where(p => p.Source.Editor == rootSource).FirstOrDefault();
                                    if (ancestorFound != null)
                                    {
                                        int sourceColorIndex = ancestorFound.Source.Editor.PaletteColorList.IndexOf(ancestorFound.Source.Color);
                                        int targetColorIndex = ancestorFound.Target.Editor.PaletteColorList.IndexOf(ancestorFound.Target.Color);
                                        displayList.Add(new PalettePaletteLink(new PaletteLink(sourceColorIndex, targetColorIndex), rootSource));
                                        rootSource = ancestorFound.Target.Editor;
                                    }
                                    else
                                    {
                                        displayList.Add(new PalettePaletteLink(null, rootSource));
                                    }
                                }
                                while (ancestorFound != null);
                            }
                        }
                    }


                    //calculate yOffset
                    for (int i = 0; i < displayList.Count; i++)
                    {
                        int maxOffsetMinus = 0;
                        int minOffsetMinus = 0;
                        int offsetMinus = 0;
                        maxOffsetMinus = offsetMinus = 0;
                        PaletteLink? link = displayList[i].PLink;
                        PaletteEditor editor = displayList[i].Editor;
                        while (link != null)
                        {
                            offsetMinus += link.Value.SourceColorIndex - link.Value.TargetColorIndex;
                            if (offsetMinus < maxOffsetMinus)
                            {
                                maxOffsetMinus = offsetMinus;
                            }
                            if (offsetMinus > minOffsetMinus)
                            {
                                minOffsetMinus = offsetMinus;
                            }

                            link = displayList[++i].PLink;
                        }

                        if (maxOffsetMinus < totalMaxOffsetMinus)
                        {
                            totalMaxOffsetMinus = maxOffsetMinus;
                        }
                        if (minOffsetMinus > totalMinOffsetMinus)
                        {
                            totalMinOffsetMinus = minOffsetMinus;
                        }

                    }
                }
                else //create simple list
                {
                    foreach (PaletteEditor palette in Editors)
                    {
                        displayList.Add(new PalettePaletteLink(null, palette));
                    }
                }

                int max = Editors.Max(x => x.PaletteColorList.Count);
                int totalMaxOffsetPlus = max + totalMinOffsetMinus - totalMaxOffsetMinus;
                ColorGrid.RowDefinitions.Clear();
                ColorGrid.ColumnDefinitions.Clear();
                ColorGrid.Children.Clear();
                DrawDisplayList(displayList, totalMaxOffsetMinus, totalMaxOffsetPlus);
            }
            else
            {
                ColorGrid.RowDefinitions.Clear();
                ColorGrid.ColumnDefinitions.Clear();
                ColorGrid.Children.Clear();
            }
        }

        protected override void UpdateSize(double width, double height)
        {

        }

        private void DrawDisplayList(List<PalettePaletteLink> displayList, int offsetMinus, int offsetPlus)
        {
            for (int i = 0; i < displayList.Count * 2 - 1; i++)
            {
                ColumnDefinition col = new();
                if (i % 2 == 0)
                {
                    col.Width = new GridLength(2, GridUnitType.Star);
                }
                else
                {
                    col.Width = new GridLength(1, GridUnitType.Star);
                }
                ColorGrid.ColumnDefinitions.Add(col);
            }

            for (int i = 0; i < offsetPlus; i++)
            {
                RowDefinition row = new()
                {
                    Height = new GridLength(1, GridUnitType.Star)
                };
                ColorGrid.RowDefinitions.Add(row);
            }

            int drawingOffset = 0;
            for (int i = 0; i < displayList.Count; ++i)
            {
                for (int j = 0; j < displayList[i].Editor.PaletteColorList.Count; j++)
                {
                    Rectangle r = new()
                    {
                        Fill = new SolidColorBrush(displayList[i].Editor.PaletteColorList[j].HSVColor.GetRGBColor())
                    };
                    Grid.SetRow(r, j - offsetMinus + drawingOffset);
                    Grid.SetColumn(r, i * 2);
                    if (displayList[i].PLink.HasValue && displayList[i].PLink.GetValueOrDefault().SourceColorIndex == j)
                    {
                        Grid.SetColumnSpan(r, 2);
                    }
                    _ = ColorGrid.Children.Add(r);
                }
                if (displayList[i].PLink.HasValue)
                {
                    drawingOffset += displayList[i].PLink.GetValueOrDefault().SourceColorIndex - displayList[i].PLink.GetValueOrDefault().TargetColorIndex;
                }
                else
                {
                    drawingOffset = 0;
                }
            }
        }

        private static bool CheckIntegrity(List<PaletteEditor> palettes, List<ColorLink> colorLinkList)
        {
            bool isInteger = true;

            foreach (PaletteEditor palette in palettes)
            {
                if (colorLinkList.Where(p => p.Source.Editor == palette).Count() > 1)
                {
                    isInteger = false;
                    break;
                }
            }
            return isInteger;
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
