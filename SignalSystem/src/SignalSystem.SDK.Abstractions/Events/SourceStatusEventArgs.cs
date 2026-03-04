using SignalSystem.Contracts.Enums;

namespace SignalSystem.SDK.Abstractions.Events;

/// <summary>
/// 设备/通道状态变更事件参数
/// </summary>
public class SourceStatusEventArgs : EventArgs
{
    public string DeviceId { get; }
    public string? ChannelId { get; }
    public SourceStatus Status { get; }
    public string? Message { get; }

    public SourceStatusEventArgs(string deviceId, string? channelId, SourceStatus status, string? message = null)
    {
        DeviceId = deviceId;
        ChannelId = channelId;
        Status = status;
        Message = message;
    }
}
