using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves.Managers;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
public static class EndlessProgressEpochGuardPatch
{
  [HarmonyPrefix]
  private static bool SkipUnsupportedActsForEndless(Player localPlayer, int act)
  {
    if (act < 3)
    {
      return true;
    }

    if (!InfinityEndlessModeDebuff.IsActive(localPlayer?.RunState))
    {
      return true;
    }

    MainFile.Logger.Info($"[Endless] Skipping ObtainCharUnlockEpoch for unsupported act index {act} in endless mode.");
    return false;
  }
}