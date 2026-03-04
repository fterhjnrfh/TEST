namespace SignalSystem.Contracts.Enums;

/// <summary>
/// 设备/通道运行状态
/// </summary>
public enum SourceStatus
{
    Idle,
    Initializing,
    Running,
    Paused,
    Stopped,
    Error
}
