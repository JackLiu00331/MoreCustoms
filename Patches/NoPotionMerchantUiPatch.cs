using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(NMerchantInventory))]
public static class NoPotionMerchantUiPatch
{
  [HarmonyPatch("Initialize")]
  [HarmonyPostfix]
  private static void HidePotionContainerOnInitialize(NMerchantInventory __instance, MerchantInventory inventory)
  {
    ApplyPotionContainerVisibility(__instance, inventory, "Initialize");
  }

  [HarmonyPatch(nameof(NMerchantInventory.Open))]
  [HarmonyPostfix]
  private static void HidePotionContainerOnOpen(NMerchantInventory __instance)
  {
    MerchantInventory? inventory = __instance.Inventory;
    if (inventory == null)
    {
      return;
    }

    ApplyPotionContainerVisibility(__instance, inventory, "Open");
  }

  private static void ApplyPotionContainerVisibility(NMerchantInventory inventoryUi, MerchantInventory inventory, string stage)
  {
    bool isActive = NoPotionDebuff.IsActiveForPlayer(inventory.Player);
    if (!isActive)
    {
      return;
    }

    Control? potionContainer = inventoryUi.GetNodeOrNull<Control>("%Potions");
    if (potionContainer == null)
    {
      Log.Warn($"[NoPotionMerchantUiPatch.{stage}] Potion container not found.");
      return;
    }

    potionContainer.Visible = false;
    potionContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
    Log.Info($"[NoPotionMerchantUiPatch.{stage}] Potion container hidden.");
  }
}
