namespace NVHAlgorithmKit.Core;

/// <summary>
/// 信号参数校验工具，在算法入口处统一检查输入合法性。
/// </summary>
public static class SignalGuard
{
    /// <summary>
    /// 校验数组引用不为 <see langword="null"/>。
    /// </summary>
    /// <typeparam name="T">数组元素类型。</typeparam>
    /// <param name="data">待校验的数组。</param>
    /// <param name="paramName">参数名称，用于异常信息，默认为 "data"。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    public static void NotNull<T>(T[]? data, string paramName = "data")
    {
        if (data is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// 校验数组不为 <see langword="null"/> 且至少包含一个元素。
    /// </summary>
    /// <param name="data">待校验的时域采样序列，单位取决于具体算法（如 Pa、m/s²）。</param>
    /// <param name="paramName">参数名称，用于异常信息，默认为 "data"。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="data"/> 长度为 0 时抛出。</exception>
    public static void NotEmpty(double[] data, string paramName = "data")
    {
        NotNull(data, paramName);
        if (data.Length == 0)
        {
            throw new ArgumentException("Signal must not be empty.", paramName);
        }
    }

    /// <summary>
    /// 校验双精度浮点值为正数（大于 0）。
    /// </summary>
    /// <param name="value">待校验的数值，如采样率（Hz）、截止频率（Hz）等。</param>
    /// <param name="paramName">参数名称，用于异常信息。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="value"/> 小于等于 0 时抛出。</exception>
    public static void Positive(double value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.");
        }
    }

    /// <summary>
    /// 校验整型值为正数（大于 0）。
    /// </summary>
    /// <param name="value">待校验的整数值，如 FFT 长度、滤波器阶数等。</param>
    /// <param name="paramName">参数名称，用于异常信息。</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="value"/> 小于等于 0 时抛出。</exception>
    public static void Positive(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.");
        }
    }

    /// <summary>
    /// 校验两个时域信号数组长度一致。
    /// </summary>
    /// <param name="a">第一个信号数组。</param>
    /// <param name="b">第二个信号数组，须与 <paramref name="a"/> 等长。</param>
    /// <param name="paramName">参数名称，用于异常信息，默认为 "data"。</param>
    /// <exception cref="ArgumentNullException">当任一数组为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当两个数组长度不一致时抛出。</exception>
    public static void SameLength(double[] a, double[] b, string paramName = "data")
    {
        NotEmpty(a);
        NotNull(b, paramName);
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Signals must have the same length.", paramName);
        }
    }
}
