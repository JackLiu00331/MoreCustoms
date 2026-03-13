using System;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using ModTemplate.Config;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainGold))]
public static class GoldGainPatch
{
  [HarmonyPrefix]
  private static void ApplyGoldGainBuff(ref decimal amount, Player player)
  {
    if (player?.RunState?.Modifiers == null)
    {
      return;
    }

    bool hasBuff = player.RunState.Modifiers.Any(modifier => modifier.GetType() == typeof(GoldGainBuff));
    if (!hasBuff)
    {
      return;
    }

    decimal multiplier = MoreCustomsConfig.Current.GoldGainMultiplier;
    if (multiplier <= 0m)
    {
      multiplier = 1m;
    }

    amount = Math.Max(0m, amount * multiplier);
  }
}
