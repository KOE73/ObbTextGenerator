namespace ObbTextGenerator;

public sealed class SampledValueSpec
{
    public static SampledValueSpec Parse(string text)
    {
        return SampledValueParser.Parse(text);
    }

    public static SampledValueSpec FromAbsolute(float value)
    {
        return new SampledValueSpec(new SampledValueEndpoint(value, false, 0f), new SampledValueEndpoint(value, false, 0f), value.ToStringInvariant());
    }

    public static SampledValueSpec FromAbsoluteRange(float minimum, float maximum)
    {
        var minEndpoint = new SampledValueEndpoint(minimum, false, 0f);
        var maxEndpoint = new SampledValueEndpoint(maximum, false, 0f);
        return new SampledValueSpec(minEndpoint, maxEndpoint, $"{minimum.ToStringInvariant()}..{maximum.ToStringInvariant()}");
    }

    public static SampledValueSpec FromSymmetricAbsolute(float magnitude)
    {
        var text = $"0+-{magnitude.ToStringInvariant()}";
        return Parse(text);
    }

    public SampledValueSpec(SampledValueEndpoint minimum, SampledValueEndpoint maximum, string expression)
    {
        Minimum = minimum;
        Maximum = maximum;
        Expression = expression;
    }

    public SampledValueEndpoint Minimum { get; }

    public SampledValueEndpoint Maximum { get; }

    public string Expression { get; }

    public float ResolveMinimum(float reference = 0f)
    {
        return Minimum.Resolve(reference);
    }

    public float ResolveMaximum(float reference = 0f)
    {
        return Maximum.Resolve(reference);
    }

    public float ResolveValue(float reference = 0f)
    {
        return ResolveMinimum(reference);
    }

    public float Sample(Random random, float reference = 0f)
    {
        var minimum = ResolveMinimum(reference);
        var maximum = ResolveMaximum(reference);
        if (maximum < minimum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        if (maximum <= minimum)
        {
            return minimum;
        }

        return (float)(random.NextDouble() * (maximum - minimum) + minimum);
    }

    public int SampleInt(Random random, float reference = 0f)
    {
        var minimum = ResolveMinimum(reference);
        var maximum = ResolveMaximum(reference);
        if (maximum < minimum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        var minimumInteger = (int)MathF.Ceiling(minimum);
        var maximumInteger = (int)MathF.Floor(maximum);
        if (maximumInteger < minimumInteger)
        {
            return (int)MathF.Round((minimum + maximum) / 2f, MidpointRounding.AwayFromZero);
        }

        if (maximumInteger == minimumInteger)
        {
            return minimumInteger;
        }

        return random.Next(minimumInteger, maximumInteger + 1);
    }

    public override string ToString()
    {
        return Expression;
    }
}
