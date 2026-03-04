namespace SignalSystem.Contracts.Enums;

/// <summary>
/// 背压策略（队列满时的行为）
/// </summary>
public enum BackpressureStrategy
{
    /// <summary>阻塞上游</summary>
    Block,

    /// <summary>丢弃最旧帧</summary>
    DropOldest,

    /// <summary>丢弃最新帧</summary>
    DropNewest
}
