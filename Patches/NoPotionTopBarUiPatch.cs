using System;
using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(NPotionContainer), "GrowPotionHolders")]
public static class NoPotionTopBarUiPatch
{
  private static readonly System.Reflection.FieldInfo HoldersField = AccessTools.Field(typeof(NPotionContainer), "_holders");

  private static readonly System.Reflection.MethodInfo? UpdateNavigationMethod =
    AccessTools.Method(typeof(NPotionContainer), "UpdateNavigation");

  [HarmonyPrefix]
  private static bool SupportShrinkingPotionHolders(NPotionContainer __instance, int newMaxPotionSlots)
  {
    int targetSlots = Math.Max(0, newMaxPotionSlots);

    if (HoldersField.GetValue(__instance) is not IList holders)
    {
      return true;
    }

    if (targetSlots > holders.Count)
    {
      __instance.Visible = true;
      return true;
    }

    for (int index = holders.Count - 1; index >= targetSlots; index--)
    {
      object? holder = holders[index];
      holders.RemoveAt(index);

      if (holder is Node node)
      {
        node.GetParent()?.RemoveChild(node);
        node.QueueFree();
      }
    }

    __instance.Visible = targetSlots > 0;
    UpdateNavigationMethod?.Invoke(__instance, null);
    return false;
  }
}
