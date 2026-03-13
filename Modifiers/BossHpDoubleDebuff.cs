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

    decimal multiplier = MoreCustomsConfig.Current.BossHpMultiplier;
    if (multiplier <= 0m)
    {
      return;
    }

    foreach (Creature creature in combatRoom.CombatState.Enemies)
    {
      await CreatureCmd.SetMaxAndCurrentHp(creature, creature.MaxHp * multiplier);
    }
  }

  public override async Task AfterCreatureAddedToCombat(Creature creature)
  {
    if (creature.Side != CombatSide.Enemy)
    {
      return;
    }

    if (creature.CombatState?.Encounter?.RoomType != RoomType.Boss)
    {
      return;
    }

    decimal multiplier = MoreCustomsConfig.Current.BossHpMultiplier;
    if (multiplier <= 0m)
    {
      return;
    }

    await CreatureCmd.SetMaxAndCurrentHp(creature, creature.MaxHp * multiplier);
  }
}
