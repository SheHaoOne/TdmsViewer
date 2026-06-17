using NvhLibCSharp.Interop;
using TdmsViewer.Analysis.Contracts;
using TdmsViewer.Analysis.Pipeline;
using TdmsViewer.Services;

namespace TdmsViewer.Analysis.Steps;

internal static class StepSignalHelper
{
    public static AnalysisTimeRange ResolveRange(
        AnalysisSourceSample source,
        AnalysisTimeRange? globalRange,
        IReadOnlyDictionary<string, object?>? stepParameters)
    {
        var totalSec = source.Samples.Length / source.SampleRateHz;
        return AnalysisTimeRangeResolver.Resolve(globalRange, stepParameters, totalSec);
    }

    public static Signal ToSignal(
        AnalysisSourceSample source,
        AnalysisTimeRange? globalRange,
        IReadOnlyDictionary<string, object?>? stepParameters)
    {
        var range = ResolveRange(source, globalRange, stepParameters);
        return NvhSignalAdapter.ToSignal(source.Samples, source.SampleRateHz, range);
    }

    public static SignalSegmentHelper.SliceResult SliceSource(
        AnalysisSourceSample source,
        AnalysisTimeRange? globalRange,
        IReadOnlyDictionary<string, object?>? stepParameters)
    {
        var range = ResolveRange(source, globalRange, stepParameters);
        return SignalSegmentHelper.Slice(source.Samples, source.SampleRateHz, range);
    }
}
