using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SkiaSharp;

namespace ObbTextGenerator;

public sealed class ContextDumpFormatter
{
    private const string SectionSeparator = "================================================================================";
    private readonly WriteContextDumpStageSettings _settings;

    public ContextDumpFormatter(WriteContextDumpStageSettings settings)
    {
        _settings = settings;
    }

    public string Format(RenderContext context)
    {
        var builder = new StringBuilder(32 * 1024);

        AppendHeader(builder, context);
        AppendSummaryTables(builder, context);
        AppendFullTree(builder, "RenderContext.Tree", context);

        if (_settings.IncludeSettingsTree)
        {
            AppendFullTree(builder, "RenderContext.Settings.Tree", context.Settings);
        }

        if (_settings.IncludeTextLinesTree)
        {
            AppendFullTree(builder, "RenderContext.TextLines.Tree", context.TextLines);
        }

        if (_settings.IncludeAnnotationLayersTree)
        {
            AppendFullTree(builder, "RenderContext.AnnotationLayers.Tree", context.AnnotationLayers);
        }

        if (_settings.IncludeSampleDataTree)
        {
            AppendFullTree(builder, "RenderContext.SampleData.Tree", context.SampleData);
        }

        if (_settings.IncludeTraceTree)
        {
            AppendFullTree(builder, "RenderContext.TraceEntries.Tree", context.TraceEntries);
        }

        return builder.ToString();
    }

    private void AppendHeader(StringBuilder builder, RenderContext context)
    {
        builder.AppendLine("OBB TEXT GENERATOR CONTEXT DUMP");
        builder.AppendLine("FormatVersion: 1");
        builder.AppendLine("Purpose: Human-readable snapshot of the current RenderContext state.");
        builder.AppendLine(SectionSeparator);
        builder.AppendLine();
        builder.AppendLine($"SampleIndex: {context.SampleIndex}");
        builder.AppendLine($"SetName: {context.SetName}");
        builder.AppendLine($"CanvasSize: {context.Width} x {context.Height}");
        builder.AppendLine($"TextLineCount: {context.TextLines.Count}");
        builder.AppendLine($"AnnotationLayerCount: {context.AnnotationLayers.Count}");
        builder.AppendLine($"SampleDataCount: {context.SampleData.Count}");
        builder.AppendLine($"TraceEntryCount: {context.TraceEntries.Count}");
        builder.AppendLine();
    }

    private void AppendSummaryTables(StringBuilder builder, RenderContext context)
    {
        AppendKeyValueTable(builder, "Context Summary", [
            ("SampleIndex", context.SampleIndex.ToString(CultureInfo.InvariantCulture)),
            ("SetName", context.SetName),
            ("Width", context.Width.ToString(CultureInfo.InvariantCulture)),
            ("Height", context.Height.ToString(CultureInfo.InvariantCulture)),
            ("HasParentContext", (context.ParentContext is not null).ToString()),
            ("ActivePatternName", context.ActivePatternName),
            ("ActiveScheme", context.ActiveScheme?.Name ?? "<null>"),
            ("TextLines", context.TextLines.Count.ToString(CultureInfo.InvariantCulture)),
            ("AnnotationLayers", context.AnnotationLayers.Count.ToString(CultureInfo.InvariantCulture)),
            ("SampleDataItems", context.SampleData.Count.ToString(CultureInfo.InvariantCulture)),
            ("TraceEntries", context.TraceEntries.Count.ToString(CultureInfo.InvariantCulture))
        ]);

        AppendTextLinesTable(builder, context);
        AppendAnnotationLayersTable(builder, context);
        AppendSampleDataTable(builder, context);
        AppendTraceTable(builder, context);
    }

