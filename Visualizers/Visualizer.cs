﻿using System;
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
            LabView
        }

        public VisualizerType Type { get; protected set; }
        public event EventHandler? VisualizerSelectionChanged;
        protected ComboBox Selector;

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
        }

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

        public abstract void Update(List<PaletteEditor> editors, List<ColorLink> links);

        public static Visualizer GetVisualizerForType(VisualizerType type)
        {
            Visualizer vis = type switch
            {
                Visualizer.VisualizerType.PaletteMerge => new PaletteMergeView(),
                Visualizer.VisualizerType.HueVal => new HueValView(),
                Visualizer.VisualizerType.SatVal => new SatValView(),
                Visualizer.VisualizerType.LabView => new LabView(),
                _ => new ShadingPreview(),
            };
            return vis;
        }
    }
}
