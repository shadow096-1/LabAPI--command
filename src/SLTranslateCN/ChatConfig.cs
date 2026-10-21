namespace SLTranslateCN;

public sealed class ChatConfig
{
    public bool IsEnabled { get; set; } = true;

    public ushort PublicBroadcastDuration { get; set; } = 10;

    public ushort FactionBroadcastDuration { get; set; } = 10;

    public ushort AdminReportBroadcastDuration { get; set; } = 10;

    public int HintVerticalOffset { get; set; } = 500;

    public int HintFontSize { get; set; } = 18;

    public bool SendConsoleCopy { get; set; } = true;

    public bool QqReportEnabled { get; set; } = false;

    public string QqBotApiUrl { get; set; } = string.Empty;

    public string QqBotAccessToken { get; set; } = string.Empty;

    public long QqReportGroupId { get; set; }

    public long QqReportAtQq { get; set; }
}

