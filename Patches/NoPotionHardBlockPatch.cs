using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Models;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch]
public static class NoPotionHardBlockPatch
{
  [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainMaxPotionCount))]
  [HarmonyPrefix]
  private static bool BlockPotionSlotGain(int amount, Player player)
  {
    if (amount <= 0 || !NoPotionDebuff.IsActiveForPlayer(player))
    {
      return true;
    }

    NoPotionDebuff.ForceSetPotionSlots(player, 0);
    return false;
  }

  [HarmonyPatch(typeof(PotionCmd), nameof(PotionCmd.TryToProcure), new[] { typeof(PotionModel), typeof(Player), typeof(int) })]
  [HarmonyPrefix]
  private static bool ConvertDirectPotionProcureToGold(PotionModel potion, Player player, ref Task<PotionProcureResult> __result)
  {
    if (!NoPotionDebuff.IsActiveForPlayer(player))
    {
      return true;
    }

    int goldAmount = NoPotionDebuff.GetPotionReplacementGold(potion);
    if (goldAmount > 0)
    {
      player.Gold += goldAmount;
    }

    __result = Task.FromResult(new PotionProcureResult
    {
      potion = potion,
      success = false,
      failureReason = PotionProcureFailureReason.NotAllowed
    });
    return false;
  }
}
