using System.Globalization;
using NvhLibCSharp;
using NvhLibCSharp.Enums;
using NvhLibCSharp.Interop;
using NvhLibCSharp.Options;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Analysis.Reporting;

namespace TdmsViewer.Analysis.Steps;

internal static class NvhStepCharts
{
    public static LineSeriesData BuildSeries(AnalysisSourceSample source, double[] x, double[] y) =>
        new()
        {
            Label = source.Label,
            X = x,
            Y = y,
            Color = source.Color,
            SourceKey = source.FilePath
        };

    public static LineChartModel Line(
        string id,
        string title,
        string xLabel,
        string yLabel,
        IReadOnlyList<LineSeriesData> series,
        bool useLogXAxis = false,
        bool useStepLine = false) =>
        new(id, title, xLabel, yLabel, series, useLogXAxis, useStepLine);

    public static HeatmapChartModel Heatmap(
        string id,
        string title,
        string xLabel,
        string yLabel,
        double[,] values,
        double[] xAxis,
        double[] yAxis,
        string? sourceKey = null) =>
        new(id, title, xLabel, yLabel, values, xAxis, yAxis, sourceKey);

    public static MetricCardModel Metric(
        string id,
        string title,
        double value,
        string unit,
        string? sourceKey = null,
        int decimals = 2) =>
        new(id, title, value.ToString($"F{decimals}", CultureInfo.InvariantCulture), unit, SourceKey: sourceKey);

    public static BarChartModel Bar(
        string id,
        string title,
        string xLabel,
        string yLabel,
        double[] categories,
        double[] values,
        string[]? categoryLabels = null,
        string? sourceKey = null) =>
        new(id, title, xLabel, yLabel, categories, values, categoryLabels, SourceKey: sourceKey);

    public static BarChartModel BarOverlay(
        string id,
        string title,
        string xLabel,
        string yLabel,
        double[] categories,
        IReadOnlyList<BarSeriesData> series,
        string[]? categoryLabels = null) =>
        new(id, title, xLabel, yLabel, categories, series[0].Values, categoryLabels, series);

    public static double[,] Transpose(double[,] data)
    {
        var rows = data.GetLength(0);
        var cols = data.GetLength(1);
        var result = new double[cols, rows];
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
                result[j, i] = data[i, j];
        }

        return result;
    }

    public static double[] BuildTimeAxis(int count, double deltaTime)
    {
        var axis = new double[count];
        for (var i = 0; i < count; i++)
            axis[i] = i * deltaTime;

        return axis;
    }
}

internal static class RpmChannelHelper
{
    public static double[] LoadRpmValues(
        AnalysisInputContext input,
        AnalysisSourceSample source,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var rpmGroup = StepParameters.GetString(parameters, "rpmGroupName", source.GroupName);
        var rpmChannel = StepParameters.GetString(parameters, "rpmChannelName", string.Empty);
        if (string.IsNullOrWhiteSpace(rpmChannel))
            throw new InvalidOperationException("请配置转速通道名称（rpmChannelName）。");

        var accessor = input.Data
            ?? throw new InvalidOperationException("无法读取转速通道：分析上下文未提供数据访问器。");

        var rpmValues = accessor.TryReadChannel(source.FilePath, rpmGroup, rpmChannel);
        if (rpmValues == null || rpmValues.Length == 0)
            throw new InvalidOperationException($"无法读取转速通道：{rpmGroup} / {rpmChannel}");

        return rpmValues;
    }

    public static Rpm LoadRpm(
        AnalysisInputContext input,
        AnalysisSourceSample source,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var rpmValues = LoadRpmValues(input, source, parameters);

        var rpmSampleRateHz = StepParameters.GetDouble(parameters, "rpmSampleRateHz", 0);
        var increment = rpmSampleRateHz > 0
            ? 1.0 / rpmSampleRateHz
            : 1.0 / source.SampleRateHz;

        return new Rpm(rpmValues, increment);
    }
}

internal static class SpectralParameterHelper
{
    public static (
        SpectraCalcOptions Calc,
        SpectraStepOptions Step,
        ScaleOptions Scale,
        Format Format,
        Average Average,
        Window Window,
        Weight Weight,
        Scale ScaleKind) ReadAveragedSpectrumParams(IReadOnlyDictionary<string, object?>? parameters)
    {
        var calcType = NvhEnumHelper.ParseCalcType(StepParameters.GetString(parameters, "calcType", "Resolution"));
        var calcValue = StepParameters.GetDouble(parameters, "calcValue", 1.0);
        var stepType = NvhEnumHelper.ParseStepType(StepParameters.GetString(parameters, "stepType", "Increment"));
        var stepValue = StepParameters.GetDouble(parameters, "stepValue", 0.15);
        var referenceValue = StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5);
        var format = NvhEnumHelper.ParseFormat(StepParameters.GetString(parameters, "format", "Rms"));
        var average = NvhEnumHelper.ParseAverage(StepParameters.GetString(parameters, "average", "Energy"));
        var window = NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning"));
        var weight = NvhEnumHelper.ParseWeight(StepParameters.GetString(parameters, "weight", "A"));
        var scale = NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db"));

        return (
            new SpectraCalcOptions(calcType, calcValue),
            new SpectraStepOptions(stepType, stepValue),
            new ScaleOptions(scale, referenceValue),
            format,
            average,
            window,
            weight,
            scale);
    }

    public static (
        int SpectrumLines,
        double Increment,
        double ReferenceValue,
        Format Format,
        Window Window,
        Weight Weight,
        Scale Scale) ReadMapParams(IReadOnlyDictionary<string, object?>? parameters)
    {
        return (
            StepParameters.GetInt(parameters, "spectrumLines", 4096),
            StepParameters.GetDouble(parameters, "increment", 0.15),
            StepParameters.GetDouble(parameters, "referenceValue", 2.0e-5),
            NvhEnumHelper.ParseFormat(StepParameters.GetString(parameters, "format", "Rms")),
            NvhEnumHelper.ParseWindow(StepParameters.GetString(parameters, "window", "Hanning")),
            NvhEnumHelper.ParseWeight(StepParameters.GetString(parameters, "weight", "A")),
            NvhEnumHelper.ParseScale(StepParameters.GetString(parameters, "scale", "Db")));
    }

    public static (
        double MinRpm,
        double MaxRpm,
        double RpmStep,
        RpmTrigger Trigger) ReadRpmAxisParams(IReadOnlyDictionary<string, object?>? parameters) =>
        (
            StepParameters.GetDouble(parameters, "minRpm", 600),
            StepParameters.GetDouble(parameters, "maxRpm", 4000),
            StepParameters.GetDouble(parameters, "rpmStep", 25),
            NvhEnumHelper.ParseRpmTrigger(StepParameters.GetString(parameters, "rpmTrigger", "Up")));
}
