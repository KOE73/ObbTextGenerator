using System.Globalization;
using System.Text.RegularExpressions;

namespace ObbTextGenerator;

public static class SampledValueParser
{
    private static readonly Regex SymmetricRegex = new(
        "^(?<base>[+-]?\\d+(?:\\.\\d+)?%?)\\+\\-(?<delta>\\d+(?:\\.\\d+)?)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AsymmetricRegex = new(
        "^(?<base>[+-]?\\d+(?:\\.\\d+)?%?)-(?<lower>\\d+(?:\\.\\d+)?)\\+(?<upper>\\d+(?:\\.\\d+)?)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex EndpointRegex = new(
        "^(?<base>[+-]?\\d+(?:\\.\\d+)?)(?<percent>%?)(?<offset>[+-]\\d+(?:\\.\\d+)?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static SampledValueSpec Parse(string text)
    {
        if (!TryParse(text, out var spec))
        {
            throw new FormatException($"Invalid sampled value expression '{text}'.");
        }

        return spec;
    }

    public static bool TryParse(string? text, out SampledValueSpec spec)
    {
        spec = SampledValueSpec.FromAbsolute(0f);

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);

        if (TryParseRange(normalized, out spec))
        {
            return true;
        }

        if (TryParseSymmetric(normalized, out spec))
        {
            return true;
        }

        if (TryParseAsymmetric(normalized, out spec))
        {
            return true;
        }

        if (!TryParseEndpoint(normalized, out var endpoint))
        {
            return false;
        }

        spec = new SampledValueSpec(endpoint, endpoint, normalized);
        return true;
    }

    private static bool TryParseRange(string normalized, out SampledValueSpec spec)
    {
        spec = SampledValueSpec.FromAbsolute(0f);

        var rangeSeparatorIndex = normalized.IndexOf("..", StringComparison.Ordinal);
        if (rangeSeparatorIndex < 0)
        {
            return false;
        }

        var leftText = normalized[..rangeSeparatorIndex];
        var rightText = normalized[(rangeSeparatorIndex + 2)..];
        if (!TryParseEndpoint(leftText, out var minimum) || !TryParseEndpoint(rightText, out var maximum))
        {
            return false;
        }

        spec = new SampledValueSpec(minimum, maximum, normalized);
        return true;
    }

    private static bool TryParseSymmetric(string normalized, out SampledValueSpec spec)
    {
        spec = SampledValueSpec.FromAbsolute(0f);

        var match = SymmetricRegex.Match(normalized);
        if (!match.Success)
        {
            return false;
        }

        if (!TryParseEndpoint(match.Groups["base"].Value, out var baseEndpoint))
        {
            return false;
        }

        if (!float.TryParse(match.Groups["delta"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var delta))
        {
            return false;
        }

        var minimum = baseEndpoint with { Offset = baseEndpoint.Offset - delta };
        var maximum = baseEndpoint with { Offset = baseEndpoint.Offset + delta };
        spec = new SampledValueSpec(minimum, maximum, normalized);
        return true;
    }

    private static bool TryParseAsymmetric(string normalized, out SampledValueSpec spec)
    {
        spec = SampledValueSpec.FromAbsolute(0f);

        var match = AsymmetricRegex.Match(normalized);
        if (!match.Success)
        {
            return false;
        }

        if (!TryParseEndpoint(match.Groups["base"].Value, out var baseEndpoint))
        {
            return false;
        }

        if (!float.TryParse(match.Groups["lower"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lower))
        {
            return false;
        }

        if (!float.TryParse(match.Groups["upper"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var upper))
        {
            return false;
        }

        var minimum = baseEndpoint with { Offset = baseEndpoint.Offset - lower };
        var maximum = baseEndpoint with { Offset = baseEndpoint.Offset + upper };
        spec = new SampledValueSpec(minimum, maximum, normalized);
        return true;
    }

    private static bool TryParseEndpoint(string normalized, out SampledValueEndpoint endpoint)
    {
        endpoint = new SampledValueEndpoint(0f, false, 0f);

        var match = EndpointRegex.Match(normalized);
        if (!match.Success)
        {
            return false;
        }

        if (!float.TryParse(match.Groups["base"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var baseValue))
        {
            return false;
        }

        var isPercent = match.Groups["percent"].Value == "%";

        var offset = 0f;
        var offsetGroup = match.Groups["offset"].Value;
        if (!string.IsNullOrWhiteSpace(offsetGroup)
            && !float.TryParse(offsetGroup, NumberStyles.Float, CultureInfo.InvariantCulture, out offset))
        {
            return false;
        }

        endpoint = new SampledValueEndpoint(baseValue, isPercent, offset);
        return true;
    }
}
