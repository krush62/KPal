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

        public ColorLink(ColorLinkPartner source, ColorLinkPartner target)
        {
            Source = source;
            Target = target;
        }

        public ColorLink(PaletteEditor sourceEditor, PaletteColor sourceColor, PaletteEditor targetEditor, PaletteColor targetColor)
        {
            Source = new ColorLinkPartner(sourceEditor, sourceColor);
            Target = new ColorLinkPartner(targetEditor, targetColor);
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
