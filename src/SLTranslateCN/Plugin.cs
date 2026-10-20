using System;
using LabApi.Features;
using LabPlugin = LabApi.Loader.Features.Plugins.Plugin<SLTranslateCN.ChatConfig>;

namespace SLTranslateCN;

public sealed class Plugin : LabPlugin
{
    public static Plugin? Instance { get; private set; }

    public override string Name => "SLTranslateCN";

    public override string Description => "Adds Chinese client-console chat commands for public, faction, and admin-report messages.";

    public override string Author => "OpenAI";

    public override Version Version { get; } = new(1, 0, 3);

    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    public override string ConfigFileName { get; set; } = "sl_translate_cn.yml";

    public override void Enable()
    {
        Instance = this;
    }

    public override void Disable()
    {
        Instance = null;
    }
}

