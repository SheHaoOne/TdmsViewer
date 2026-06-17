namespace NvhLibCSharp.Options
{
    /// <summary>
    /// 缩放选项配置类，用于定义数据输出的缩放方式和参考值
    /// </summary>
    public class ScaleOptions(Scale scaleType, double referenceValue = 1.0)
    {
        /// <summary>
        /// 缩放类型，用于转换输出格式为 dB 或 Lin
        /// </summary>
        public Scale Scale { get; } = scaleType;

        /// <summary>
        /// 参考值，默认为 1.0。
        /// 仅在 Scale 为 dB 时生效
        /// </summary>
        public double ReferenceValue { get; } = referenceValue;
    }
}
