using SignalSystem.Contracts.Enums;
using SignalSystem.Contracts.Configuration;
using SignalSystem.Processing.Abstractions;

namespace SignalSystem.Processing.Impl.Preprocessors;

/// <summary>
/// 预处理器工厂
/// </summary>
public static class PreprocessorFactory
{
    public static IPreprocessor Create(PreprocessMethod method)
    {
        return method switch
        {
            PreprocessMethod.None => new NullPreprocessor(),
            PreprocessMethod.FirstOrderDiff => new FirstOrderDiffPreprocessor(),
            PreprocessMethod.SecondOrderDiff => new SecondOrderDiffPreprocessor(),
            PreprocessMethod.LPC => new LpcPreprocessor(),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
    }
}

/// <summary>
/// 空预处理器（不做任何处理）
/// </summary>
public class NullPreprocessor : IPreprocessor
{
    public string Name => "None";
    public byte[] Encode(byte[] input, PreprocessOptions options) => (byte[])input.Clone();
    public byte[] Decode(byte[] input, PreprocessOptions options) => (byte[])input.Clone();
}
