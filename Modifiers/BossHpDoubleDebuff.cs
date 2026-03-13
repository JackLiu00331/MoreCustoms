using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using ModTemplate.Config;

namespace ModTemplate.Modifiers;

public class BossHpDoubleDebuff : ModifierModel
{
  public static bool IsActiveFor(Creature creature)
  {
    if (creature.Side != CombatSide.Enemy)
    {
      return false;
    }

    if (creature.CombatState?.Encounter?.RoomType != RoomType.Boss)
    {
      return false;
    }

    return creature.CombatState.Modifiers.Any(modifier => modifier.GetType() == typeof(BossHpDoubleDebuff));
  }

  public static decimal GetConfiguredMultiplier()
  {
    decimal multiplier = MoreCustomsConfig.Current.BossHpMultiplier;
    if (multiplier <= 0m)
    {
      return 1m;
    }

    return multiplier;
  }

  public static async Task ApplyIfNeeded(Creature creature)
  {
    if (!IsActiveFor(creature))
    {
      return;
    }

    decimal multiplier = GetConfiguredMultiplier();
    int baseMaxHp = creature.MonsterMaxHpBeforeModification ?? creature.Monster?.MaxInitialHp ?? creature.MaxHp;
    int targetMaxHp = System.Math.Max(1, (int)(baseMaxHp * multiplier));

    if (creature.MaxHp == targetMaxHp)
    {
      return;
    }

    if (creature.MaxHp != baseMaxHp)
    {
      return;
    }

    await CreatureCmd.SetMaxAndCurrentHp(creature, targetMaxHp);
  }

  public override async Task AfterRoomEntered(AbstractRoom room)
  {
    if (room is not CombatRoom combatRoom)
    {
      return;
    }

    if (combatRoom.CombatState?.Encounter?.RoomType != RoomType.Boss)
    {
      return;
    }

    foreach (Creature creature in combatRoom.CombatState.Enemies)
    {
      await ApplyIfNeeded(creature);
    }
  }

  public override async Task AfterCreatureAddedToCombat(Creature creature)
  {
    await ApplyIfNeeded(creature);
  }
}
