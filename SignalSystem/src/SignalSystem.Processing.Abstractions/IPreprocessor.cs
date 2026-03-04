using SignalSystem.Contracts.Configuration;

namespace SignalSystem.Processing.Abstractions;

/// <summary>
/// 预处理器接口（无损可逆）
/// </summary>
public interface IPreprocessor
{
    /// <summary>预处理名称</summary>
    string Name { get; }

    /// <summary>正向编码（原始 -> 预处理后）</summary>
    byte[] Encode(byte[] input, PreprocessOptions options);

    /// <summary>逆向解码（预处理后 -> 原始）</summary>
    byte[] Decode(byte[] input, PreprocessOptions options);
}
