using ICSharpCode.AvalonEdit.Highlighting;
using RoslynPad.Editor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Xabbo.Scripter.Appearance
{
    public class DarkClassificationHighlightColors : ClassificationHighlightColors
    {
        private static Color ToColor(string hex)
        {
            int value = int.Parse(hex, NumberStyles.HexNumber);

            if (hex.Length == 6)
            {
                value |= (0xFF << 24);
            }

            return Color.FromArgb(
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF)
            );
        }

        private static HighlightingColor CreateColor(string? fg = null, string? bg = null)
        {
            HighlightingColor color = new HighlightingColor();
            if (fg is not null) color.Foreground = new SimpleHighlightingBrush(ToColor(fg));
            if (bg is not null) color.Background = new SimpleHighlightingBrush(ToColor(bg));
            return color;
        }

        public DarkClassificationHighlightColors()
        {
            DefaultBrush = CreateColor("ff0000");
            TypeBrush = CreateColor("ff0000");
            MethodBrush = CreateColor("ff0000");
            CommentBrush = CreateColor("ff0000");
            XmlCommentBrush = CreateColor("ff0000");
            KeywordBrush = CreateColor("ff0000");
            PreprocessorKeywordBrush = CreateColor("ff0000");
            StringBrush = CreateColor("ff0000");
            BraceMatchingBrush = CreateColor("ff0000");
            StaticSymbolBrush = CreateColor("ff0000");

            /*
               public HighlightingColor DefaultBrush { get; protected set; }
        public HighlightingColor TypeBrush { get; protected set; }
        public HighlightingColor MethodBrush { get; protected set; }
        public HighlightingColor CommentBrush { get; protected set; }
        public HighlightingColor XmlCommentBrush { get; protected set; }
        public HighlightingColor KeywordBrush { get; protected set; }
        public HighlightingColor PreprocessorKeywordBrush { get; protected set; }
        public HighlightingColor StringBrush { get; protected set; }
        public HighlightingColor BraceMatchingBrush { get; protected set; }
        public HighlightingColor StaticSymbolBrush { get; protected set; }*/


        }
    }
}
