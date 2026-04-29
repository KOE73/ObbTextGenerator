using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Spectre.Console;
using Spectre.Console.Rendering;
using SkiaSharp;

namespace ObbTextGenerator;

public sealed class SpectreContextDumpFormatter
{
    private const string SectionSeparator = "================================================================================";
    private readonly WriteContextDumpStageSettings _settings;

    public SpectreContextDumpFormatter(WriteContextDumpStageSettings settings)
    {
        _settings = settings;
    }

    public string Format(RenderContext context)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer)
        });

        AppendHeader(writer, context);
        AppendSummaryTables(console, writer, context);
        AppendFullTree(console, writer, "RenderContext.Tree", context);

        if (_settings.IncludeSettingsTree)
        {
            AppendFullTree(console, writer, "RenderContext.Settings.Tree", context.Settings);
        }

        if (_settings.IncludeTextLinesTree)
        {
            AppendFullTree(console, writer, "RenderContext.TextLines.Tree", context.TextLines);
        }

        if (_settings.IncludeAnnotationLayersTree)
        {
            AppendFullTree(console, writer, "RenderContext.AnnotationLayers.Tree", context.AnnotationLayers);
        }

        if (_settings.IncludeSampleDataTree)
        {
            AppendFullTree(console, writer, "RenderContext.SampleData.Tree", context.SampleData);
        }

        if (_settings.IncludeTraceTree)
        {
            AppendFullTree(console, writer, "RenderContext.TraceEntries.Tree", context.TraceEntries);
        }

        writer.Flush();
        return writer.ToString();
    }

    private static void AppendHeader(TextWriter writer, RenderContext context)
    {
        writer.WriteLine("OBB TEXT GENERATOR CONTEXT DUMP");
        writer.WriteLine("FormatVersion: 1");
        writer.WriteLine("Purpose: Human-readable snapshot of the current RenderContext state.");
        writer.WriteLine(SectionSeparator);
        writer.WriteLine();
        writer.WriteLine($"SampleIndex: {context.SampleIndex}");
        writer.WriteLine($"SetName: {context.SetName}");
        writer.WriteLine($"CanvasSize: {context.Width} x {context.Height}");
        writer.WriteLine($"TextLineCount: {context.TextLines.Count}");
        writer.WriteLine($"AnnotationLayerCount: {context.AnnotationLayers.Count}");
        writer.WriteLine($"SampleDataCount: {context.SampleData.Count}");
        writer.WriteLine($"TraceEntryCount: {context.TraceEntries.Count}");
        writer.WriteLine();
    }

    private void AppendSummaryTables(IAnsiConsole console, TextWriter writer, RenderContext context)
    {
        var summaryTable = CreateTable("Context Summary", ["Key", "Value"]);
        AddRow(summaryTable, "SampleIndex", context.SampleIndex.ToString(CultureInfo.InvariantCulture));
        AddRow(summaryTable, "SetName", context.SetName);
        AddRow(summaryTable, "Width", context.Width.ToString(CultureInfo.InvariantCulture));
        AddRow(summaryTable, "Height", context.Height.ToString(CultureInfo.InvariantCulture));
        AddRow(summaryTable, "HasParentContext", (context.ParentContext is not null).ToString());
        AddRow(summaryTable, "ActivePatternName", context.ActivePatternName);
        AddRow(summaryTable, "ActiveScheme", context.ActiveScheme?.Name ?? "<null>");
        AddRow(summaryTable, "TextLines", context.TextLines.Count.ToString(CultureInfo.InvariantCulture));
        AddRow(summaryTable, "AnnotationLayers", context.AnnotationLayers.Count.ToString(CultureInfo.InvariantCulture));
        AddRow(summaryTable, "SampleDataItems", context.SampleData.Count.ToString(CultureInfo.InvariantCulture));
        AddRow(summaryTable, "TraceEntries", context.TraceEntries.Count.ToString(CultureInfo.InvariantCulture));
        AppendRenderable(console, writer, summaryTable);

        var textLinesTable = CreateTable("TextLines", ["#", "ClassId", "Text", "Origin", "Rotation", "BlockId", "Line"]);
        for (var index = 0; index < context.TextLines.Count; index++)
        {
            var textLine = context.TextLines[index];
            AddRow(
                textLinesTable,
                index.ToString(CultureInfo.InvariantCulture),
                textLine.ClassId.ToString(CultureInfo.InvariantCulture),
                TrimForTable(textLine.Text, 48),
                FormatPoint(textLine.Origin),
                textLine.Rotation.ToString("0.###", CultureInfo.InvariantCulture),
                textLine.BlockId,
                $"{textLine.LineIndexInBlock + 1}/{textLine.LineCountInBlock}");
        }
        EnsureNotEmpty(textLinesTable);
        AppendRenderable(console, writer, textLinesTable);

        var annotationLayersTable = CreateTable("Annotation Layers", ["Layer", "Annotations", "Summary"]);
        foreach (var pair in context.AnnotationLayers.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            AddRow(annotationLayersTable, pair.Key, pair.Value.Annotations.Count.ToString(CultureInfo.InvariantCulture), SummarizeValue(pair.Value));
        }
        EnsureNotEmpty(annotationLayersTable);
        AppendRenderable(console, writer, annotationLayersTable);

        var sampleDataTable = CreateTable("Sample Data", ["Key", "Type", "Summary"]);
        foreach (var pair in context.SampleData.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            AddRow(sampleDataTable, pair.Key, pair.Value.GetType().Name, SummarizeValue(pair.Value));
        }
        EnsureNotEmpty(sampleDataTable);
        AppendRenderable(console, writer, sampleDataTable);

        var traceTable = CreateTable("Trace Entries", ["#", "Depth", "Summary", "Details"]);
        for (var index = 0; index < context.TraceEntries.Count; index++)
        {
            var traceEntry = context.TraceEntries[index];
            AddRow(traceTable, index.ToString(CultureInfo.InvariantCulture), traceEntry.Depth.ToString(CultureInfo.InvariantCulture), traceEntry.Summary, traceEntry.Details ?? string.Empty);
        }
        EnsureNotEmpty(traceTable);
        AppendRenderable(console, writer, traceTable);
    }

    private void AppendFullTree(IAnsiConsole console, TextWriter writer, string title, object? value)
    {
        writer.WriteLine(SectionSeparator);
        writer.WriteLine(title);
        writer.WriteLine(SectionSeparator);

        var tree = new Tree(Markup.Escape($"{title}: {DescribeNodeValue(value)}"));
        tree.Guide(ResolveTreeGuide());

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        AppendChildNodes(tree, value, 0, visited);

        AppendRenderable(console, writer, tree);
    }

    private void AppendChildNodes(Tree tree, object? value, int depth, HashSet<object> visited)
    {
        foreach (var child in GetChildrenForNode(value, depth, visited))
        {
            var childNode = tree.AddNode(Markup.Escape($"{child.Name}: {DescribeNodeValue(child.Value)}"));
            AppendNodeChildren(childNode, child.Value, depth + 1, visited);
        }
    }

    private void AppendNodeChildren(TreeNode parentNode, object? value, int depth, HashSet<object> visited)
    {
        foreach (var child in GetChildrenForNode(value, depth, visited))
        {
            var childNode = parentNode.AddNode(Markup.Escape($"{child.Name}: {DescribeNodeValue(child.Value)}"));
            AppendNodeChildren(childNode, child.Value, depth + 1, visited);
        }
    }

    private IEnumerable<(string Name, object? Value)> GetChildrenForNode(object? value, int depth, HashSet<object> visited)
    {
        if (value is null)
        {
            yield break;
        }

        if (depth >= _settings.MaxDepth)
        {
            yield return ("<limit>", $"<max-depth:{_settings.MaxDepth}>");
            yield break;
        }

        if (!ShouldExpand(value))
        {
            yield break;
        }

        if (!value.GetType().IsValueType)
        {
            if (!visited.Add(value))
            {
                yield return ("<state>", "<cycle-detected>");
                yield break;
            }
        }

        foreach (var child in GetChildren(value))
        {
            yield return child;
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

        foreach (var property in value.GetType()
                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(static property => property.GetIndexParameters().Length == 0)
                     .OrderBy(static property => property.Name, StringComparer.Ordinal))
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

    private Table CreateTable(string title, IReadOnlyList<string> columns)
    {
        var table = new Table();
        table.Border(ResolveTableBorder());
        table.Title(Markup.Escape(title));

        foreach (var column in columns)
        {
            table.AddColumn(Markup.Escape(column));
        }

        return table;
    }

    private static void AddRow(Table table, params string[] values)
    {
        table.AddRow(values.Select(static value => Markup.Escape(value)).ToArray());
    }

    private static void EnsureNotEmpty(Table table)
    {
        if (table.Rows.Count > 0)
        {
            return;
        }

        var emptyCells = new string[table.Columns.Count];
        for (var index = 0; index < emptyCells.Length; index++)
        {
            emptyCells[index] = index == 0 ? "<empty>" : string.Empty;
        }

        AddRow(table, emptyCells);
    }

    private static void AppendRenderable(IAnsiConsole console, TextWriter writer, IRenderable renderable)
    {
        console.Write(renderable);
        writer.WriteLine();
        writer.WriteLine();
    }

    private TableBorder ResolveTableBorder()
    {
        return _settings.TableBorderStyle switch
        {
            ContextDumpTableBorderStyle.Rounded => TableBorder.Rounded,
            _ => TableBorder.Ascii
        };
    }

    private TreeGuide ResolveTreeGuide()
    {
        return _settings.TreeGuideStyle switch
        {
            ContextDumpTreeGuideStyle.Line => TreeGuide.Line,
            _ => TreeGuide.Ascii
        };
    }

    private static bool ShouldExpand(object value)
    {
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum)
        {
            return false;
        }

        if (type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(SKPoint)
            || type == typeof(SKRect)
            || type == typeof(SKMatrix)
            || type == typeof(SKColor)
            || type == typeof(Type)
            || type == typeof(SKBitmap)
            || type == typeof(SKCanvas)
            || type == typeof(SKPaint)
            || type == typeof(SKFont))
        {
            return false;
        }

        return true;
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
