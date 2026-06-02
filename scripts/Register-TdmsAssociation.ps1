# 以当前用户身份注册 .tdms 与 TdmsViewer 的文件关联
param(
    [string]$ExePath = ""
)

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $buildPath = Join-Path $PSScriptRoot "..\src\TdmsViewer\bin\Release\net8.0-windows\TdmsViewer.exe"
    if (Test-Path $buildPath) {
        $ExePath = (Resolve-Path $buildPath).Path
    } else {
        Write-Error "请指定 TdmsViewer.exe 路径，或先执行 dotnet build -c Release"
        exit 1
    }
}

$progId = "TdmsViewer.Document"
$extension = ".tdms"

New-Item -Path "HKCU:\Software\Classes\$extension" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\$extension" -Name "(default)" -Value $progId

New-Item -Path "HKCU:\Software\Classes\$progId" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\$progId" -Name "(default)" -Value "TDMS 数据文件"

New-Item -Path "HKCU:\Software\Classes\$progId\DefaultIcon" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\$progId\DefaultIcon" -Name "(default)" -Value "`"$ExePath`",0"

New-Item -Path "HKCU:\Software\Classes\$progId\shell\open\command" -Force | Out-Null
Set-ItemProperty -Path "HKCU:\Software\Classes\$progId\shell\open\command" -Name "(default)" -Value "`"$ExePath`" `"%1`""

Write-Host "已注册: $extension -> $ExePath"
