namespace NVHAlgorithmKit.Windows;

/// <summary>
/// 窗函数类型，用于 FFT 前加窗以抑制频谱泄漏。
/// </summary>
public enum WindowType
{
    /// <summary>矩形窗（不加窗），频率分辨率最高，旁瓣泄漏最大。</summary>
    Rectangular,

    /// <summary>Hanning 窗，通用平衡型，旁瓣抑制适中。</summary>
    Hanning,

    /// <summary>Hamming 窗，旁瓣抑制优于 Hanning，主瓣略宽。</summary>
    Hamming,

    /// <summary>Blackman 窗，旁瓣抑制好，主瓣较宽，幅值精度一般。</summary>
    Blackman,

    /// <summary>Blackman-Harris 窗，极低旁瓣，适用于微弱信号检测。</summary>
    BlackmanHarris,

    /// <summary>Flat-Top 窗，幅值测量精度最高，频率分辨率最低。</summary>
    FlatTop,

    /// <summary>Kaiser 窗，可通过 Beta 参数调节主瓣/旁瓣权衡。</summary>
    Kaiser
}
