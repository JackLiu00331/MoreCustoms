using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(NCombatUi), "OnCombatWon")]
public static class EndlessGloryBossRewardUiPatch
{
  private static readonly MethodInfo? ShowRewardsMethod = AccessTools.Method(typeof(NCombatUi), "ShowRewards");

  [HarmonyPrefix]
  private static bool ForceRewardsUiForEndlessBoss(NCombatUi __instance, CombatRoom room)
  {
    if (room == null || room.RoomType != RoomType.Boss)
    {
      return true;
    }

    if (!InfinityEndlessModeDebuff.IsActive(room.CombatState?.RunState))
    {
      return true;
    }

    int currentActIndex = room.CombatState?.RunState?.CurrentActIndex ?? 0;
    if (currentActIndex < 2)
    {
      return true;
    }

    if (ShowRewardsMethod == null)
    {
      MainFile.Logger.Warn("[Endless] NCombatUi.ShowRewards method not found, fallback to base reward flow.");
      return true;
    }

    Task? showRewardsTask = ShowRewardsMethod.Invoke(__instance, new object[] { room }) as Task;
    if (showRewardsTask != null)
    {
      MainFile.Logger.Info($"[Endless] Force-opening boss rewards UI for encounter {room.Encounter.Id} at actIndex={currentActIndex}.");
      TaskHelper.RunSafely(showRewardsTask);
      return false;
    }

    MainFile.Logger.Warn("[Endless] Failed to invoke NCombatUi.ShowRewards, fallback to base reward flow.");
    return true;
  }
}
