using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(EncounterModel), "get_ShouldGiveRewards")]
public static class EndlessBossRewardsPatch
{
  private static readonly PropertyInfo StateProperty = AccessTools.Property(typeof(RunManager), "State");

  [HarmonyPostfix]
  private static void EnsureBossRewardsInEndless(EncounterModel __instance, ref bool __result)
  {
    if (__result)
    {
      return;
    }

    RunState? state = StateProperty.GetValue(RunManager.Instance) as RunState;
    if (!InfinityEndlessModeDebuff.IsActive(state))
    {
      return;
    }

    if (state == null || state.CurrentActIndex < 2)
    {
      return;
    }

    if (__instance.RoomType != MegaCrit.Sts2.Core.Rooms.RoomType.Boss)
    {
      return;
    }

    __result = true;
    MainFile.Logger.Info($"[Endless] Forced rewards enabled for boss encounter {__instance.Id} at actIndex={state.CurrentActIndex}.");
  }
}
