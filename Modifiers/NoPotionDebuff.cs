using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ModTemplate.Modifiers;

public class NoPotionDebuff : ModifierModel
{
  public static bool IsActive(IRunState runState)
  {
    return runState.Modifiers.Any(modifier => modifier.GetType() == typeof(NoPotionDebuff));
  }

  public static bool IsActiveForPlayer(Player? player)
  {
    return player?.RunState != null && IsActive(player.RunState);
  }

  public static void ForceSetPotionSlots(Player player, int targetSlots)
  {
    int clampedTarget = Math.Max(0, targetSlots);
    if (player.MaxPotionCount > clampedTarget)
    {
      player.SubtractFromMaxPotionCount(player.MaxPotionCount - clampedTarget);
    }
    else if (player.MaxPotionCount < clampedTarget)
    {
      player.AddToMaxPotionCount(clampedTarget - player.MaxPotionCount);
    }
  }

  public static bool ShouldUseAscensionSafeStep(IRunState runState)
  {
    return runState.AscensionLevel >= (int)AscensionLevel.TightBelt;
  }

  public override bool ShouldProcurePotion(PotionModel potion, Player player)
  {
    return false;
  }

  public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
  {
    for (int index = 0; index < rewards.Count; index++)
    {
      if (rewards[index] is not PotionReward potionReward)
      {
        continue;
      }

      int goldAmount = GetPotionReplacementGold(potionReward.Potion);
      rewards[index] = new GoldReward(goldAmount, player);
    }

    return true;
  }

  protected override void AfterRunCreated(RunState runState)
  {
    int initialTargetSlots = ShouldUseAscensionSafeStep(runState) ? 1 : 0;

    foreach (Player player in runState.Players)
    {
      ForceSetPotionSlots(player, initialTargetSlots);
    }
  }

  protected override void AfterRunLoaded(RunState runState)
  {
    foreach (Player player in runState.Players)
    {
      ForceSetPotionSlots(player, 0);
    }
  }

  public static int GetPotionReplacementGold(PotionModel? potion)
  {
    if (potion is FoulPotion)
    {
      return 100;
    }

    PotionRarity rarity = potion?.Rarity ?? PotionRarity.Common;
    return rarity switch
    {
      PotionRarity.Common => 10,
      PotionRarity.Uncommon => 20,
      PotionRarity.Rare => 30,
      PotionRarity.Event => 40,
      PotionRarity.Token => 10,
      _ => 10
    };
  }
}
