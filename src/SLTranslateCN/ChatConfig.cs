namespace SLTranslateCN;

public sealed class ChatConfig
{
    public bool IsEnabled { get; set; } = true;

    public ushort PublicBroadcastDuration { get; set; } = 10;

    public ushort FactionBroadcastDuration { get; set; } = 10;

    public ushort AdminReportBroadcastDuration { get; set; } = 10;

    public bool SendConsoleCopy { get; set; } = true;
}

