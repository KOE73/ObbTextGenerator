namespace ObbTextGenerator;

public readonly record struct SampledValueEndpoint(float BaseValue, bool IsPercent, float Offset)
{
    public float Resolve(float reference)
    {
        var baseResult = IsPercent
            ? reference * (BaseValue / 100f)
            : BaseValue;

        return baseResult + Offset;
    }

    public override string ToString()
    {
        var baseText = BaseValue.ToStringInvariant();
        if (IsPercent)
        {
            baseText += "%";
        }

        if (Offset > 0f)
        {
            return $"{baseText}+{Offset.ToStringInvariant()}";
        }

        if (Offset < 0f)
        {
            return $"{baseText}{Offset.ToStringInvariant()}";
        }

        return baseText;
    }
}
