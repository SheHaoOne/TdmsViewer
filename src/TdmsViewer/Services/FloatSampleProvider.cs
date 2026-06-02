using System;
using NAudio.Wave;

namespace TdmsViewer.Services;

/// <summary>
/// 从内存 float 样本缓冲提供音频。
/// </summary>
internal sealed class FloatSampleProvider : ISampleProvider
{
    private readonly float[] _samples;
    private int _position;

    public FloatSampleProvider(float[] samples, WaveFormat waveFormat)
    {
        _samples = samples;
        WaveFormat = waveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        Array.Clear(buffer, offset, count);

        var available = _samples.Length - _position;
        if (available <= 0)
            return 0;

        var toRead = Math.Min(count, available);
        Array.Copy(_samples, _position, buffer, offset, toRead);
        _position += toRead;
        return toRead;
    }
}
