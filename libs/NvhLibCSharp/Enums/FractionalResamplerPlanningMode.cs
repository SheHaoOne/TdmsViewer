namespace NvhLibCSharp.Enums
{
    /// <summary>
    /// 控制分数重采样器在 stage 分解规划阶段投入的搜索成本。
    /// </summary>
    public enum FractionalResamplerPlanningMode
    {
        /// <summary>
        /// 快速规划，偏向较短的 plan 创建时间。
        /// </summary>
        Fast,

        /// <summary>
        /// 默认规划，在 plan 创建成本和执行成本之间折中。
        /// </summary>
        Balanced,

        /// <summary>
        /// 更积极地搜索较低执行成本的分解，plan 创建可能更慢。
        /// </summary>
        Patient
    }
}
