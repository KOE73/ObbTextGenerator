namespace ObbTextGenerator;

public sealed class PipelineSelectStage(
    PipelineSelectStageSettings settings,
    List<CompiledPipelineProgram> candidates) : CompositePipelineStageBase(settings)
{
    private readonly PipelineSelectStageSettings _settings = settings;
    private readonly List<CompiledPipelineProgram> _candidates = candidates;

    public override string Name => $"Pipeline/Select({GetSelectionTarget()})";

    protected override void ApplyCore(RenderContext context)
    {
        if (_candidates.Count == 0)
        {
            context.AddTrace($"select {GetSelectionTarget()} -> none");
            return;
        }

        var selectedPrograms = SelectPrograms(context.Settings.Random);
        if (selectedPrograms.Count == 0)
        {
            context.AddTrace($"select {GetSelectionTarget()} -> none");
            return;
        }

        var selectedNames = string.Join(", ", selectedPrograms.Select(program => program.Name));
        context.AddTrace($"select {GetSelectionTarget()} -> {selectedNames}");
        foreach (var program in selectedPrograms)
        {
            using var scope = context.BeginTraceScope($"program: {program.Name}");
            foreach (var stage in program.Stages)
            {
                stage.Apply(context);
            }
        }
    }

    private string GetSelectionTarget()
    {
        if (!string.IsNullOrWhiteSpace(_settings.Group))
        {
            return _settings.Group;
        }

        return "custom";
    }

    private List<CompiledPipelineProgram> SelectPrograms(Random random)
    {
        return _settings.Mode switch
        {
            PipelineSelectionMode.OptionalSingle => SelectOptionalSingle(random),
            PipelineSelectionMode.Multi => SelectMultiple(random),
            _ => SelectSingle(random)
        };
    }

    private List<CompiledPipelineProgram> SelectSingle(Random random)
    {
        var selected = SelectOne(_candidates, random);
        if (selected == null)
        {
            return [];
        }

        return [selected];
    }

    private List<CompiledPipelineProgram> SelectOptionalSingle(Random random)
    {
        var totalWeight = _settings.NoneWeight;
        foreach (var candidate in _candidates)
        {
            totalWeight += Math.Max(0.0, candidate.Weight);
        }

        if (totalWeight <= 0.0)
        {
            return [];
        }

        var value = random.NextDouble() * totalWeight;
        if (value < _settings.NoneWeight)
        {
            return [];
        }

        value -= _settings.NoneWeight;
        foreach (var candidate in _candidates)
        {
            var weight = Math.Max(0.0, candidate.Weight);
            if (value <= weight)
            {
                return [candidate];
            }

            value -= weight;
        }

        return [];
    }

    private List<CompiledPipelineProgram> SelectMultiple(Random random)
    {
        var selectedPrograms = new List<CompiledPipelineProgram>();
        var availableCandidates = new List<CompiledPipelineProgram>(_candidates);
        var count = _settings.Count.SampleInt(random);

        for (int index = 0; index < count; index++)
        {
            var candidatePool = _settings.AllowDuplicates ? _candidates : availableCandidates;
            if (candidatePool.Count == 0)
            {
                break;
            }

            var selected = SelectOne(candidatePool, random);
            if (selected == null)
            {
                break;
            }

            selectedPrograms.Add(selected);

            if (!_settings.AllowDuplicates)
            {
                availableCandidates.Remove(selected);
            }
        }

        return selectedPrograms;
    }

    private static CompiledPipelineProgram? SelectOne(List<CompiledPipelineProgram> candidates, Random random)
    {
        double totalWeight = 0.0;
        foreach (var candidate in candidates)
        {
            totalWeight += Math.Max(0.0, candidate.Weight);
        }

        if (totalWeight <= 0.0)
        {
            return candidates[0];
        }

        var value = random.NextDouble() * totalWeight;
        foreach (var candidate in candidates)
        {
            var weight = Math.Max(0.0, candidate.Weight);
            if (value <= weight)
            {
                return candidate;
            }

            value -= weight;
        }

        return candidates[^1];
    }

    protected override string BuildTraceSummary(RenderContext context)
    {
        return $"select: {GetSelectionTarget()}";
    }
}
