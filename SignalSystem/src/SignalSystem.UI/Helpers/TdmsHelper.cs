using System.Text;
using SignalSystem.Contracts.Models;

namespace SignalSystem.UI.Helpers;

/// <summary>
/// TDMS 文件读写帮助类（纯 C# 实现）
///
/// TDMS (Technical Data Management Streaming) 简化实现:
/// - 文件由 Segment 组成
/// - 每个 Segment: LeadIn(28B) + MetaData + RawData
/// - 支持 Float32 通道数据
///
/// 本实现写入一个 Segment，包含所有通道的 interleaved 原始数据。
/// </summary>
public static class TdmsHelper
{
    // TDMS Tag "TDSm" (little-endian)
    private static readonly byte[] TdmsTag = "TDSm"u8.ToArray();

    // ToC mask bits
    private const uint kTocMetaData = 1 << 1;
    private const uint kTocRawData = 1 << 3;
    private const uint kTocInterleavedData = 1 << 5;

    // TDMS data type codes
    private const uint TdsTypeString = 0x20;
    private const uint TdsTypeFloat32 = 0x0A; // tdsTypeSingleFloat
    private const uint TdsTypeFloat64 = 0x0B;
    private const uint TdsTypeTimestamp = 0x44;

    /// <summary>
    /// 将 SignalDataFile 写入 .tdms 文件
    /// </summary>
    public static async Task WriteAsync(string filePath, SignalDataFile data)
    {
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true);

        // --- 构建 metadata 和 raw data ---
        var metaStream = new MemoryStream();
        var metaWriter = new BinaryWriter(metaStream, Encoding.UTF8);
        var rawStream = new MemoryStream();
        var rawWriter = new BinaryWriter(rawStream, Encoding.UTF8);

        // 收集所有对象（group + channels）
        var objectInfos = new List<TdmsObjectInfo>();

        // 文件级属性（root "/"）
        var rootProps = new List<(string name, uint type, byte[] value)>
        {
            StringProperty("CreatedUtc", data.CreatedUtc.ToString("O")),
            Int32Property("TotalFrames", data.TotalFrames),
        };
        objectInfos.Add(new TdmsObjectInfo { Path = "/", Properties = rootProps });

        // 每个设备是一个 Group，每个通道是 Group 下的 Channel
        foreach (var dev in data.Devices)
        {
            string groupPath = $"/'{dev.DeviceId}'";

            // Group 对象（无 raw data）
            objectInfos.Add(new TdmsObjectInfo
            {
                Path = groupPath,
                Properties = new List<(string, uint, byte[])>
                {
                    StringProperty("DeviceId", dev.DeviceId),
                },
            });

            foreach (var ch in dev.Channels)
            {
                string channelPath = $"/'{dev.DeviceId}'/'{ch.ChannelId}'";

                var chProps = new List<(string name, uint type, byte[] value)>
                {
                    StringProperty("SignalType", ch.SignalType),
                    Float64Property("SampleRate", ch.SampleRate),
                    Int32Property("FrameCount", ch.FrameCount),
                };

                long rawOffset = rawStream.Position;

                // 写入 raw data（Float32 数组）
                foreach (var sample in ch.Samples)
                    rawWriter.Write(sample);

                objectInfos.Add(new TdmsObjectInfo
                {
                    Path = channelPath,
                    Properties = chProps,
                    HasRawData = true,
                    DataType = TdsTypeFloat32,
                    RawDataLength = (ulong)(ch.Samples.Length * sizeof(float)),
                    NumberOfValues = (ulong)ch.Samples.Length,
                });
            }
        }

        // --- 序列化 meta data ---
        // Number of objects
        metaWriter.Write((uint)objectInfos.Count);

