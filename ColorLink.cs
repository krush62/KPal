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

namespace KPal
{
    public class ColorLink
    {
        public class ColorLinkPartner
        {
            public PaletteEditor Editor { get; private set; }
            public PaletteColor Color { get; private set; }
            public ColorLinkPartner(PaletteEditor editor, PaletteColor color)
            {
                Editor = editor;
                Color = color;
            }
        }

        public ColorLinkPartner Source { get; private set; }
        public ColorLinkPartner Target { get; private set; }
        public bool KeepBrightnessData { get; private set; }

        public ColorLink(ColorLinkPartner source, ColorLinkPartner target, bool keepBrightnessData = false)
        {
            KeepBrightnessData = keepBrightnessData;
            Source = source;
            Target = target;
        }

        public ColorLink(PaletteEditor sourceEditor, PaletteColor sourceColor, PaletteEditor targetEditor, PaletteColor targetColor, bool keepBrightnessData = false)
        {
            Source = new ColorLinkPartner(sourceEditor, sourceColor);
            Target = new ColorLinkPartner(targetEditor, targetColor);
            KeepBrightnessData = keepBrightnessData;
        }

        public void SetFollower(ColorLinkPartner target)
        {
            Target = target;
        }

        public void SetFollower(PaletteEditor targetEditor, PaletteColor targetColor)
        {
            SetFollower(new ColorLinkPartner(targetEditor, targetColor));
        }
    }
}
