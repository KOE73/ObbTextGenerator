using System.Globalization;

namespace ObbTextGenerator;

public static class InvariantNumberExtensions
{
    public static string ToStringInvariant(this float value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