    private void AppendTextLinesTable(StringBuilder builder, RenderContext context)
    {
        var rows = new List<string[]>();
        for (var index = 0; index < context.TextLines.Count; index++)
        {
            var textLine = context.TextLines[index];
            rows.Add([
                index.ToString(CultureInfo.InvariantCulture),
                textLine.ClassId.ToString(CultureInfo.InvariantCulture),
                TrimForTable(textLine.Text, 48),
                FormatPoint(textLine.Origin),
                textLine.Rotation.ToString("0.###", CultureInfo.InvariantCulture),
                textLine.BlockId,
                $"{textLine.LineIndexInBlock + 1}/{textLine.LineCountInBlock}"
            ]);
        }

        AppendTable(builder, "TextLines", ["#", "ClassId", "Text", "Origin", "Rotation", "BlockId", "Line"], rows);
    }

    private void AppendAnnotationLayersTable(StringBuilder builder, RenderContext context)
    {
        var rows = new List<string[]>();
        foreach (var pair in context.AnnotationLayers.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            rows.Add([
                pair.Key,
                pair.Value.Annotations.Count.ToString(CultureInfo.InvariantCulture),
                SummarizeValue(pair.Value)
            ]);
        }

        AppendTable(builder, "Annotation Layers", ["Layer", "Annotations", "Summary"], rows);
    }

    private void AppendSampleDataTable(StringBuilder builder, RenderContext context)
    {
        var rows = new List<string[]>();
        foreach (var pair in context.SampleData.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            rows.Add([
                pair.Key,
                pair.Value.GetType().Name,
                SummarizeValue(pair.Value)
            ]);
        }

        AppendTable(builder, "Sample Data", ["Key", "Type", "Summary"], rows);
    }

    private void AppendTraceTable(StringBuilder builder, RenderContext context)
    {
        var rows = new List<string[]>();
        for (var index = 0; index < context.TraceEntries.Count; index++)
        {
            var traceEntry = context.TraceEntries[index];
            rows.Add([
                index.ToString(CultureInfo.InvariantCulture),
                traceEntry.Depth.ToString(CultureInfo.InvariantCulture),
                traceEntry.Summary,
                traceEntry.Details ?? string.Empty
            ]);
        }

        AppendTable(builder, "Trace Entries", ["#", "Depth", "Summary", "Details"], rows);
    }

    private void AppendKeyValueTable(StringBuilder builder, string title, IReadOnlyList<(string Key, string Value)> items)
    {
        var rows = items
            .Select(static item => new[] { item.Key, item.Value })
            .ToList();

        AppendTable(builder, title, ["Key", "Value"], rows);
    }

    private void AppendTable(StringBuilder builder, string title, IReadOnlyList<string> columns, IReadOnlyList<string[]> rows)
    {
        builder.AppendLine(SectionSeparator);
        builder.AppendLine(title);
        builder.AppendLine(SectionSeparator);

        if (rows.Count == 0)
        {
            builder.AppendLine("<empty>");
            builder.AppendLine();
            return;
        }

        var widths = new int[columns.Count];
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            widths[columnIndex] = columns[columnIndex].Length;
        }

        foreach (var row in rows)
        {
            for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
            {
                var value = columnIndex < row.Length ? row[columnIndex] : string.Empty;
                widths[columnIndex] = Math.Max(widths[columnIndex], value.Length);
            }
        }

        builder.AppendLine(BuildTableRow(columns, widths));
        builder.AppendLine(BuildSeparatorRow(widths));
        foreach (var row in rows)
        {
            builder.AppendLine(BuildTableRow(row, widths));
        }

