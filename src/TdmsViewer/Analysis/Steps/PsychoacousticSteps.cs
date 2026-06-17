using NvhLibCSharp;
using NvhLibCSharp.Enums;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class StationaryLoudnessStep : IAnalysisStep
{
    public string StepType => "StationaryLoudness";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "StationaryLoudness",
        DisplayName = "稳态响度",
        Description = "ISO 稳态响度分析",
        Category = "心理声学"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var soundField = NvhEnumHelper.ParseSoundField(StepParameters.GetString(parameters, "soundField", "Free"));
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var (loudness, specLoudness) = Nvh.StationaryLoudnessAnalyze(signal, soundField, 0.0, out var barkAxis, out _);

            cards.Add(NvhStepCharts.Metric($"sl-{source.FilePath}", "稳态响度", loudness, "sone", source.FilePath));
            cards.Add(NvhStepCharts.Bar(
                $"sl-spec-{source.FilePath}",
                "特征响度",
                "Bark 频带",
                "响度 (sone/Bark)",
                Enumerable.Range(0, barkAxis.Length).Select(i => (double)i).ToArray(),
                specLoudness,
                barkAxis.Select(b => $"{b:F1}").ToArray(),
                source.FilePath));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class TimeVaryingLoudnessStep : IAnalysisStep
{
    public string StepType => "TimeVaryingLoudness";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "TimeVaryingLoudness",
        DisplayName = "时变响度",
        Description = "ISO 时变响度分析",
        Category = "心理声学"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var soundField = NvhEnumHelper.ParseSoundField(StepParameters.GetString(parameters, "soundField", "Free"));
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var (loudness, specLoudness) = Nvh.TimeVaryingLoudnessAnalyze(
                signal, soundField, 0.0, out var barkAxis, out _, out var timeAxis);

            var (txs, tys) = PlotDataHelper.DownsampleXY(timeAxis, loudness);
            cards.Add(NvhStepCharts.Line(
                $"tvl-{source.FilePath}",
                "时变响度",
                "时间 (s)",
                "响度 (sone)",
                [NvhStepCharts.BuildSeries(source, txs, tys)]));

            cards.Add(NvhStepCharts.Heatmap(
                $"tvl-spec-{source.FilePath}",
                "特征响度谱",
                "时间 (s)",
                "Bark 频带",
                NvhStepCharts.Transpose(specLoudness),
                timeAxis,
                barkAxis,
                source.FilePath,
                colorRange: input.GlobalHeatmapColorRange));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class StationarySharpnessStep : IAnalysisStep
{
    public string StepType => "StationarySharpness";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "StationarySharpness",
        DisplayName = "稳态尖锐度",
        Description = "稳态尖锐度分析",
        Category = "心理声学"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var weighting = NvhEnumHelper.ParseSharpnessWeighting(StepParameters.GetString(parameters, "weighting", "Din"));
        var soundField = NvhEnumHelper.ParseSoundField(StepParameters.GetString(parameters, "soundField", "Free"));
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var sharpness = Nvh.StationarySharpnessAnalyze(
                signal, weighting, soundField, 0.0, out var specSharpness, out var barkAxis, out _);

            cards.Add(NvhStepCharts.Metric($"ss-{source.FilePath}", "稳态尖锐度", sharpness, "acum", source.FilePath));
            cards.Add(NvhStepCharts.Bar(
                $"ss-spec-{source.FilePath}",
                "特征尖锐度",
                "Bark 频带",
                "尖锐度",
                Enumerable.Range(0, barkAxis.Length).Select(i => (double)i).ToArray(),
                specSharpness,
                barkAxis.Select(b => $"{b:F1}").ToArray(),
                source.FilePath));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class TimeVaryingSharpnessStep : IAnalysisStep
{
    public string StepType => "TimeVaryingSharpness";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "TimeVaryingSharpness",
        DisplayName = "时变尖锐度",
        Description = "时变尖锐度分析",
        Category = "心理声学"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var weighting = NvhEnumHelper.ParseSharpnessWeighting(StepParameters.GetString(parameters, "weighting", "Din"));
        var soundField = NvhEnumHelper.ParseSoundField(StepParameters.GetString(parameters, "soundField", "Free"));
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var sharpness = Nvh.TimeVaryingSharpnessAnalyze(
                signal, weighting, soundField, 0.0, out var specSharpness, out var barkAxis, out _, out var timeAxis);

            var (txs, tys) = PlotDataHelper.DownsampleXY(timeAxis, sharpness);
            cards.Add(NvhStepCharts.Line(
                $"tvs-{source.FilePath}",
                "时变尖锐度",
                "时间 (s)",
                "尖锐度 (acum)",
                [NvhStepCharts.BuildSeries(source, txs, tys)]));

            cards.Add(NvhStepCharts.Heatmap(
                $"tvs-spec-{source.FilePath}",
                "特征尖锐度谱",
                "时间 (s)",
                "Bark 频带",
                NvhStepCharts.Transpose(specSharpness),
                timeAxis,
                barkAxis,
                source.FilePath,
                colorRange: input.GlobalHeatmapColorRange));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class RoughnessStep : IAnalysisStep
{
    public string StepType => "Roughness";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Roughness",
        DisplayName = "粗糙度",
        Description = "粗糙度分析",
        Category = "心理声学"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var soundField = NvhEnumHelper.ParseSoundField(StepParameters.GetString(parameters, "soundField", "Free"));
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var roughness = Nvh.RoughnessAnalyze(
                signal,
                soundField,
                0.0,
                out var roughnessTimeDep,
                out _,
                out var roughnessSpecAvg,
                out var bandAxis,
                out _,
                out _,
                out var timeAxis);

            cards.Add(NvhStepCharts.Metric($"rough-{source.FilePath}", "粗糙度", roughness, "asper", source.FilePath));

            var (txs, tys) = PlotDataHelper.DownsampleXY(timeAxis, roughnessTimeDep);
            cards.Add(NvhStepCharts.Line(
                $"rough-tv-{source.FilePath}",
                "时变粗糙度",
                "时间 (s)",
                "粗糙度 (asper)",
                [NvhStepCharts.BuildSeries(source, txs, tys)]));

            cards.Add(NvhStepCharts.Bar(
                $"rough-band-{source.FilePath}",
                "频带平均粗糙度",
                "频带",
                "粗糙度 (asper)",
                Enumerable.Range(0, bandAxis.Length).Select(i => (double)i).ToArray(),
                roughnessSpecAvg,
                bandAxis.Select(b => $"{b:F1}").ToArray(),
                source.FilePath));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class FluctuationStrengthStep : IAnalysisStep
{
    public string StepType => "FluctuationStrength";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "FluctuationStrength",
        DisplayName = "波动度",
        Description = "波动强度分析",
        Category = "心理声学"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var method = NvhEnumHelper.ParseFluctuationMethod(StepParameters.GetString(parameters, "method", "Stationary"));
        var cards = new List<ChartCardModel>();

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var fluctuation = Nvh.FluctuationStrengthAnalyze(
                signal,
                method,
                out var fluctuationTimeDep,
                out _,
                out var fluctuationSpecAvg,
                out var bandAxis,
                out _,
                out var timeAxis);

            cards.Add(NvhStepCharts.Metric($"fluc-{source.FilePath}", "波动度", fluctuation, "vacil", source.FilePath));

            var (txs, tys) = PlotDataHelper.DownsampleXY(timeAxis, fluctuationTimeDep);
            cards.Add(NvhStepCharts.Line(
                $"fluc-tv-{source.FilePath}",
                "时变波动度",
                "时间 (s)",
                "波动度",
                [NvhStepCharts.BuildSeries(source, txs, tys)]));

            cards.Add(NvhStepCharts.Bar(
                $"fluc-band-{source.FilePath}",
                "频带平均波动度",
                "频带",
                "波动度",
                Enumerable.Range(0, bandAxis.Length).Select(i => (double)i).ToArray(),
                fluctuationSpecAvg,
                bandAxis.Select(b => $"{b:F1}").ToArray(),
                source.FilePath));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}
