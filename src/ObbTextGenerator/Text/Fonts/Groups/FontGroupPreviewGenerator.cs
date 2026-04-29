using SkiaSharp;
using Spectre.Console;

namespace ObbTextGenerator;

public sealed class FontGroupPreviewGenerator
{
    private const int ImageWidth = 2200;
    private const int Margin = 40;
    private const int TitleHeight = 110;
    private const int RowHeight = 88;
    private const int LabelColumnWidth = 430;
    private const int ColumnGap = 30;
    private const float TitleFontSize = 40;
    private const float SubtitleFontSize = 22;
    private const float LabelFontSize = 24;
    private const float BaseSampleFontSize = 46;
    private const float MinSampleFontSize = 18;

    public void GenerateAll(
        string configDirectory,
        string outputDirectory,
        string sampleText,
        FontPreviewVariantsMode variantsMode,
        RandomSystemFontProviderSettings? filteredSettings)
    {
        var groups = FontGroupLoader.LoadAllGroups(configDirectory);
        if (groups.Count == 0)
        {
            throw new InvalidOperationException("No font groups were found in Resources/FontGroups.");
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (var group in groups)
        {
            GenerateSingleGroup(group, configDirectory, outputDirectory, sampleText, variantsMode, filteredSettings);
        }

        AnsiConsole.MarkupLine($"[green]Font group preview saved to:[/] [yellow]{outputDirectory}[/]");
    }

    private void GenerateSingleGroup(
        FontGroupDefinition group,
        string configDirectory,
        string outputDirectory,
        string sampleText,
        FontPreviewVariantsMode variantsMode,
        RandomSystemFontProviderSettings? filteredSettings)
    {
        var resolvedFonts = ResolveFonts(group, configDirectory, sampleText, variantsMode, filteredSettings);
        var imageHeight = CalculateImageHeight(resolvedFonts.MissingFamilies.Count, resolvedFonts.FoundFonts.Count);

        using var bitmap = new SKBitmap(ImageWidth, imageHeight);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        DrawHeader(canvas, group, resolvedFonts, sampleText);
        DrawRows(canvas, resolvedFonts.FoundFonts, sampleText);
        DrawMissingFamilies(canvas, resolvedFonts.FoundFonts.Count, resolvedFonts.MissingFamilies);

        var fileName = $"{SanitizeFileName(group.Name)}.png";
        var outputPath = Path.Combine(outputDirectory, fileName);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Open(outputPath, FileMode.Create, FileAccess.Write);
        data.SaveTo(stream);

        AnsiConsole.MarkupLine($"[blue]Preview:[/] [white]{group.Name}[/] -> [grey]{outputPath}[/]");
    }

    private static ResolvedGroupFonts ResolveFonts(
        FontGroupDefinition group,
        string configDirectory,
        string sampleText,
        FontPreviewVariantsMode variantsMode,
        RandomSystemFontProviderSettings? filteredSettings)
    {
        var foundFonts = new List<ResolvedFontPreview>();
        var missingFamilies = new List<string>();
        var variants = ResolveVariants(group, configDirectory, variantsMode, filteredSettings);
        var groupedVariants = variants
            .GroupBy(variant => variant.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var family in group.Families)
        {
            var trimmedFamily = family.Trim();
            if (string.IsNullOrWhiteSpace(trimmedFamily))
            {
                continue;
            }

            if (!groupedVariants.TryGetValue(trimmedFamily, out var familyVariants) || familyVariants.Count == 0)
            {
                missingFamilies.Add(trimmedFamily);
                continue;
            }

            foreach (var variant in familyVariants)
            {
                var fittedFontSize = CalculateSampleFontSize(variant.Typeface, sampleText);
                var displayName = $"{trimmedFamily} / {variant.StyleName}";
                foundFonts.Add(new ResolvedFontPreview(displayName, variant.Typeface, fittedFontSize));
            }
        }

        return new ResolvedGroupFonts(foundFonts, missingFamilies);
    }

    private static List<ResolvedSystemFontVariant> ResolveVariants(
        FontGroupDefinition group,
        string configDirectory,
        FontPreviewVariantsMode variantsMode,
        RandomSystemFontProviderSettings? filteredSettings)
    {
        var includeGroups = new List<string> { group.Name };

        return variantsMode switch
        {
            FontPreviewVariantsMode.All => ResolveAllVariants(includeGroups, configDirectory),
            FontPreviewVariantsMode.Filtered => ResolveFilteredVariants(includeGroups, configDirectory, filteredSettings),
            _ => ResolveFamilyOnlyVariants(includeGroups, configDirectory)
        };
    }

    private static List<ResolvedSystemFontVariant> ResolveAllVariants(List<string> includeGroups, string configDirectory)
    {
        var settings = new RandomSystemFontProviderSettings
        {
            IncludeGroups = includeGroups,
            AllowedWeights = [],
            AllowedWidths = [],
            AllowedSlants = []
        };

        return SystemFontVariantResolver.Resolve(settings, configDirectory);
    }

    private static List<ResolvedSystemFontVariant> ResolveFilteredVariants(
        List<string> includeGroups,
        string configDirectory,
        RandomSystemFontProviderSettings? filteredSettings)
    {
        if (filteredSettings == null)
        {
            throw new InvalidOperationException("Font preview mode 'filtered' requires a text-line stage with font.type: system-random in the selected config.");
        }

        var settings = new RandomSystemFontProviderSettings
        {
            IncludeGroups = includeGroups,
            ExcludeGroups = filteredSettings.ExcludeGroups,
            RequiredGlyph = filteredSettings.RequiredGlyph,
            AllowedWeights = filteredSettings.AllowedWeights,
            AllowedWidths = filteredSettings.AllowedWidths,
            AllowedSlants = filteredSettings.AllowedSlants
        };

        return SystemFontVariantResolver.Resolve(settings, configDirectory);
    }

    private static List<ResolvedSystemFontVariant> ResolveFamilyOnlyVariants(List<string> includeGroups, string configDirectory)
    {
        var settings = new RandomSystemFontProviderSettings
        {
            IncludeGroups = includeGroups,
            AllowedWeights = [],
            AllowedWidths = [],
            AllowedSlants = []
        };

        var allVariants = SystemFontVariantResolver.Resolve(settings, configDirectory);
        var preferredVariants = allVariants
            .GroupBy(variant => variant.FamilyName, StringComparer.OrdinalIgnoreCase)
            .Select(grouping => ChooseRepresentativeVariant(grouping.ToList()))
            .ToList();

        return preferredVariants;
    }

    private static ResolvedSystemFontVariant ChooseRepresentativeVariant(List<ResolvedSystemFontVariant> variants)
    {
        var regularVariant = variants.FirstOrDefault(variant =>
            (SKFontStyleWeight)variant.Style.Weight == SKFontStyleWeight.Normal
            && (SKFontStyleWidth)variant.Style.Width == SKFontStyleWidth.Normal
            && variant.Style.Slant == SKFontStyleSlant.Upright);

        if (regularVariant != null)
        {
            return regularVariant;
        }

        return variants[0];
    }

    private static int CalculateImageHeight(int missingFamilyCount, int foundFontCount)
    {
        var missingSectionHeight = 0;
        if (missingFamilyCount > 0)
        {
            missingSectionHeight = 70 + missingFamilyCount * 28;
        }

        var imageHeight = Margin + TitleHeight + foundFontCount * RowHeight + missingSectionHeight + Margin;
        return Math.Max(imageHeight, 240);
    }

    private static void DrawHeader(SKCanvas canvas, FontGroupDefinition group, ResolvedGroupFonts resolvedFonts, string sampleText)
    {
        var titleBaseline = Margin + 42;
        DrawText(canvas, group.Name, Margin, titleBaseline, SKTypeface.Default, TitleFontSize, new SKColor(30, 30, 30));

        var subtitle = $"Found: {resolvedFonts.FoundFonts.Count} / Declared: {group.Families.Count} / Missing: {resolvedFonts.MissingFamilies.Count}";
        DrawText(canvas, subtitle, Margin, titleBaseline + 34, SKTypeface.Default, SubtitleFontSize, new SKColor(90, 90, 90));

        var sampleLabel = $"Sample text: {sampleText}";
        DrawText(canvas, sampleLabel, Margin, titleBaseline + 66, SKTypeface.Default, SubtitleFontSize, new SKColor(90, 90, 90));

        using var separatorPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(220, 220, 220),
            StrokeWidth = 1
        };

        var separatorY = Margin + TitleHeight - 8;
        canvas.DrawLine(Margin, separatorY, ImageWidth - Margin, separatorY, separatorPaint);
    }

