using NVHAlgorithmKit.Core;

namespace NVHAlgorithmKit.Fatigue;

/// <summary>
/// 雨流计数循环。
/// </summary>
public sealed class RainflowCycle
{
    /// <summary>应力/载荷幅值（半范围），单位与输入信号一致。</summary>
    public double Range { get; init; }

    /// <summary>循环均值，单位与输入信号一致。</summary>
    public double Mean { get; init; }

    /// <summary>循环计数（完整循环 = 1.0，半循环 = 0.5）。</summary>
    public double Count { get; init; }
}

/// <summary>
/// 雨流计数直方图结果。
/// </summary>
public sealed class RainflowHistogram
{
    /// <summary>幅值范围分箱中心值数组。</summary>
    public double[] RangeBins { get; init; } = Array.Empty<double>();

    /// <summary>各分箱累计循环次数。</summary>
    public double[] Counts { get; init; } = Array.Empty<double>();

    /// <summary>总循环数。</summary>
    public double TotalCycles { get; init; }
}

/// <summary>
/// 雨流计数器（ASTM E1049），用于疲劳载荷谱统计。
/// </summary>
public static class RainflowCounter
{
    /// <summary>
    /// 对载荷时程执行三阶雨流计数。
    /// </summary>
    /// <param name="load">载荷或应力时域序列。</param>
    /// <returns>识别出的雨流循环列表。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="load"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="load"/> 长度小于 3 时抛出。</exception>
    public static IReadOnlyList<RainflowCycle> Count(double[] load)
    {
        SignalGuard.NotEmpty(load);
        if (load.Length < 3)
        {
            throw new ArgumentException("Load series must have at least 3 points.", nameof(load));
        }

        var turningPoints = ExtractTurningPoints(load);
        var cycles = new List<RainflowCycle>();
        var stack = new List<double>(turningPoints);

        var index = 0;
        while (index < stack.Count)
        {
            if (stack.Count < 3)
            {
                index++;
                continue;
            }

            var s0 = stack[stack.Count - 3];
            var s1 = stack[stack.Count - 2];
            var s2 = stack[stack.Count - 1];
            var range01 = Math.Abs(s1 - s0);
            var range12 = Math.Abs(s2 - s1);

            if (range12 >= range01)
            {
                cycles.Add(new RainflowCycle
                {
                    Range = range12,
                    Mean = (s1 + s2) / 2.0,
                    Count = 1.0
                });
                stack.RemoveAt(stack.Count - 1);
                stack.RemoveAt(stack.Count - 1);
                index = Math.Max(0, stack.Count - 2);
            }
            else
            {
                index++;
            }
        }

        for (var i = 1; i < stack.Count; i++)
        {
            cycles.Add(new RainflowCycle
            {
                Range = Math.Abs(stack[i] - stack[i - 1]),
                Mean = (stack[i] + stack[i - 1]) / 2.0,
                Count = 0.5
            });
        }

        return cycles;
    }

    /// <summary>
    /// 将雨流计数结果聚合为幅值直方图。
    /// </summary>
    /// <param name="cycles">雨流循环列表，由 <see cref="Count"/> 生成。</param>
    /// <param name="binCount">幅值分箱数，默认 20，须大于 0。</param>
    /// <returns>雨流直方图 <see cref="RainflowHistogram"/>。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="cycles"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当循环列表为空时抛出。</exception>
    public static RainflowHistogram BuildHistogram(IReadOnlyList<RainflowCycle> cycles, int binCount = 20)
    {
        if (cycles is null)
        {
            throw new ArgumentNullException(nameof(cycles));
        }

        if (cycles.Count == 0)
        {
            throw new ArgumentException("Cycles must not be empty.", nameof(cycles));
        }

        SignalGuard.Positive(binCount, nameof(binCount));

        var maxRange = cycles.Max(c => c.Range);
        if (maxRange <= 0)
        {
            return new RainflowHistogram
            {
                RangeBins = new double[binCount],
                Counts = new double[binCount],
                TotalCycles = 0
            };
        }

        var bins = new double[binCount];
        var counts = new double[binCount];
        var binWidth = maxRange / binCount;

        for (var i = 0; i < binCount; i++)
        {
            bins[i] = (i + 0.5) * binWidth;
        }

        foreach (var cycle in cycles)
        {
            var binIndex = (int)Math.Min(binCount - 1, cycle.Range / binWidth);
            counts[binIndex] += cycle.Count;
        }

        return new RainflowHistogram
        {
            RangeBins = bins,
            Counts = counts,
            TotalCycles = counts.Sum()
        };
    }

    private static List<double> ExtractTurningPoints(double[] data)
    {
        var points = new List<double> { data[0] };
        for (var i = 1; i < data.Length - 1; i++)
        {
            var rising = data[i] > data[i - 1] && data[i] >= data[i + 1];
            var falling = data[i] < data[i - 1] && data[i] <= data[i + 1];
            if (rising || falling)
            {
                points.Add(data[i]);
            }
        }

        points.Add(data[data.Length - 1]);
        return points;
    }
}