        builder.AppendLine();
    }

    private static string BuildTableRow(IReadOnlyList<string> values, IReadOnlyList<int> widths)
    {
        var parts = new string[widths.Count];
        for (var columnIndex = 0; columnIndex < widths.Count; columnIndex++)
        {
            var value = columnIndex < values.Count ? values[columnIndex] : string.Empty;
            parts[columnIndex] = value.PadRight(widths[columnIndex], ' ');
        }

        return $"| {string.Join(" | ", parts)} |";
    }

    private static string BuildSeparatorRow(IReadOnlyList<int> widths)
    {
        var parts = widths
            .Select(static width => new string('-', width))
            .ToArray();

        return $"|-{string.Join("-|-", parts)}-|";
    }

    private void AppendFullTree(StringBuilder builder, string title, object? value)
    {
        builder.AppendLine(SectionSeparator);
        builder.AppendLine(title);
        builder.AppendLine(SectionSeparator);

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        AppendNode(builder, title, value, string.Empty, true, 0, visited);
        builder.AppendLine();
    }

    private void AppendNode(
        StringBuilder builder,
        string nodeName,
        object? value,
        string indent,
        bool isLast,
        int depth,
        HashSet<object> visited)
    {
        var branchPrefix = depth == 0 ? string.Empty : (isLast ? "\\- " : "|- ");
        var nodeValue = DescribeNodeValue(value);
        builder.Append(indent);
        builder.Append(branchPrefix);
        builder.Append(nodeName);
        builder.Append(": ");
        builder.AppendLine(nodeValue);

        if (value is null)
        {
            return;
        }

        if (depth >= _settings.MaxDepth)
        {
            AppendChildLine(builder, indent, isLast, $"<max-depth:{_settings.MaxDepth}>");
            return;
        }

        if (!ShouldExpand(value))
        {
            return;
        }

        if (!value.GetType().IsValueType)
        {
            if (!visited.Add(value))
            {
                AppendChildLine(builder, indent, isLast, "<cycle-detected>");
                return;
            }
        }

        var childIndent = depth == 0
            ? string.Empty
            : indent + (isLast ? "   " : "|  ");
        var children = GetChildren(value).ToList();
        for (var childIndex = 0; childIndex < children.Count; childIndex++)
        {
            var child = children[childIndex];
            var childIsLast = childIndex == children.Count - 1;
            AppendNode(builder, child.Name, child.Value, childIndent, childIsLast, depth + 1, visited);
        }
    }

    private IEnumerable<(string Name, object? Value)> GetChildren(object value)
    {
        if (value is IDictionary dictionary)
        {
            var itemIndex = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (itemIndex >= _settings.MaxCollectionItems)
                {
                    yield return ("<truncated>", $"{dictionary.Count} total items");
                    yield break;
                }

                yield return ($"[{entry.Key}]", entry.Value);
                itemIndex++;
            }

            yield break;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var itemIndex = 0;
            foreach (var item in enumerable)
            {
                if (itemIndex >= _settings.MaxCollectionItems)
                {
                    yield return ("<truncated>", $"max {_settings.MaxCollectionItems} items shown");
                    yield break;
                }

                yield return ($"[{itemIndex}]", item);
                itemIndex++;
            }

            yield break;
        }

        foreach (var property in GetInspectableProperties(value.GetType()))
        {
            object? propertyValue;
            try
            {
                propertyValue = property.GetValue(value);
            }
            catch (Exception exception)
            {
                propertyValue = $"<unavailable: {exception.GetType().Name}>";
            }

            yield return (property.Name, propertyValue);
        }
    }

    private static IEnumerable<PropertyInfo> GetInspectableProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.GetIndexParameters().Length == 0)
            .OrderBy(static property => property.Name, StringComparer.Ordinal);
    }

    private static bool ShouldExpand(object value)
    {
        var type = value.GetType();

        if (IsSimple(type))
        {
            return false;
        }

        if (type == typeof(SKBitmap) || type == typeof(SKCanvas) || type == typeof(SKPaint) || type == typeof(SKFont))
        {
            return false;
        }

        return true;
    }

    private static bool IsSimple(Type type)
    {
        if (type.IsPrimitive || type.IsEnum)
        {
            return true;
        }

        return type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(SKPoint)
            || type == typeof(SKRect)
            || type == typeof(SKMatrix)
            || type == typeof(SKColor)
            || type == typeof(Type);
    }

    private static string DescribeNodeValue(object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        return value switch
        {
            string text => $"\"{text}\"",
            bool flag => flag ? "true" : "false",
            IFormattable formattable when value is not SKPoint && value is not SKRect && value is not SKColor => formattable.ToString(null, CultureInfo.InvariantCulture),
            SKPoint point => FormatPoint(point),
            SKRect rect => FormatRect(rect),
            SKColor color => $"rgba({color.Red}, {color.Green}, {color.Blue}, {color.Alpha}) #{color.Red:X2}{color.Green:X2}{color.Blue:X2}{color.Alpha:X2}",
            SKMatrix matrix => FormatMatrix(matrix),
            Type type => type.FullName ?? type.Name,
            _ => SummarizeValue(value)
        };
    }

    private static string SummarizeValue(object value)
    {
        return value switch
        {
            SKBitmap bitmap => $"SKBitmap {{ Width = {bitmap.Width}, Height = {bitmap.Height}, ColorType = {bitmap.ColorType}, AlphaType = {bitmap.AlphaType} }}",
            SKCanvas => "SKCanvas { opaque runtime canvas }",
            SKPaint paint => $"SKPaint {{ Color = rgba({paint.Color.Red}, {paint.Color.Green}, {paint.Color.Blue}, {paint.Color.Alpha}), Style = {paint.Style}, StrokeWidth = {paint.StrokeWidth.ToString("0.###", CultureInfo.InvariantCulture)} }}",
            SKFont font => $"SKFont {{ Size = {font.Size.ToString("0.###", CultureInfo.InvariantCulture)}, Typeface = {font.Typeface?.FamilyName ?? "<null>"} }}",
            IDictionary dictionary => $"{value.GetType().Name} (Count = {dictionary.Count})",
            IEnumerable enumerable when value is not string => $"{value.GetType().Name} (Count = {TryGetCount(enumerable)})",
            _ => value.ToString() ?? value.GetType().Name
        };
    }

    private static int TryGetCount(IEnumerable enumerable)
    {
        return enumerable switch
        {
            ICollection collection => collection.Count,
            _ => -1
        };
    }

    private static string FormatPoint(SKPoint point)
    {
        return $"({point.X.ToString("0.###", CultureInfo.InvariantCulture)}, {point.Y.ToString("0.###", CultureInfo.InvariantCulture)})";
    }

    private static string FormatRect(SKRect rect)
    {
        return $"L={rect.Left.ToString("0.###", CultureInfo.InvariantCulture)}, T={rect.Top.ToString("0.###", CultureInfo.InvariantCulture)}, R={rect.Right.ToString("0.###", CultureInfo.InvariantCulture)}, B={rect.Bottom.ToString("0.###", CultureInfo.InvariantCulture)}, W={rect.Width.ToString("0.###", CultureInfo.InvariantCulture)}, H={rect.Height.ToString("0.###", CultureInfo.InvariantCulture)}";
    }

    private static string FormatMatrix(SKMatrix matrix)
    {
        return $"[{matrix.ScaleX.ToString("0.###", CultureInfo.InvariantCulture)}, {matrix.SkewX.ToString("0.###", CultureInfo.InvariantCulture)}, {matrix.TransX.ToString("0.###", CultureInfo.InvariantCulture)}; {matrix.SkewY.ToString("0.###", CultureInfo.InvariantCulture)}, {matrix.ScaleY.ToString("0.###", CultureInfo.InvariantCulture)}, {matrix.TransY.ToString("0.###", CultureInfo.InvariantCulture)}; {matrix.Persp0.ToString("0.###", CultureInfo.InvariantCulture)}, {matrix.Persp1.ToString("0.###", CultureInfo.InvariantCulture)}, {matrix.Persp2.ToString("0.###", CultureInfo.InvariantCulture)}]";
    }

    private static string TrimForTable(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..(maxLength - 3)] + "...";
    }

    private static void AppendChildLine(StringBuilder builder, string indent, bool isLast, string text)
    {
        var childIndent = indent + (isLast ? "   " : "|  ");
        builder.Append(childIndent);
        builder.AppendLine("\\- " + text);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
