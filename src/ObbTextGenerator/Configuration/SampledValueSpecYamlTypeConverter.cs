using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace ObbTextGenerator;

public sealed class SampledValueSpecYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(SampledValueSpec);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        if (!parser.TryConsume<Scalar>(out var scalar))
        {
            throw new YamlException("SampledValueSpec expects a scalar YAML value.");
        }

        return SampledValueSpec.Parse(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {
        var spec = value as SampledValueSpec ?? SampledValueSpec.FromAbsolute(0f);
        emitter.Emit(new Scalar(spec.Expression));
    }
}
