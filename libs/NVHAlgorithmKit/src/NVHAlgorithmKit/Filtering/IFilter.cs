namespace NVHAlgorithmKit.Filtering;

/// <summary>
/// 数字滤波器统一接口，支持逐样本流式处理与块处理。
/// </summary>
public interface IFilter
{
    /// <summary>
    /// 处理单个采样点，适用于实时流式场景。
    /// </summary>
    /// <param name="sample">当前输入采样值。</param>
    /// <returns>滤波后的输出采样值。</returns>
    double ProcessSample(double sample);

    /// <summary>
    /// 批量处理一段时域信号。
    /// </summary>
    /// <param name="data">输入时域采样序列。</param>
    /// <returns>滤波后的输出数组，长度与输入相同。</returns>
    double[] ProcessBlock(double[] data);

    /// <summary>
    /// 重置滤波器内部状态（延迟单元归零），用于处理不连续的信号段。
    /// </summary>
    void Reset();
}
