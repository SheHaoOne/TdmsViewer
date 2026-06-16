using NVHAlgorithmKit;
using NVHAlgorithmKit.Core;
using NVHAlgorithmKit.TimeDomain;
using Xunit;

namespace NVHAlgorithmKit.Compatibility.Tests;

public class CompatibilitySmokeTests
{
    [Fact]
    public void NvhAnalyzer_WorksOnCurrentRuntime()
    {
        var samples = Enumerable.Range(0, 1024)
            .Select(i => Math.Sin(2.0 * Math.PI * 50 * i / 1000))
            .ToArray();

        var signal = new NvhSignal(samples, 1000);
        var features = NvhAnalyzer.AnalyzeTimeDomain(signal);

        Assert.True(features.Rms > 0.5);
        Assert.True(features.Peak <= 1.01);
    }
}
