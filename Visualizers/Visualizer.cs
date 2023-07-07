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
using System.Windows.Controls;

namespace KPal
{
    public abstract class Visualizer : UserControl
    {
        public enum VisualizerType : byte
        {
            ShadingCube = 0,
            PaletteMerge = 1,
            HueVal = 2,
            SatVal = 3,
            LabView = 4,
            Voronoi = 5
        }

        public VisualizerType Type { get; protected set; }
        public event EventHandler? VisualizerSelectionChanged;
        protected ComboBox Selector;
        protected List<PaletteEditor>? Editors;
        protected List<ColorLink>? Links;

        public Visualizer()
        {
            Selector = new ComboBox();
            foreach (VisualizerType type in Enum.GetValues(typeof(VisualizerType)))
            {
                _ = Selector.Items.Add(type);
            }
            Selector.Style = (System.Windows.Style)TryFindResource("ComboBoxStyle1");
            Selector.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            Selector.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            Selector.SelectionChanged += Selector_SelectionChanged;
            Selector.Visibility = System.Windows.Visibility.Hidden;
            SizeChanged += Visualizer_SizeChanged;
        }

        private void Visualizer_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (Editors != null && Links != null)
            {
                UpdateSize(ActualWidth, ActualHeight);
            }
        }

        protected abstract void UpdateSize(double width, double height);

        protected static List<HSVColor> GetUniqueColorsFromPalettes(List<PaletteEditor> editors)
        {
            List<HSVColor> hSVColors = new();
            foreach (PaletteEditor editor in editors)
            {
                foreach (PaletteColor paletteColor in editor.PaletteColorList)
                {
                    HSVColor color = paletteColor.HSVColor;
                    if (!hSVColors.Where(c => c.Hue == color.Hue && c.Saturation == color.Saturation && c.Brightness == color.Brightness).Any())
                    {
                        hSVColors.Add(color);
                    }
                }

            }
            return hSVColors;
        }

        private void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VisualizerSelectionChanged?.Invoke(this, e);
        }

        public void Update(List<PaletteEditor> editors, List<ColorLink> links)
        {
            Editors = editors;
            Links = links;
            Update();
        }

        public abstract void Update();

        public static Visualizer GetVisualizerForType(VisualizerType type)
        {
            Visualizer vis = type switch
            {
                Visualizer.VisualizerType.PaletteMerge => new PaletteMergeView(),
                Visualizer.VisualizerType.HueVal => new HueValView(),
                Visualizer.VisualizerType.SatVal => new SatValView(),
                Visualizer.VisualizerType.LabView => new LabView(),
                Visualizer.VisualizerType.Voronoi => new VoronoiView(),
                _ => new ShadingPreview(),
            };
            return vis;
        }
    }
}
