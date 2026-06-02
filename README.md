# TdmsViewer

基于 WPF 的 **TDMS 文件查看器**，采用 macOS 风格界面，支持通道属性卡片、波形展示、数据分页、在线播放与 WAV 导出。

## 功能

| 功能 | 说明 |
|------|------|
| 打开 / 导入 | 工具栏「打开」「导入」，或拖放 `.tdms` 到窗口 |
| 文件关联 | 应用内「关联 .tdms」或运行 `scripts/Register-TdmsAssociation.ps1`，双击即可打开 |
| 通道列表 | 左侧列表选择通道，默认展示第一个通道 |
| 属性卡片 | 网格卡片展示当前通道全部属性 |
| 波形图 | 自动降采样绘制，支持 `wf_increment` 时间轴 |
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

**方式三**：命令行传入文件：

```text
TdmsViewer.exe "D:\data\sample.tdms"
```

## 技术栈

- **.NET 8** + **WPF**
- **[TDMSReader](https://www.nuget.org/packages/TDMSReader)** — 读取 NI TDMS 文件
- **[NAudio](https://www.nuget.org/packages/NAudio)** — 播放与 WAV 导出
- **[CommunityToolkit.Mvvm](https://www.nuget.org/packages/CommunityToolkit.Mvvm)** — MVVM

## 说明

- TDMSReader 主要支持 TDMS 1.x 及常见数值通道；DAQmx 原始数据类型可能无法读取。
- 音频播放将通道数值线性归一化到 16-bit PCM，采样率优先使用属性 `wf_increment`（1/增量）或 `NI_SampleRate`，否则默认 44100 Hz。
- 界面为 macOS 风格（浅灰背景、圆角卡片、#007AFF 强调色），在 Windows 上使用 Segoe UI / 微软雅黑。

## 许可证

见 [LICENSE](LICENSE)。
