# NVHAlgorithmKit

C# 通用跨平台 NVH（Noise, Vibration, Harshness — 噪声、振动、声振粗糙度）信号处理算法库。

支持 **.NET Framework 4.7.2+**、**.NET 6**、**.NET 8+** 及 **.NET Standard 2.0** 兼容平台，可在 Windows、Linux、macOS 使用，无原生依赖。

## 平台兼容性

| 调用方 | 支持方式 | 说明 |
|--------|----------|------|
| **.NET Framework 4.7.2+** | 直接引用 `net472` 程序集，或引用 `netstandard2.0` | 推荐 .NET Framework 4.7.2 / 4.8 |
| **.NET 6** | 直接引用 `net6.0` 程序集，或引用 `netstandard2.0` | 类库多目标已包含 `net6.0` |
| **.NET 8+** | 直接引用 `net8.0` 程序集 | 当前开发/测试目标框架 |
| **.NET Standard 2.0** 项目 | 引用 `netstandard2.0` 程序集 | 适用于 .NET Core 2.x / 3.x |

库本身多目标编译输出：

```
netstandard2.0 | net472 | net6.0 | net8.0
```

NuGet 包发布后会自动为各目标框架选取最优程序集。

## 功能模块

| 模块 | 说明 |
|------|------|
| **Core** | 信号容器 `NvhSignal`、数学工具、参数校验 |
| **Windows** | Hanning、Hamming、Blackman、Flat-Top、Kaiser 等窗函数 |
| **Transform** | Cooley-Tukey 基-2 FFT / 逆 FFT |
| **TimeDomain** | RMS、峰值、波峰因子、自/互相关 |
| **FrequencyDomain** | 频谱、Welch PSD、STFT、倒谱、相干函数、FRF(H1/H2) |
| **Filtering** | Biquad IIR、Butterworth 低/高/带通、FIR |
| **Acoustics** | A/C/Z 计权、倍频程、SPL、**声品质指标** |
| **Vibration** | 加速度积分（速度/位移）、包络分析 |
| **Order** | 阶次谱、**Campbell 图** |
| **Transform** | FFT、**Morlet 小波 CWT** |
| **Modal** | 共振峰拾取、半功率带宽阻尼估算 |
| **Fatigue** | 雨流计数、载荷直方图 |
| **Core+** | 数字重采样、RPM 提取、Zwicker 响度 |

## 快速开始

```csharp
using NVHAlgorithmKit;
using NVHAlgorithmKit.Core;

// 构造信号（采样率 Hz）
var samples = LoadSamples(); // double[]
var signal = new NvhSignal(samples, sampleRate: 48000);

// 时域特征
var features = NvhAnalyzer.AnalyzeTimeDomain(signal);
Console.WriteLine($"RMS: {features.Rms:F4}, 波峰因子: {features.CrestFactor:F2}");

// 频谱分析
var spectrum = NvhAnalyzer.AnalyzeSpectrum(signal);

// A 计权声压级
var spl = NvhAnalyzer.ComputeSpl(signal);
Console.WriteLine($"SPL(A): {spl:F1} dB");

// STFT 时频分析
var stft = NvhAnalyzer.AnalyzeStft(signal);

// 倒谱（齿轮/轴承故障）
var cepstrum = NvhAnalyzer.AnalyzeCepstrum(signal);

// 双通道相干函数与传递函数
var coherence = NvhAnalyzer.AnalyzeCoherence(inputSignal, outputSignal);
var frf = NvhAnalyzer.AnalyzeTransferFunctionH1(inputSignal, outputSignal);

// Campbell 图（扫频工况）
var campbell = NvhAnalyzer.AnalyzeCampbellDiagram(vibrationSignal, rpmTrace);

// 小波变换 + 模态识别 + 声品质
var cwt = NvhAnalyzer.AnalyzeWavelet(signal);
var modes = NvhAnalyzer.IdentifyModalParameters(spectrum);
var sq = NvhAnalyzer.AnalyzeSoundQuality(signal);
var loudness = NvhAnalyzer.AnalyzeLoudness(signal);
var rpm = NvhAnalyzer.ExtractRpmFromTachometer(tachSignal);
var cycles = NvhAnalyzer.AnalyzeRainflow(loadSignal);
var resampled = NvhAnalyzer.Resample(signal, targetSampleRate: 16000);

// 加速度 → 速度
var velocity = NvhAnalyzer.IntegrateToVelocity(signal);
```

## 项目结构

```
NVHAlgorithmKit/
├── src/NVHAlgorithmKit/          # 核心算法库
│   ├── Core/
│   ├── Windows/
│   ├── Transform/
│   ├── TimeDomain/
│   ├── FrequencyDomain/
│   ├── Filtering/
│   ├── Acoustics/
│   ├── Vibration/
│   ├── Order/
│   └── NvhAnalyzer.cs            # 统一入口
└── tests/NVHAlgorithmKit.Tests/  # 单元测试
```

## 构建与测试

```bash
dotnet build
dotnet test
```

## 引用方式

**SDK 风格项目（.NET 6 / .NET 8）：**

```bash
dotnet add reference path/to/NVHAlgorithmKit/NVHAlgorithmKit.csproj
```

**.NET Framework 4.7.2+（Visual Studio）：**

1. 添加现有项目 `src/NVHAlgorithmKit/NVHAlgorithmKit.csproj`，或
2. 引用编译产物 `NVHAlgorithmKit.dll`（`bin/Release/net472/`）

```xml
<!-- packages.config 或 PackageReference 均可 -->
<ProjectReference Include="..\NVHAlgorithmKit\src\NVHAlgorithmKit\NVHAlgorithmKit.csproj" />
```

**.NET 6 项目示例：**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\NVHAlgorithmKit\src\NVHAlgorithmKit\NVHAlgorithmKit.csproj" />
  </ItemGroup>
</Project>
```

## 设计原则

- **纯托管代码**：不依赖 FFTW、Intel MKL 等原生库，便于跨平台部署
- **零外部 NuGet 依赖**：核心库仅使用 BCL（`System.Numerics` 等）
- **模块化 API**：可按需引用底层模块，也可通过 `NvhAnalyzer` 快速调用
- **流式友好**：`IFilter` 接口支持逐样本 `ProcessSample` 实时处理

## 许可证

MIT License — 详见 [LICENSE](LICENSE)
