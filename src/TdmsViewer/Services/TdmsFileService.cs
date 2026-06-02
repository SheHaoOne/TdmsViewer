using System.Globalization;
using NationalInstruments.Tdms;
using TdmsViewer.Models;

namespace TdmsViewer.Services;

public sealed class TdmsFileService
{
    public IReadOnlyList<TdmsChannelInfo> LoadChannels(string filePath)
    {
        using var file = new File(filePath);
        file.Open();

        var channels = new List<TdmsChannelInfo>();

        foreach (var group in file)
        {
            foreach (var channel in group)
            {
                var props = channel.Properties
                    .ToDictionary(p => p.Key, p => (object?)p.Value);

                var dataTypeName = GetDataTypeName(channel);
                var sampleCount = TryGetSampleCount(channel, dataTypeName);
                var sampleRate = TryGetSampleRateHz(props);

                channels.Add(new TdmsChannelInfo
                {
                    GroupName = group.Name,
                    ChannelName = channel.Name,
                    DisplayName = $"{group.Name} / {channel.Name}",
                    DataTypeName = dataTypeName,
                    SampleCount = sampleCount,
                    SampleRateHz = sampleRate,
                    Properties = props
                });
            }
        }

        return channels;
    }

    public double[] ReadChannelData(string filePath, TdmsChannelInfo channelInfo)
    {
        using var file = new File(filePath);
        file.Open();

        var channel = file.Groups[channelInfo.GroupName].Channels[channelInfo.ChannelName];
        return ReadAsDoubles(channel, channelInfo.DataTypeName);
    }

    public IReadOnlyList<DataPageRow> GetPage(
        double[] data,
        int pageIndex,
        int pageSize,
        CultureInfo culture)
    {
        if (data.Length == 0)
            return Array.Empty<DataPageRow>();

        var start = pageIndex * pageSize;
        if (start >= data.Length)
            return Array.Empty<DataPageRow>();

        var count = Math.Min(pageSize, data.Length - start);
        var rows = new List<DataPageRow>(count);

        for (var i = 0; i < count; i++)
        {
            var idx = start + i;
            var value = data[idx];
            rows.Add(new DataPageRow
            {
                Index = idx,
                Value = value,
                FormattedValue = value.ToString("G9", culture)
            });
        }

        return rows;
    }

    public IReadOnlyList<ChannelPropertyCard> BuildPropertyCards(TdmsChannelInfo channel)
    {
        return channel.Properties
            .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .Select(p => new ChannelPropertyCard
            {
                Key = p.Key,
                Value = FormatPropertyValue(p.Value)
            })
            .ToList();
    }

    public IReadOnlyList<WaveformPoint> BuildWaveform(
        double[] data,
        double? sampleRateHz,
        int maxPoints = 4000)
    {
        if (data.Length == 0)
            return Array.Empty<WaveformPoint>();

        var step = Math.Max(1, data.Length / maxPoints);
        var points = new List<WaveformPoint>((data.Length / step) + 1);
        var dt = sampleRateHz is > 0 ? 1.0 / sampleRateHz.Value : 1.0;

        for (var i = 0; i < data.Length; i += step)
            points.Add(new WaveformPoint { X = i * dt, Y = data[i] });

        if ((data.Length - 1) % step != 0)
        {
            var last = data.Length - 1;
            points.Add(new WaveformPoint { X = last * dt, Y = data[last] });
        }

        return points;
    }

    private static string GetDataTypeName(Channel channel)
    {
        if (channel.Properties.TryGetValue("NI_DataType", out var niType) && niType != null)
            return niType.ToString() ?? "Unknown";

        try
        {
            _ = channel.GetData<double>().Take(1).ToArray();
            return "Double";
        }
        catch
        {
            // fall through
        }

        foreach (var typeName in new[] { "Single", "Int32", "Int16", "Int64", "UInt16", "UInt32", "Byte" })
        {
            try
            {
                ReadByTypeName(channel, typeName).Take(1).ToArray();
                return typeName;
            }
            catch
            {
                // try next
            }
        }

        return "Unknown";
    }

    private static long TryGetSampleCount(Channel channel, string dataTypeName)
    {
        try
        {
            return ReadByTypeName(channel, dataTypeName).LongCount();
        }
        catch
        {
            if (channel.Properties.TryGetValue("NI_ChannelLength", out var len) &&
                long.TryParse(len?.ToString(), out var count))
                return count;
            return 0;
        }
    }

    private static double? TryGetSampleRateHz(IReadOnlyDictionary<string, object?> props)
    {
        if (TryGetDouble(props, "wf_increment", out var increment) && increment > 0)
            return 1.0 / increment;

        if (TryGetDouble(props, "NI_SampleRate", out var rate) && rate > 0)
            return rate;

        return null;
    }

    private static bool TryGetDouble(IReadOnlyDictionary<string, object?> props, string key, out double value)
    {
        value = 0;
        if (!props.TryGetValue(key, out var raw) || raw == null)
            return false;

        return raw switch
        {
            double d => (value = d) > 0 || d == 0,
            float f => (value = f) > 0 || f == 0,
            int i => (value = i) > 0 || i == 0,
            long l => (value = l) > 0 || l == 0,
            _ => double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        };
    }

    private static IEnumerable<double> ReadByTypeName(Channel channel, string dataTypeName) =>
        dataTypeName switch
        {
            "Single" => channel.GetData<float>().Select(v => (double)v),
            "Int32" => channel.GetData<int>().Select(v => (double)v),
            "Int16" => channel.GetData<short>().Select(v => (double)v),
            "Int64" => channel.GetData<long>().Select(v => (double)v),
            "UInt16" => channel.GetData<ushort>().Select(v => (double)v),
            "UInt32" => channel.GetData<uint>().Select(v => (double)v),
            "Byte" => channel.GetData<byte>().Select(v => (double)v),
            _ => channel.GetData<double>()
        };

    private static double[] ReadAsDoubles(Channel channel, string dataTypeName) =>
        ReadByTypeName(channel, dataTypeName).ToArray();

    private static string FormatPropertyValue(object? value) =>
        value switch
        {
            null => "—",
            double d => d.ToString("G9", CultureInfo.InvariantCulture),
            float f => f.ToString("G9", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("O"),
            _ => value.ToString() ?? "—"
        };
}
