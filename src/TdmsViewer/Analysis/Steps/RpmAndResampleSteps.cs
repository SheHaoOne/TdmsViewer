using NvhLibCSharp;
using NvhLibCSharp.Enums;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class ResampleStep : IAnalysisStep
{
    public string StepType => "Resample";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "Resample",
        DisplayName = "重采样",
        Description = "分数倍重采样波形",
        Category = "时域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var destSampleRateHz = StepParameters.GetDouble(parameters, "destSampleRateHz", 44100);
        var bandRatio = StepParameters.GetDouble(parameters, "bandRatio", 0.9);
        var planningMode = NvhEnumHelper.ParseResamplerPlanning(StepParameters.GetString(parameters, "planningMode", "Balanced"));
        var maxPoints = StepParameters.GetInt(parameters, "maxPoints", 2000);
        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var resampled = Nvh.ResampleSignal(signal, destSampleRateHz, bandRatio, planningMode);
            var deltaTime = 1.0 / destSampleRateHz;
            var timeAxis = NvhStepCharts.BuildTimeAxis(resampled.Length, deltaTime);
            var (xs, ys) = PlotDataHelper.DownsampleXY(timeAxis, resampled, maxPoints);
            series.Add(NvhStepCharts.BuildSeries(source, xs, ys));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([
            NvhStepCharts.Line("rs", $"重采样 ({destSampleRateHz:N0} Hz)", "时间 (s)", "幅值", series)
        ]);
    }
}

public sealed class OrderSectionStep : IAnalysisStep
{
    public string StepType => "OrderSection";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "OrderSection",
        DisplayName = "阶次截面",
        Description = "指定阶次的幅值随转速变化",
        Category = "阶次"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var spectrumLines = StepParameters.GetInt(parameters, "spectrumLines", 4096);
        var targetOrder = StepParameters.GetDouble(parameters, "targetOrder", 14.0);
        var orderBandwidth = StepParameters.GetDouble(parameters, "orderBandwidth", 0.5);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var format = NvhEnumHelper.ParseFormat(StepParameters.GetString(parameters, "format", "Rms"));
        var window = NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning"));
        var weight = NvhEnumHelper.ParseWeight(StepParameters.GetString(parameters, "weight", "A"));
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));
        var rpmAxisParams = SpectralParameterHelper.ReadRpmAxisParams(parameters);
        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var rpm = RpmChannelHelper.LoadRpm(input, source, parameters);
            var values = Nvh.OrderSection(
                signal,
                rpm,
                spectrumLines,
                targetOrder,
                orderBandwidth,
                rpmAxisParams.MinRpm,
                rpmAxisParams.MaxRpm,
                rpmAxisParams.RpmStep,
                referenceValue,
                format,
                window,
                weight,
                scale,
                rpmAxisParams.Trigger,
                out var rpmAxis);

            series.Add(NvhStepCharts.BuildSeries(source, rpmAxis, values));
        }

        var yLabel = scale == Scale.Db ? "声压级 (dB)" : "幅值";
        return Task.FromResult<IReadOnlyList<ChartCardModel>>([
            NvhStepCharts.Line("ots", $"阶次截面 ({targetOrder} 阶)", "转速 (RPM)", yLabel, series)
        ]);
    }
}

public sealed class RpmFrequencyMapStep : IAnalysisStep
{
    public string StepType => "RpmFrequencyMap";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "RpmFrequencyMap",
        DisplayName = "Campbell 图",
        Description = "转速-频率谱图（Campbell Diagram）",
        Category = "阶次"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var mapParams = SpectralParameterHelper.ReadMapParams(parameters);
        var rpmAxisParams = SpectralParameterHelper.ReadRpmAxisParams(parameters);
        var cards = new List<ChartCardModel>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var rpm = RpmChannelHelper.LoadRpm(input, source, parameters);
            var data = Nvh.RpmFrequencyMap(
                signal,
                rpm,
                mapParams.SpectrumLines,
                rpmAxisParams.MinRpm,
                rpmAxisParams.MaxRpm,
                rpmAxisParams.RpmStep,
                mapParams.ReferenceValue,
                mapParams.Format,
                mapParams.Window,
                mapParams.Weight,
                mapParams.Scale,
                rpmAxisParams.Trigger,
                out var rpmAxis,
                out var frequencyAxis);

            cards.Add(NvhStepCharts.Heatmap(
                $"rfm-{source.FilePath}",
                $"Campbell 图 · {source.Label}",
                "转速 (RPM)",
                "频率 (Hz)",
                data,
                rpmAxis,
                frequencyAxis,
                source.FilePath,
                colorRange: input.GlobalHeatmapColorRange));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}

public sealed class RpmOrderMapStep : IAnalysisStep
{
    public string StepType => "RpmOrderMap";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "RpmOrderMap",
        DisplayName = "阶次谱图",
        Description = "转速-阶次谱图",
        Category = "阶次"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var maxOrder = StepParameters.GetDouble(parameters, "maxOrder", 32.0);
        var orderResolution = StepParameters.GetDouble(parameters, "orderResolution", 0.25);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var format = NvhEnumHelper.ParseFormat(StepParameters.GetString(parameters, "format", "Rms"));
        var window = NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning"));
        var weight = NvhEnumHelper.ParseWeight(StepParameters.GetString(parameters, "weight", "A"));
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));
        var rpmAxisParams = SpectralParameterHelper.ReadRpmAxisParams(parameters);
        var cards = new List<ChartCardModel>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var rpm = RpmChannelHelper.LoadRpm(input, source, parameters);
            var data = Nvh.RpmOrderMap(
                signal,
                rpm,
                maxOrder,
                orderResolution,
                rpmAxisParams.MinRpm,
                rpmAxisParams.MaxRpm,
                rpmAxisParams.RpmStep,
                referenceValue,
                format,
                window,
                weight,
                scale,
                out var rpmAxis,
                out var orderAxis);

            cards.Add(NvhStepCharts.Heatmap(
                $"rom-{source.FilePath}",
                $"阶次谱图 · {source.Label}",
                "转速 (RPM)",
                "阶次",
                data,
                rpmAxis,
                orderAxis,
                source.FilePath,
                colorRange: input.GlobalHeatmapColorRange));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }
}
