using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using ModTemplate.Config;

namespace ModTemplate.Modifiers;

public class ScalingPlatingDebuff : ModifierModel
{
  public override async Task AfterRoomEntered(AbstractRoom room)
  {
    if (room is not CombatRoom combatRoom)
    {
      return;
    }

    int basePerAct = Math.Max(0, MoreCustomsConfig.Current.PlatingBasePerAct);
    int actMultiplier = Math.Max(1, base.RunState.CurrentActIndex + 1);
    decimal platingAmount = basePerAct * actMultiplier;

    if (platingAmount <= 0m)
    {
      return;
    }

    foreach (Creature creature in combatRoom.CombatState.Enemies)
    {
      await PowerCmd.Apply<PlatingPower>(creature, platingAmount, null, null);
    }
  }
}
