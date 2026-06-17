using NvhLibCSharp.Interop;

namespace TdmsViewer.Services;

public static class NvhSignalAdapter
{
    public static Signal ToSignal(double[] samples, double sampleRateHz)
    {
        if (samples.Length == 0)
            throw new InvalidOperationException("通道数据为空，无法进行分析。");

        if (sampleRateHz <= 0)
            throw new InvalidOperationException("采样率无效，无法进行分析。");

        return new Signal(samples, 1.0 / sampleRateHz);
    }
}
