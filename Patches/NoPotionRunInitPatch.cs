using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.ApplyAscensionEffects))]
public static class NoPotionRunInitPatch
{
  [HarmonyPostfix]
  private static void ForceNoPotionSlotsAfterAscension(Player player)
  {
    if (!NoPotionDebuff.IsActiveForPlayer(player))
    {
      return;
    }

    if (!NoPotionDebuff.ShouldUseAscensionSafeStep(player.RunState))
    {
      return;
    }

    NoPotionDebuff.ForceSetPotionSlots(player, 0);
  }
}
