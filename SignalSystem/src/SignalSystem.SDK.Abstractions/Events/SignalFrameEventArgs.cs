using SignalSystem.Contracts.Models;

namespace SignalSystem.SDK.Abstractions.Events;

/// <summary>
/// 信号帧批量到达事件参数
/// </summary>
public class SignalFrameEventArgs : EventArgs
{
    public IReadOnlyList<SignalFrame> Frames { get; }

    public SignalFrameEventArgs(IReadOnlyList<SignalFrame> frames)
    {
        Frames = frames;
    }
}
