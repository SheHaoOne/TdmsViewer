using NvhLibCSharp;
using NvhLibCSharp.Options;
using NvhLibCSharp.Utils;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class MorletWaveletStep : IAnalysisStep
{
    public string StepType => "MorletWavelet";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "MorletWavelet",
        DisplayName = "Morlet 小波",
        Description = "指定频率轴的 Morlet 小波变换",
        Category = "小波"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var minFrequency = StepParameters.GetDouble(parameters, "minFrequency", 1.0);
        var maxFrequency = StepParameters.GetDouble(parameters, "maxFrequency", 1000);
        var bandCount = StepParameters.GetInt(parameters, "bandCount", 50);
        var nCycles = StepParameters.GetDouble(parameters, "nCycles", 5.0);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));
        var scaleOpt = new ScaleOptions(scale, referenceValue);
        var cards = new List<ChartCardModel>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var nyquist = source.SampleRateHz / 2.0;
            var maxFreq = Math.Min(maxFrequency, nyquist);
            var frequencyAxis = MathUtils.Logspace(Math.Log10(minFrequency), Math.Log10(maxFreq), bandCount).ToArray();
            var data = Nvh.MorletWaveletTransform(signal, scaleOpt, frequencyAxis, nCycles, out var timeAxis);

            cards.Add(NvhStepCharts.Heatmap(
                $"morlet-{source.FilePath}",
                $"Morlet 小波 · {source.Label}",
                "时间 (s)",
                "频率 (Hz)",
                data,
                timeAxis,
                frequencyAxis,
                source.FilePath));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class LmsMorletWaveletStep : IAnalysisStep
{
    public string StepType => "LmsMorletWavelet";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "LmsMorletWavelet",
        DisplayName = "LMS Morlet 小波",
        Description = "对数频带 Morlet 小波变换",
        Category = "小波"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var minFrequency = StepParameters.GetDouble(parameters, "minFrequency", 10);
        var maxFrequency = StepParameters.GetDouble(parameters, "maxFrequency", 1000);
        var bandsPerOctave = StepParameters.GetInt(parameters, "bandsPerOctave", 100);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));
        var scaleOpt = new ScaleOptions(scale, referenceValue);
        var cards = new List<ChartCardModel>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var data = Nvh.LmsMorletWaveletTransform(
                signal,
                scaleOpt,
                minFrequency,
                Math.Min(maxFrequency, source.SampleRateHz / 2.0),
                bandsPerOctave,
                out var timeAxis,
                out var frequencyAxis);

            cards.Add(NvhStepCharts.Heatmap(
                $"lms-{source.FilePath}",
                $"LMS Morlet · {source.Label}",
                "时间 (s)",
                "频率 (Hz)",
                data,
                timeAxis,
                frequencyAxis,
                source.FilePath));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}
