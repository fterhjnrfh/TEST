namespace SignalSystem.Processing.Abstractions;

/// <summary>
/// 无损完整性校验器接口
/// </summary>
public interface IIntegrityValidator
{
    /// <summary>校验：原始字节与解压字节完全一致</summary>
    bool Validate(byte[] original, byte[] restored);
}