        foreach (var obj in objectInfos)
        {
            // Object path (length-prefixed string)
            WriteString(metaWriter, obj.Path);

            if (obj.HasRawData)
            {
                // Raw data index
                // Format: [4B length of index info][4B data type][4B dimension=1][8B number of values]
                // length of index info = 4(dataType) + 4(dimension) + 8(numValues) = 16
                metaWriter.Write((uint)16);
                metaWriter.Write(obj.DataType);
                metaWriter.Write((uint)1); // dimension (always 1 for 1D arrays)
                metaWriter.Write(obj.NumberOfValues);
            }
            else
            {
                // No raw data: 0xFFFFFFFF
                metaWriter.Write(0xFFFFFFFF);
            }

            // Number of properties
            metaWriter.Write((uint)obj.Properties.Count);
            foreach (var (name, type, value) in obj.Properties)
            {
                WriteString(metaWriter, name);
                metaWriter.Write(type);
                if (type == TdsTypeString)
                {
                    // String value is length-prefixed
                    metaWriter.Write((uint)value.Length);
                }
                metaWriter.Write(value);
            }
        }

        metaWriter.Flush();
        rawWriter.Flush();

        var metaBytes = metaStream.ToArray();
        var rawBytes = rawStream.ToArray();

        // --- Write Lead-In (28 bytes) ---
        // [4B tag][4B toc_mask][4B version][8B next_segment_offset][8B raw_data_offset]
        bw.Write(TdmsTag);
        uint tocMask = kTocMetaData | kTocRawData;
        bw.Write(tocMask);
        bw.Write((uint)4713); // TDMS version number (2.0)
        ulong nextSegOffset = (ulong)(metaBytes.Length + rawBytes.Length);
        bw.Write((long)nextSegOffset); // next segment offset from end of lead-in
        bw.Write((long)metaBytes.Length); // raw data offset from end of lead-in

        // --- Write Metadata ---
        bw.Write(metaBytes);

        // --- Write Raw Data ---
        bw.Write(rawBytes);

