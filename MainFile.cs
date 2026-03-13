using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using ModTemplate.Config;

namespace ModTemplate;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string ModId = "MoreCustoms"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        MoreCustomsConfig.LoadOrCreate(Logger);

        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }


}
