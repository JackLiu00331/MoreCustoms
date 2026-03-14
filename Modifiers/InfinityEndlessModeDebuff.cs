using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using ModTemplate.Config;
using MegaCrit.Sts2.Core.Runs;

namespace ModTemplate.Modifiers;

public class InfinityEndlessModeDebuff : ModifierModel
{
  private static int GetEndlessDepth(IRunState runState)
  {
    return Math.Max(0, runState.CurrentActIndex - 2);
  }

  private static decimal GetHpMultiplierForEndlessDepth(int endlessDepth, bool isBossRoom, bool isDoubleBossAct)
  {
    decimal hpPercentPerAct = Math.Max(0m, MoreCustomsConfig.Current.EndlessEnemyHpPerActPercent);
    decimal bossExtraHpPercentPerAct = isBossRoom ? Math.Max(0m, MoreCustomsConfig.Current.EndlessBossExtraHpPerActPercent) : 0m;
    decimal doubleBossExtraHpPercent = isDoubleBossAct ? Math.Max(0m, MoreCustomsConfig.Current.EndlessDoubleBossExtraHpPercent) : 0m;
    return 1m + endlessDepth * ((hpPercentPerAct + bossExtraHpPercentPerAct) / 100m) + (doubleBossExtraHpPercent / 100m);
  }

  private static int GetStrengthBonusForEndlessDepth(int endlessDepth)
  {
    int everyActs = Math.Max(1, MoreCustomsConfig.Current.EndlessEnemyStrengthEveryActs);
    int perStep = Math.Max(0, MoreCustomsConfig.Current.EndlessEnemyStrengthPerStep);
    return (endlessDepth / everyActs) * perStep;
  }

  private static async Task ApplyScalingIfNeeded(Creature creature, RunState runState, bool isBossRoom, bool isDoubleBossAct)
  {
    if (creature.Side != CombatSide.Enemy)
    {
      return;
    }

    int endlessDepth = GetEndlessDepth(runState);
    if (endlessDepth <= 0)
    {
      return;
    }

    int baseMaxHp = creature.MonsterMaxHpBeforeModification ?? creature.Monster?.MaxInitialHp ?? creature.MaxHp;
    int targetMaxHp = Math.Max(1, (int)Math.Round(baseMaxHp * GetHpMultiplierForEndlessDepth(endlessDepth, isBossRoom, isDoubleBossAct)));
    if (creature.MaxHp == baseMaxHp && creature.MaxHp != targetMaxHp)
    {
      await CreatureCmd.SetMaxAndCurrentHp(creature, targetMaxHp);
    }

    int targetStrength = GetStrengthBonusForEndlessDepth(endlessDepth);
    if (targetStrength <= 0)
    {
      return;
    }

    int currentStrength = creature.GetPowerAmount<StrengthPower>();
    if (currentStrength < targetStrength)
    {
      await PowerCmd.Apply<StrengthPower>(creature, targetStrength - currentStrength, null, null);
    }
  }

  public static bool IsActive(IRunState? runState)
  {
    if (runState == null)
    {
      return false;
    }

    return runState.Modifiers.Any(modifier => modifier.GetType() == typeof(InfinityEndlessModeDebuff));
  }

  protected override void AfterRunCreated(RunState runState)
  {
    if (!IsActive(runState))
    {
      return;
    }

    MainFile.Logger.Info("[Endless] InfinityEndlessModeDebuff activated for this run.");
  }

  public override async Task AfterRoomEntered(AbstractRoom room)
  {
    if (room is not CombatRoom combatRoom)
    {
      return;
    }

    if (!IsActive(base.RunState))
    {
      return;
    }

    bool isBossRoom = combatRoom.RoomType == RoomType.Boss;
    bool isDoubleBossAct = isBossRoom && combatRoom.Act.HasSecondBoss;

    foreach (Creature creature in combatRoom.CombatState.Enemies)
    {
      await ApplyScalingIfNeeded(creature, base.RunState, isBossRoom, isDoubleBossAct);
    }
  }

  public override async Task AfterCreatureAddedToCombat(Creature creature)
  {
    if (!IsActive(base.RunState))
    {
      return;
    }

    CombatRoom? currentCombatRoom = base.RunState.CurrentRoom as CombatRoom;
    bool isBossRoom = currentCombatRoom?.RoomType == RoomType.Boss;
    bool isDoubleBossAct = isBossRoom && currentCombatRoom?.Act.HasSecondBoss == true;

    await ApplyScalingIfNeeded(creature, base.RunState, isBossRoom, isDoubleBossAct);
  }
}