using NvhLibCSharp;
using NvhLibCSharp.Options;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class HilbertEnvelopeStep : IAnalysisStep
{
    public string StepType => "HilbertEnvelope";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "HilbertEnvelope",
        DisplayName = "希尔伯特包络",
        Description = "全频段希尔伯特包络（时域）",
        Category = "时域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var maxPoints = StepParameters.GetInt(parameters, "maxPoints", 2000);
        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var envelope = Nvh.HilbertEnvelope(signal);
            var timeAxis = NvhStepCharts.BuildTimeAxis(envelope.Length, signal.DeltaTime);
            var (xs, ys) = PlotDataHelper.DownsampleXY(timeAxis, envelope, maxPoints);
            series.Add(NvhStepCharts.BuildSeries(source, xs, ys));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([
            NvhStepCharts.Line("he", "希尔伯特包络", "时间 (s)", "幅值", series)
        ]);
    }
}

public sealed class HilbertEnvelopeSpectraStep : IAnalysisStep
{
    public string StepType => "HilbertEnvelopeSpectra";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "HilbertEnvelopeSpectra",
        DisplayName = "包络谱",
        Description = "希尔伯特包络频谱（单帧）",
        Category = "频域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var window = NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning"));
        var format = NvhEnumHelper.ParseFormat(StepParameters.GetString(parameters, "format", "Rms"));
        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var values = Nvh.HilbertEnvelopeSpectra(signal, window, format, out var freqAxis);
            var (xs, ys) = PlotDataHelper.DownsampleXY(freqAxis, values);
            series.Add(NvhStepCharts.BuildSeries(source, xs, ys));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([
            NvhStepCharts.Line("hes", "包络谱", "频率 (Hz)", "幅值", series)
        ]);
    }
}

public sealed class HilbertEnvelopeAvgSpectraStep : IAnalysisStep
{
    public string StepType => "HilbertEnvelopeAvgSpectra";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "HilbertEnvelopeAvgSpectra",
        DisplayName = "平均包络谱",
        Description = "希尔伯特包络平均频谱",
        Category = "频域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var spectral = SpectralParameterHelper.ReadAveragedSpectrumParams(parameters);
        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var values = Nvh.HilbertEnvelopeAvgSpectra(
                signal,
                spectral.Calc,
                spectral.Step,
                spectral.Format,
                spectral.Average,
                spectral.Window,
                spectral.Weight,
                out var freqAxis);
            var (xs, ys) = PlotDataHelper.DownsampleXY(freqAxis, values);
            series.Add(NvhStepCharts.BuildSeries(source, xs, ys));
        }

        var yLabel = spectral.ScaleKind == Scale.Db ? "声压级 (dB)" : "幅值";
        return Task.FromResult<IReadOnlyList<ChartCardModel>>([
            NvhStepCharts.Line("heas", "平均包络谱", "频率 (Hz)", yLabel, series)
        ]);
    }
}

public sealed class HilbertEnvelopeExFixedStep : IAnalysisStep
{
    public string StepType => "HilbertEnvelopeExFixed";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "HilbertEnvelopeExFixed",
        DisplayName = "固定带通包络",
        Description = "固定中心频率的希尔伯特带通包络",
        Category = "时域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var bandWidth = StepParameters.GetDouble(parameters, "bandWidth", 350);
        var centerFrequency = StepParameters.GetDouble(parameters, "centerFrequency", 875);
        var maxPoints = StepParameters.GetInt(parameters, "maxPoints", 2000);
        var options = new EnvelopeExOptions(bandWidth, centerFrequency);
        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var envelope = Nvh.HilbertEnvelopeEx(signal, options);
            var timeAxis = NvhStepCharts.BuildTimeAxis(envelope.Length, signal.DeltaTime);
            var (xs, ys) = PlotDataHelper.DownsampleXY(timeAxis, envelope, maxPoints);
            series.Add(NvhStepCharts.BuildSeries(source, xs, ys));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([
            NvhStepCharts.Line("hexf", "固定带通包络", "时间 (s)", "幅值", series)
        ]);
    }
}

public sealed class HilbertEnvelopeExTrackedStep : IAnalysisStep
{
    public string StepType => "HilbertEnvelopeExTracked";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "HilbertEnvelopeExTracked",
        DisplayName = "阶次跟踪包络",
        Description = "随转速跟踪的希尔伯特带通包络",
        Category = "阶次"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var centerOrder = StepParameters.GetDouble(parameters, "centerOrder", 2.0);
        var bandWidth = StepParameters.GetDouble(parameters, "bandWidth", 1000);
        var windowLength = StepParameters.GetInt(parameters, "windowLength", 4096);
        var minFrequency = StepParameters.GetDouble(parameters, "minFrequency", 100);
        var maxFrequency = StepParameters.GetDouble(parameters, "maxFrequency", 4000);
        var maxPoints = StepParameters.GetInt(parameters, "maxPoints", 2000);
        var series = new List<LineSeriesData>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var rpmValues = RpmChannelHelper.LoadRpmValuesForRange(input, source, parameters);
            var options = new EnvelopeExOptions(centerOrder, bandWidth, windowLength, minFrequency, maxFrequency, rpmValues);
            var envelope = Nvh.HilbertEnvelopeEx(signal, options);
            var timeAxis = NvhStepCharts.BuildTimeAxis(envelope.Length, signal.DeltaTime);
            var (xs, ys) = PlotDataHelper.DownsampleXY(timeAxis, envelope, maxPoints);
            series.Add(NvhStepCharts.BuildSeries(source, xs, ys));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>([
            NvhStepCharts.Line("hext", "阶次跟踪包络", "时间 (s)", "幅值", series)
        ]);
    }
}
