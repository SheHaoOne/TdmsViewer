using System.IO;
using NAudio.Wave;

namespace TdmsViewer.Services;

public sealed class AudioService : IDisposable
{
    private WaveOutEvent? _player;
    private ISampleProvider? _sampleProvider;

    public const int DefaultSampleRate = 44100;

    public void PlayFromChannelData(double[] data, double? sampleRateHz)
    {
        Stop();

        var rate = (int)Math.Clamp(sampleRateHz ?? DefaultSampleRate, 8000, 192000);
        var samples = NormalizeToPcm16(data);
        var format = WaveFormat.CreateIeeeFloatWaveFormat(rate, 1);

        _sampleProvider = new FloatSampleProvider(samples, format);
        _player = new WaveOutEvent();
        _player.Init(_sampleProvider);
        _player.Play();
    }

    public void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;
        _sampleProvider = null;
    }

    public void ExportWav(string filePath, double[] data, double? sampleRateHz)
    {
        var rate = (int)Math.Clamp(sampleRateHz ?? DefaultSampleRate, 8000, 192000);
        var normalized = NormalizeToPcm16(data);

        using var writer = new WaveFileWriter(filePath, new WaveFormat(rate, 16, 1));
        writer.WriteSamples(normalized, 0, normalized.Length);
    }

    private static float[] NormalizeToPcm16(double[] data)
    {
        if (data.Length == 0)
            return Array.Empty<float>();

        var max = data.Select(Math.Abs).DefaultIfEmpty(1).Max();
        if (max < 1e-12)
            max = 1;

        var scale = (float)(0.95 / max);
        return data.Select(v => (float)(v * scale)).ToArray();
    }

    public void Dispose() => Stop();
}
