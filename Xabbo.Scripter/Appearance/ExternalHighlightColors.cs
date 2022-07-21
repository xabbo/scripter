using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.CodeAnalysis.Classification;
using RoslynPad.Editor;

namespace Xabbo.Scripter.Appearance
{
    public class ExternalHighlightColors : IClassificationHighlightColors
    {
        private static HighlightingColor CreateColor(Color? fg = null, Color? bg = null)
        {
            return new HighlightingColor
            {
                Foreground = new SimpleHighlightingBrush(fg ?? Color.FromArgb(0, 0, 0, 0)),
                Background = new SimpleHighlightingBrush(bg ?? Color.FromArgb(0, 0, 0, 0))
            };
        }

        private static Color ToColor(string hex)
        {
            int value = int.Parse(hex, NumberStyles.HexNumber);

            if (hex.Length == 6)
            {
                value |= (0xFF << 24);
            }

            return Color.FromArgb(
                (byte)((value >> 24) & 0xFF),
                255,//(byte)((value >> 16) & 0xFF),
                0,//(byte)((value >> 8) & 0xFF),
                0//(byte)(value & 0xFF)
            );
        }

        private static HighlightingColor CreateColor(string? fg = null, string? bg = null)
        {
            HighlightingColor color = new HighlightingColor();
            if (fg is not null) color.Foreground = new SimpleHighlightingBrush(ToColor(fg));
            if (bg is not null) color.Background = new SimpleHighlightingBrush(ToColor(bg));
            return color;
        }

        public HighlightingColor DefaultBrush { get; protected set; } = CreateColor("FFF1F1F1");

        public HighlightingColor TypeBrush { get; protected set; } = CreateColor("4ec9b0");

        public HighlightingColor InterfaceBrush { get; protected set; } = CreateColor("4ec9b0");

        public HighlightingColor MethodBrush { get; protected set; } = CreateColor("dcdcaa");

        public HighlightingColor CommentBrush { get; protected set; } = CreateColor("505050");

        public HighlightingColor XmlCommentBrush { get; protected set; } = CreateColor("608b4e");
        public HighlightingColor XmlCdataBrush { get; protected set; } = CreateColor("608b4e");
        public HighlightingColor XmlCommentCommentBrush { get; protected set; } = CreateColor("608b4e");
        public HighlightingColor KeywordBrush { get; protected set; } = CreateColor("569cd6");
        public HighlightingColor ControlKeywordBrush { get; protected set; } = CreateColor("C586C0");
        public HighlightingColor PreprocessorKeywordBrush { get; protected set; } = CreateColor("61AFEF");
        public HighlightingColor StringBrush { get; protected set; } = CreateColor("ce9178");
        public HighlightingColor StringEscapeBrush { get; protected set; } = CreateColor("d7ba7d");
        public HighlightingColor BraceMatchingBrush { get; protected set; } = CreateColor("505050", "528BFF3D");
        public HighlightingColor StaticSymbolBrush { get; protected set; } = new HighlightingColor { /*FontWeight = FontWeights.Bold*/ };

        public const string BraceMatchingClassificationTypeName = "brace matching";

        private readonly Lazy<ImmutableDictionary<string, HighlightingColor>> _map;

        private static Color ParseColor(string hex)
        {
            if (hex.StartsWith("#"))
            {
                hex = hex[1..];
            }

            if (hex.Length == 6)
            {
                hex = "FF" + hex;
            }

            int value = int.Parse(hex, NumberStyles.HexNumber);

            return Color.FromArgb(
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF)
            );
        }

        public ExternalHighlightColors()
        {
            _map = new Lazy<ImmutableDictionary<string, HighlightingColor>>(() =>
            {
                Dictionary<string, HighlightingColor> map = new();

                HighlightingColor bruh = new HighlightingColor()
                {
                    Background = new SimpleHighlightingBrush(ParseColor("#FF33FF")),
                    Foreground = new SimpleHighlightingBrush(ParseColor("#33FF33"))
                };

                foreach (var field in typeof(ClassificationTypeNames).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    string? name = (string?)field.GetValue(null);
                    if (name is null) continue;

                    //map[name] = AsFrozen(bruh);
                }

                return map.ToImmutableDictionary();

                var definitions = System.Text.Json.JsonSerializer.Deserialize<List<HighlightingColorDefinition>>(
                    System.IO.File.ReadAllText("theme.json")
                ) ?? throw new Exception("Failed to deserialize theme.");

                foreach (var definition in definitions)
                {
                    HighlightingColor brush = new HighlightingColor();

                    if (!string.IsNullOrWhiteSpace(definition.Foreground))
                    {
                        brush.Foreground = new SimpleHighlightingBrush(
                            ParseColor(definition.Foreground)
                        );
                    }

                    if (!string.IsNullOrWhiteSpace(definition.Background))
                    {
                        brush.Background = new SimpleHighlightingBrush(
                            ParseColor(definition.Background)
                        );
                    }

                    foreach (string scope in definition.Scopes)
                    {
                        map[scope] = AsFrozen(brush);
                    }
                }

                return map.ToImmutableDictionary();
            });
        }

        protected virtual ImmutableDictionary<string, HighlightingColor> GetOrCreateMap()
        {
            return _map.Value;
        }

        public HighlightingColor GetBrush(string classificationTypeName)
        {
            GetOrCreateMap().TryGetValue(classificationTypeName, out var brush);
            return brush ?? AsFrozen(DefaultBrush);
        }

        private static HighlightingColor AsFrozen(HighlightingColor color)
        {
            if (!color.IsFrozen)
            {
                color.Freeze();
            }
            return color;
        }
    }

    class HighlightingColorDefinition
    {
        [JsonPropertyName("foreground")]
        public string? Foreground { get; set; }

        [JsonPropertyName("background")]
        public string? Background { get; set; }

        [JsonPropertyName("fontStyle")]
        public FontStyle? FontStyle { get; set; }

        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; } = new();
    }
}