    private static void DrawRows(SKCanvas canvas, IReadOnlyList<ResolvedFontPreview> foundFonts, string sampleText)
    {
        using var separatorPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(235, 235, 235),
            StrokeWidth = 1
        };

        var sampleStartX = Margin + LabelColumnWidth + ColumnGap;
        for (int index = 0; index < foundFonts.Count; index++)
        {
            var rowTop = Margin + TitleHeight + index * RowHeight;
            var rowBaseline = rowTop + 56;

            if (index > 0)
            {
                canvas.DrawLine(Margin, rowTop, ImageWidth - Margin, rowTop, separatorPaint);
            }

            var resolvedFont = foundFonts[index];
            DrawText(canvas, resolvedFont.FamilyName, Margin, rowBaseline, SKTypeface.Default, LabelFontSize, new SKColor(70, 70, 70));
            DrawText(canvas, sampleText, sampleStartX, rowBaseline, resolvedFont.Typeface, resolvedFont.FontSize, SKColors.Black);
        }
    }

    private static void DrawMissingFamilies(SKCanvas canvas, int foundFontCount, IReadOnlyList<string> missingFamilies)
    {
        if (missingFamilies.Count == 0)
        {
            return;
        }

        var sectionTitleY = Margin + TitleHeight + foundFontCount * RowHeight + 32;
        DrawText(canvas, "Missing families in current system:", Margin, sectionTitleY, SKTypeface.Default, 22, new SKColor(170, 40, 40));

        var startY = sectionTitleY + 34;
        for (int index = 0; index < missingFamilies.Count; index++)
        {
            var lineY = startY + index * 28;
            DrawText(canvas, missingFamilies[index], Margin, lineY, SKTypeface.Default, 20, new SKColor(120, 60, 60));
        }
    }

    private static float CalculateSampleFontSize(SKTypeface typeface, string sampleText)
    {
        var availableWidth = ImageWidth - Margin - LabelColumnWidth - ColumnGap - Margin;
        if (availableWidth <= 0)
        {
            return BaseSampleFontSize;
        }

        using var font = new SKFont(typeface, BaseSampleFontSize);
        var measuredWidth = font.MeasureText(sampleText);
        if (measuredWidth <= 0)
        {
            return BaseSampleFontSize;
        }

        var scaledFontSize = BaseSampleFontSize * availableWidth / measuredWidth;
        var clampedFontSize = Math.Clamp(scaledFontSize, MinSampleFontSize, BaseSampleFontSize);
        return clampedFontSize;
    }

    private static string SanitizeFileName(string groupName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitizedChars = groupName
            .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
            .ToArray();

        return new string(sanitizedChars);
    }

    private static void DrawText(SKCanvas canvas, string text, float x, float y, SKTypeface typeface, float fontSize, SKColor color)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = color
        };

        using var font = new SKFont(typeface, fontSize);
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private sealed record ResolvedGroupFonts(
        List<ResolvedFontPreview> FoundFonts,
        List<string> MissingFamilies);

    private sealed record ResolvedFontPreview(
        string FamilyName,
        SKTypeface Typeface,
        float FontSize);
}
