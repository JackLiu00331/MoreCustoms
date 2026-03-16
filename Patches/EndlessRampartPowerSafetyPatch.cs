using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ModTemplate.Patches;

/// <summary>
/// 修复无尽深层战斗中 RampartPower 在回合开始时枚举敌人集合导致的
/// "Collection was modified" 异常。
///
/// 原版实现直接枚举 CombatState.Enemies.Where(...)
/// 并在循环中 await GainBlock；期间敌人列表可能变化（召唤/死亡/状态更新），
/// 触发 ListWhereIterator 失效并中断战斗流程（表现为黑屏）。
///
/// 这里改为先对目标敌人做快照，再逐个结算，避免枚举期集合变更。
/// </summary>
[HarmonyPatch(typeof(RampartPower), nameof(RampartPower.AfterSideTurnStart))]
public static class EndlessRampartPowerSafetyPatch
{
  [HarmonyPrefix]
  private static bool UseSnapshotEnumeration(
    RampartPower __instance,
    CombatSide side,
    CombatState combatState,
    ref Task __result)
  {
    __result = ExecuteSafely(__instance, side);
    return false;
  }

  private static async Task ExecuteSafely(RampartPower power, CombatSide side)
  {
    if (side != CombatSide.Player)
    {
      return;
    }

    Creature[] targets = power.CombatState.Enemies
      .Where(creature => creature.Monster is TurretOperator)
      .ToArray();

    foreach (Creature creature in targets)
    {
      if (creature.CombatState == null || !creature.IsAlive)
      {
        continue;
      }

      await CreatureCmd.GainBlock(creature, power.Amount, ValueProp.Unpowered, null);
    }
  }
}
