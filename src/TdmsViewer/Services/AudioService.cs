using System.IO;
using NAudio.Wave;
using TdmsViewer.Models;

namespace TdmsViewer.Services;

public sealed class AudioService : IDisposable
{
    private WaveOutEvent? _player;
    private WaveStream? _waveStream;

    public const int DefaultSampleRate = 44100;

    public void PlayFromChannelData(double[] data, double? sampleRateHz)
    {
        Stop();
        var wav = BuildWaveStream(data, sampleRateHz ?? DefaultSampleRate);
        _waveStream = new WaveFileReader(wav);
        _player = new WaveOutEvent();
        _player.Init(_waveStream);
        _player.Play();
    }

    public void Stop()
    {
        _player?.Stop();
        _player?.Dispose();
        _player = null;
        _waveStream?.Dispose();
        _waveStream = null;
    }

    public void ExportWav(string filePath, double[] data, double? sampleRateHz)
    {
        var rate = (int)Math.Clamp(sampleRateHz ?? DefaultSampleRate, 8000, 192000);
        var normalized = NormalizeToPcm16(data);

        using var writer = new WaveFileWriter(filePath, new WaveFormat(rate, 16, 1));
        writer.WriteSamples(normalized, 0, normalized.Length);
    }

    public MemoryStream BuildWaveStream(double[] data, double sampleRateHz)
    {
        var rate = (int)Math.Clamp(sampleRateHz, 8000, 192000);
        var normalized = NormalizeToPcm16(data);
        var stream = new MemoryStream();
        using (var writer = new WaveFileWriter(stream, new WaveFormat(rate, 16, 1)))
            writer.WriteSamples(normalized, 0, normalized.Length);

        stream.Position = 0;
        return stream;
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
