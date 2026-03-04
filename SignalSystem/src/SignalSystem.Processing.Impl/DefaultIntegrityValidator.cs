using SignalSystem.Processing.Abstractions;

namespace SignalSystem.Processing.Impl;

/// <summary>
/// 默认无损完整性校验器
/// </summary>
public class DefaultIntegrityValidator : IIntegrityValidator
{
    public bool Validate(byte[] original, byte[] restored)
    {
        return original.AsSpan().SequenceEqual(restored);
    }
}
