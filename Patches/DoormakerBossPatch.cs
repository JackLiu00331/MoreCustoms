using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(DoorRevivalPower), nameof(DoorRevivalPower.DoRevive))]
public static class DoormakerBossPatch
{
  [HarmonyPostfix]
  private static void ReapplyBossHpScalingAfterDoorRevive(DoorRevivalPower __instance, ref Task __result)
  {
    __result = ReapplyBossHpScalingAfterDoorReviveAsync(__instance, __result);
  }

  private static async Task ReapplyBossHpScalingAfterDoorReviveAsync(DoorRevivalPower power, Task originalTask)
  {
    await originalTask;
    await BossHpDoubleDebuff.ApplyIfNeeded(power.Owner);
  }
}
