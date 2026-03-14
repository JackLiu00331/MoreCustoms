using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using HarmonyLib;

namespace ModTemplate.Modifiers;

public class CloneRunDebuff : ModifierModel
{
  private const int Act2Index = 1;
  private const decimal CloneHpLossRatio = 0.30m;
  private static readonly System.Reflection.FieldInfo RoomsField = AccessTools.Field(typeof(ActModel), "_rooms");

  public static bool IsActive(IRunState runState)
  {
    return runState.Modifiers.Any(modifier => modifier.GetType() == typeof(CloneRunDebuff));
  }

  public static bool IsActiveForPlayer(Player? player)
  {
    return player?.RunState != null && IsActive(player.RunState);
  }

  public static bool HasPaelsGrowth(Player player)
  {
    return player.GetRelic<PaelsGrowth>() != null;
  }

  public static int CalculateCloneHpLoss(int currentHp)
  {
    if (currentHp <= 0)
    {
      return 0;
    }

    int loss = (int)Math.Floor(currentHp * CloneHpLossRatio);
    if (currentHp >= 2)
    {
      return Math.Max(1, loss);
    }

    return loss;
  }

  public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
  {
    if (!HasPaelsGrowth(player) || player.Creature.CurrentHp > 1)
    {
      return false;
    }

    RestSiteOption? cloneOption = options.FirstOrDefault(option => option is CloneRestSiteOption);
    if (cloneOption == null)
    {
      return false;
    }

    options.Remove(cloneOption);
    return true;
  }

  public override bool ShouldAllowAncient(Player player, AncientEventModel ancient)
  {
    if (player.RunState.CurrentActIndex == Act2Index)
    {
      return ancient is Pael;
    }

    return true;
  }

  public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
  {
    if (actIndex != Act2Index)
    {
      return map;
    }

    if (runState.Acts.Count <= actIndex)
    {
      return map;
    }

    ActModel act = runState.Acts[actIndex];
    RoomSet? rooms = RoomsField.GetValue(act) as RoomSet;
    if (rooms != null)
    {
      rooms.Ancient = ModelDb.AncientEvent<Pael>();
    }

    return map;
  }
}