        bw.Flush();
        await fs.FlushAsync();
    }

    /// <summary>
    /// 从 .tdms 文件读取为 SignalDataFile
    /// </summary>
    public static async Task<SignalDataFile> ReadAsync(string filePath)
    {
        var raw = await File.ReadAllBytesAsync(filePath);
        using var ms = new MemoryStream(raw);
        using var br = new BinaryReader(ms, Encoding.UTF8);

        // --- Read Lead-In ---
        var tag = br.ReadBytes(4);
        if (!tag.AsSpan().SequenceEqual(TdmsTag))
            throw new InvalidDataException("Not a valid TDMS file");

        uint tocMask = br.ReadUInt32();
        uint version = br.ReadUInt32();
        long nextSegOffset = br.ReadInt64();
        long rawDataOffset = br.ReadInt64();

        long metaStartPos = ms.Position;
        long rawStartPos = metaStartPos + rawDataOffset;

        // --- Read Metadata ---
        uint objectCount = br.ReadUInt32();

        var result = new SignalDataFile();
        var devices = new Dictionary<string, SignalDataFile.DeviceData>();
        long rawReadPos = rawStartPos;

        for (uint i = 0; i < objectCount; i++)
        {
            string path = ReadString(br);
            bool hasRawData = false;
            uint dataType = 0;
            ulong numberOfValues = 0;

            // Raw data index
            uint rawIndexMarker = br.ReadUInt32();
            if (rawIndexMarker != 0xFFFFFFFF)
            {
                // rawIndexMarker is the length of index info (expect 16: 4+4+8)
                if (rawIndexMarker >= 16)
                {
                    dataType = br.ReadUInt32();
                    uint dimension = br.ReadUInt32(); // always 1
                    numberOfValues = br.ReadUInt64();
                    hasRawData = true;
                    // Skip any extra index bytes beyond the 16 we read
                    int extra = (int)rawIndexMarker - 16;
                    if (extra > 0) br.ReadBytes(extra);
                }
                else
                {
                    br.ReadBytes((int)rawIndexMarker);
                }
            }

            // Properties
            uint propCount = br.ReadUInt32();
            var props = new Dictionary<string, string>();
            for (uint p = 0; p < propCount; p++)
            {
                string propName = ReadString(br);
                uint propType = br.ReadUInt32();
                string propValue;

                if (propType == TdsTypeString)
                {
                    uint strLen = br.ReadUInt32();
                    propValue = Encoding.UTF8.GetString(br.ReadBytes((int)strLen));
                }
                else if (propType == TdsTypeFloat64)
                {
                    propValue = br.ReadDouble().ToString();
                }
                else if (propType == TdsTypeFloat32)
                {
                    propValue = br.ReadSingle().ToString();
                }
                else
                {
                    // Int32 or other 4-byte types
                    propValue = br.ReadInt32().ToString();
                }

                props[propName] = propValue;
            }

            // Parse path: "/" = root, "/'Group'" = group, "/'Group'/'Channel'" = channel
            var parts = ParsePath(path);

            if (parts.Length == 0)
            {
                // Root
                if (props.TryGetValue("CreatedUtc", out var created))
                    result.CreatedUtc = DateTimeOffset.TryParse(created, out var dto) ? dto : DateTimeOffset.UtcNow;
                if (props.TryGetValue("TotalFrames", out var tf))
                    result.TotalFrames = int.TryParse(tf, out var v) ? v : 0;
            }
            else if (parts.Length == 1)
            {
                // Group (device)
                if (!devices.ContainsKey(parts[0]))
                    devices[parts[0]] = new SignalDataFile.DeviceData { DeviceId = parts[0] };
            }
            else if (parts.Length == 2)
            {
                // Channel
                string deviceId = parts[0];
                string channelId = parts[1];

                if (!devices.ContainsKey(deviceId))
                    devices[deviceId] = new SignalDataFile.DeviceData { DeviceId = deviceId };

                var chData = new SignalDataFile.ChannelData
                {
                    ChannelId = channelId,
                    SignalType = props.GetValueOrDefault("SignalType", ""),
                    SampleRate = double.TryParse(props.GetValueOrDefault("SampleRate", "1000"), out var sr) ? sr : 1000,
                    FrameCount = int.TryParse(props.GetValueOrDefault("FrameCount", "0"), out var fc) ? fc : 0,
                    Samples = Array.Empty<float>(),
                };

                // Read raw data if present
                if (hasRawData && dataType == TdsTypeFloat32 && numberOfValues > 0)
                {
                    long savedPos = ms.Position;
                    ms.Position = rawReadPos;
                    var samples = new float[numberOfValues];
                    for (ulong s = 0; s < numberOfValues; s++)
                        samples[s] = br.ReadSingle();
                    rawReadPos = ms.Position;
                    ms.Position = savedPos;
                    chData.Samples = samples;
                }

                devices[deviceId].Channels.Add(chData);
            }
        }

        result.Devices = devices.Values.ToList();
        return result;
    }

    // ---- Helper methods ----

    private static void WriteString(BinaryWriter bw, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        bw.Write((uint)bytes.Length);
        bw.Write(bytes);
    }

    private static string ReadString(BinaryReader br)
    {
        uint len = br.ReadUInt32();
        return Encoding.UTF8.GetString(br.ReadBytes((int)len));
    }

    private static (string name, uint type, byte[] value) StringProperty(string name, string value)
    {
        return (name, TdsTypeString, Encoding.UTF8.GetBytes(value));
    }

    private static (string name, uint type, byte[] value) Int32Property(string name, int value)
    {
        return (name, 0x07, BitConverter.GetBytes(value));  // tdsTypeI32
    }

    private static (string name, uint type, byte[] value) Float64Property(string name, double value)
    {
        return (name, TdsTypeFloat64, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 解析 TDMS 路径: "/" → [], "/'A'" → ["A"], "/'A'/'B'" → ["A","B"]
    /// </summary>
    private static string[] ParsePath(string path)
    {
        if (path == "/") return Array.Empty<string>();
        var parts = new List<string>();
        int i = 0;
        while (i < path.Length)
        {
            int start = path.IndexOf('\'', i);
            if (start < 0) break;
            int end = path.IndexOf('\'', start + 1);
            if (end < 0) break;
            parts.Add(path.Substring(start + 1, end - start - 1));
            i = end + 1;
        }
        return parts.ToArray();
    }

    private class TdmsObjectInfo
    {
        public string Path { get; set; } = "";
        public List<(string name, uint type, byte[] value)> Properties { get; set; } = new();
        public bool HasRawData { get; set; }
        public uint DataType { get; set; }
        public ulong RawDataLength { get; set; }
        public ulong NumberOfValues { get; set; }
    }
}
