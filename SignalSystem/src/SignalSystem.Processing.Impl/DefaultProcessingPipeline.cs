using System.Diagnostics;
using System.Runtime.CompilerServices;
using SignalSystem.Contracts.Configuration;
using SignalSystem.Contracts.Models;
using SignalSystem.Processing.Abstractions;
using SignalSystem.Processing.Impl.Compressors;
using SignalSystem.Processing.Impl.Preprocessors;

namespace SignalSystem.Processing.Impl;

/// <summary>
/// 默认处理管道实现：预处理 -> 压缩 -> 可选校验 -> 输出 ProcessResult
/// </summary>
public class DefaultProcessingPipeline : IProcessingPipeline
{
    private PipelineOptions _options = new();
    private IPreprocessor _preprocessor = new NullPreprocessor();
    private ICompressor _compressor = new NullCompressor();

    public void Configure(PipelineOptions options)
    {
        _options = options;
        _preprocessor = PreprocessorFactory.Create(options.Preprocess.Method);
        _compressor = CompressorFactory.Create(options.Compression.Algorithm);
    }

    public Task<ProcessResult> ProcessAsync(SignalFrame frame, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // 1. 预处理
        var preprocessed = _preprocessor.Encode(frame.Payload, _options.Preprocess);

        // 2. 压缩
        var compressed = _compressor.Compress(preprocessed, _options.Compression);

        // 3. 可选无损校验
        bool? verified = null;
        if (_options.Preprocess.EnableReversibleCheck)
        {
            var decompressed = _compressor.Decompress(compressed, _options.Compression);
            var restored = _preprocessor.Decode(decompressed, _options.Preprocess);
            verified = frame.Payload.AsSpan().SequenceEqual(restored);
        }

        sw.Stop();

        var result = new ProcessResult
        {
            FrameId = frame.FrameId,
            DeviceId = frame.DeviceId,
            ChannelId = frame.ChannelId,
            PreprocessMethod = _options.Preprocess.Method,
            CompressionAlgorithm = _options.Compression.Algorithm,
            OriginalSize = frame.Payload.Length,
            CompressedSize = compressed.Length,
            CompressedPayload = compressed,
            Elapsed = sw.Elapsed,
            LosslessVerified = verified,
        };

        return Task.FromResult(result);
    }

    public async IAsyncEnumerable<ProcessResult> ProcessStreamAsync(
        IAsyncEnumerable<SignalFrame> frames,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var frame in frames.WithCancellation(ct))
        {
            yield return await ProcessAsync(frame, ct);
        }
    }
}
