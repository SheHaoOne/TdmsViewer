using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace TdmsViewer.Services;

public sealed class AudioService : IDisposable
{
    private const int MaxPlaybackSamples = 4_000_000;

    private WaveOutEvent? _player;
    private AudioFileReader? _reader;
    private string? _tempWavPath;

    public event EventHandler? PlaybackStopped;

    public const int DefaultSampleRate = 44100;

    public void PlayFromChannelData(double[] data, double? sampleRateHz)
    {
        Stop();

        if (data.Length == 0)
            throw new InvalidOperationException("通道数据为空，无法播放。");

        var rate = ResolveSampleRate(sampleRateHz);
        var playbackSamples = BuildPlaybackSamples(data);

        // 优先使用临时 WAV + AudioFileReader，在 Windows 上最稳定
        try
        {
            PlayViaTempWavFile(playbackSamples, rate);
        }
        catch (Exception ex) when (IsDeviceError(ex))
        {
            PlayViaWaveOut(playbackSamples, rate);
        }
    }

    private void PlayViaWaveOut(float[] samples, int sampleRate)
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        ISampleProvider sampleProvider = new FloatSampleProvider(samples, format);
        IWaveProvider waveProvider = new SampleToWaveProvider16(sampleProvider);

        _player = new WaveOutEvent();
        _player.PlaybackStopped += OnPlayerPlaybackStopped;
        _player.Init(waveProvider);
        _player.Play();
    }

    private void PlayViaTempWavFile(float[] samples, int sampleRate)
    {
        _tempWavPath = Path.Combine(Path.GetTempPath(), $"TdmsViewer_{Guid.NewGuid():N}.wav");
        using (var writer = new WaveFileWriter(_tempWavPath, new WaveFormat(sampleRate, 16, 1)))
            writer.WriteSamples(samples, 0, samples.Length);

        _reader = new AudioFileReader(_tempWavPath);
        _player = new WaveOutEvent();
        _player.PlaybackStopped += OnPlayerPlaybackStopped;
        _player.Init(_reader);
        _player.Play();
    }

    private void OnPlayerPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (sender is not WaveOutEvent player || player != _player)
            return;

        CleanupPlayback();
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (_player == null)
            return;

        _player.PlaybackStopped -= OnPlayerPlaybackStopped;
        _player.Stop();
        CleanupPlayback();
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    private void CleanupPlayback()
    {
        _player?.Dispose();
        _player = null;

        _reader?.Dispose();
        _reader = null;

        if (_tempWavPath != null)
        {
            try
            {
                if (File.Exists(_tempWavPath))
                    File.Delete(_tempWavPath);
            }
            catch
            {
                // ignore temp cleanup errors
            }

            _tempWavPath = null;
        }
    }

    public void ExportWav(string filePath, double[] data, double? sampleRateHz)
    {
        var rate = ResolveSampleRate(sampleRateHz);
        var samples = BuildPlaybackSamples(data);

        using var writer = new WaveFileWriter(filePath, new WaveFormat(rate, 16, 1));
        writer.WriteSamples(samples, 0, samples.Length);
    }

    private static int ResolveSampleRate(double? sampleRateHz)
    {
        if (sampleRateHz is > 0 and <= 384_000)
            return (int)Math.Round(sampleRateHz.Value);

        return DefaultSampleRate;
    }

    private static float[] BuildPlaybackSamples(double[] data)
    {
        var working = data;

        if (data.Length > MaxPlaybackSamples)
        {
            var step = (double)data.Length / MaxPlaybackSamples;
            var down = new double[MaxPlaybackSamples];
            for (var i = 0; i < down.Length; i++)
                down[i] = data[(int)(i * step)];
            working = down;
        }

        return NormalizeToFloat(working);
    }

    private static float[] NormalizeToFloat(double[] data)
    {
        if (data.Length == 0)
            return Array.Empty<float>();

        var max = data.Select(Math.Abs).DefaultIfEmpty(1).Max();
        if (max < 1e-12)
            max = 1;

        var scale = (float)(0.95 / max);
        var result = new float[data.Length];
        for (var i = 0; i < data.Length; i++)
            result[i] = (float)(data[i] * scale);

        return result;
    }

    private static bool IsDeviceError(Exception ex) =>
        ex is InvalidOperationException or MmException or Win32Exception;

    public void Dispose() => Stop();
}
