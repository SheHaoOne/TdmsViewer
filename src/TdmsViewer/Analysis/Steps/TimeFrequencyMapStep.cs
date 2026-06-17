using NvhLibCSharp;
using NvhLibCSharp.Options;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

public sealed class TimeFrequencyMapStep : IAnalysisStep
{
    public string StepType => "TimeFrequencyMap";

    public StepDefinition Definition { get; } = new()
    {
        StepType = "TimeFrequencyMap",
        DisplayName = "时频图",
        Description = "短时傅里叶变换（STFT）谱图",
        Category = "频域"
    };

    public Task<IReadOnlyList<ChartCardModel>> ExecuteAsync(
        AnalysisInputContext input,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NvhLicenseService.EnsureLoaded();

        var mapParams = SpectralParameterHelper.ReadMapParams(parameters);
        var maxPoints = StepParameters.GetInt(parameters, "maxPoints", 500);
        var cards = new List<ChartCardModel>(input.Sources.Count);

        foreach (var source in input.Sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signal = StepSignalHelper.ToSignal(source, input.GlobalTimeRange, parameters);
            var data = Nvh.TimeFrequencyMap(
                signal,
                mapParams.SpectrumLines,
                mapParams.Increment,
                mapParams.ReferenceValue,
                mapParams.Format,
                mapParams.Window,
                mapParams.Weight,
                mapParams.Scale,
                out var timeAxis,
                out var frequencyAxis);

            var (xs, _) = PlotDataHelper.DownsampleXY(timeAxis, timeAxis, maxPoints);
            var downsampled = DownsampleMatrixRows(data, xs.Length, timeAxis.Length);

            cards.Add(NvhStepCharts.Heatmap(
                $"tfm-{source.FilePath}",
                "时频图",
                "时间 (s)",
                "频率 (Hz)",
                downsampled,
                xs,
                frequencyAxis,
                source.FilePath,
                colorRange: input.GlobalHeatmapColorRange));
        }

        return Task.FromResult<IReadOnlyList<ChartCardModel>>(cards);
    }

    private static double[,] DownsampleMatrixRows(double[,] data, int targetRows, int sourceRows)
    {
        if (targetRows >= sourceRows)
            return data;

        var cols = data.GetLength(1);
        var result = new double[targetRows, cols];
        var step = (double)sourceRows / targetRows;
        for (var i = 0; i < targetRows; i++)
        {
            var sourceRow = Math.Min(sourceRows - 1, (int)Math.Round(i * step));
            for (var j = 0; j < cols; j++)
                result[i, j] = data[sourceRow, j];
        }

        return result;
    }
}
