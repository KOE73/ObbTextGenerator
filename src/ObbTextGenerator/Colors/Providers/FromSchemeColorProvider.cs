using SkiaSharp;

namespace ObbTextGenerator;

public sealed class FromSchemeColorProvider(
    FromSchemeColorProviderSettings settings,
    StageFactoryContext stageFactoryContext) : IColorProvider
{
    private readonly string _role = settings.Role;
    private readonly StageFactoryContext _stageFactoryContext = stageFactoryContext;

    public SKColor GetColor(RenderContext context)
    {
        var activeScheme = context.ActiveScheme;
        if (activeScheme == null)
        {
            return SKColors.White;
        }

        if (!activeScheme.Roles.TryGetValue(_role, out var roleSettings))
        {
            return SKColors.White;
        }

        if (roleSettings is FromSchemeColorProviderSettings)
        {
            return SKColors.White;
        }

        var colorProvider = ColorProviderFactory.Create(roleSettings, _stageFactoryContext);
        return colorProvider.GetColor(context);
    }
}
