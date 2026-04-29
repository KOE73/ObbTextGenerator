using SkiaSharp;

namespace ObbTextGenerator;

public sealed record ResolvedSystemFontVariant(
    string FamilyName,
    string StyleName,
    SKFontStyle Style,
    SKTypeface Typeface);
