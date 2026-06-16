# TdmsViewer

基于 WPF 的 **TDMS 文件查看器**，采用 macOS 风格界面，支持通道属性卡片、波形展示、数据分页、在线播放与 WAV 导出。

## 功能

| 功能 | 说明 |
|------|------|
| 批量导入 | 一次选择多个 `.tdms`；拖放可追加导入；已移除原单文件「导入」 |
| 波形叠加 | 当前选定组内，相同**通道名**的波形在同一图中叠加（ScottPlot 多色曲线 + 图例） |
| **NVH 分析编排** | 可勾选组合时域、频谱、倍频程、SPL、PSD、STFT 等步骤；方案可保存/加载 JSON |
| **批量分析** | 对当前通道在所有已导入文件中运行同一方案，生成指标对比表 |
| **分析大屏** | 应用内 Mac 风格报表大屏（ECharts），报表历史列表，可导出 HTML |
| 文件关联 | 双击 `.tdms` 加入查看会话；PowerShell 脚本注册 |
| 组列表 | 选中文件后显示其 TDMS 组，默认选中第一组，可切换组并查看组属性 |
| 通道列表 | 仅显示当前选定组内的通道，显示可叠加的文件数量 |
| 属性 / 数据 | 组属性与通道属性分开展示；多文件时单击文件名切换数据来源 |
| 波形图 | ScottPlot 绘制，自动降采样，支持 `wf_increment` 时间轴与缩放/平移 |
| 数据分页 | 每页 100 条，首页 / 上一页 / 下一页 / 末页 |
| 音频 | 将通道数据归一化后播放；可导出为 WAV（采样率取自通道属性） |

## 环境要求

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022（可选，含「.NET 桌面开发」工作负载）

## 构建与运行

```bash
cd /path/to/TdmsViewer
dotnet restore
dotnet build -c Release
dotnet run --project src/TdmsViewer/TdmsViewer.csproj
```

发布单文件（可选）：

```bash
dotnet publish src/TdmsViewer/TdmsViewer.csproj -c Release -r win-x64 --self-contained false
```

## 文件关联

**方式一（推荐）**：在应用中点击「关联 .tdms」。

**方式二**：PowerShell（当前用户，无需管理员）：

```powershell
.\scripts\Register-TdmsAssociation.ps1 -ExePath "C:\path\to\TdmsViewer.exe"
```

**方式三**：命令行传入一个或多个文件：

```text
TdmsViewer.exe "D:\data\a.tdms" "D:\data\b.tdms"
```

## 技术栈

- **.NET 8** + **WPF**
- **[TDMSReader](https://www.nuget.org/packages/TDMSReader)** — 读取 NI TDMS 文件
- **[NVHAlgorithmKit](https://github.com/SheHaoOne/NVHAlgorithmKit)** — NVH 信号处理（时域 / 频域 / 声学）
- **[Microsoft WebView2](https://developer.microsoft.com/microsoft-edge/webview2/)** — 应用内分析报表大屏
- **[NAudio](https://www.nuget.org/packages/NAudio)** — 播放与 WAV 导出
- **[ScottPlot.WPF](https://www.nuget.org/packages/ScottPlot.WPF)** — 波形图
- **[CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)** — MVVM

## NVH 分析与报表

1. 导入 TDMS，选择组与通道，单击文件名加载数据。
2. 切换到 **分析编排**，勾选步骤（可选 **批量分析**），点击 **运行分析**。
3. 自动跳转 **分析大屏**；左侧 **报表历史** 可切换单次结果，批量模式显示对比表。
4. 方案可 **保存/加载** 为 `.tdms-analysis.json`；可 **导出 HTML**。

分析步骤依赖通道采样率（`wf_increment` 或 `NI_SampleRate`）。

## 说明

- TDMSReader 主要支持 TDMS 1.x 及常见数值通道；DAQmx 原始数据类型可能无法读取。
- 音频播放将通道数值线性归一化到 16-bit PCM，采样率优先使用属性 `wf_increment`（1/增量）或 `NI_SampleRate`，否则默认 44100 Hz。
- 界面为 macOS 风格（浅灰背景、圆角卡片、#007AFF 强调色），在 Windows 上使用 Segoe UI / 微软雅黑。

## 许可证

见 [LICENSE](LICENSE)。
