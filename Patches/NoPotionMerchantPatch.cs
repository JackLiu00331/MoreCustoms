using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Logging;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(MerchantInventory), "PopulatePotionEntries")]
public static class NoPotionMerchantPatch
{
  [HarmonyPrefix]
  private static bool KeepMerchantInventoryStable(MerchantInventory __instance)
  {
    bool isActive = NoPotionDebuff.IsActiveForPlayer(__instance.Player);
    Log.Info($"[NoPotionMerchantPatch] Active={isActive}, keeping base merchant inventory population for UI stability.");
    return true;
  }
}
