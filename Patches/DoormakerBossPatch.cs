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

    if (power.Owner == null)
    {
      return;
    }

    // Endless 中避免门扉在反复开合时重复触发额外血量补偿。
    if (InfinityEndlessModeDebuff.IsActive(power.Owner.CombatState?.RunState))
    {
      return;
    }

    await BossHpDoubleDebuff.ApplyIfNeeded(power.Owner);
  }
}
