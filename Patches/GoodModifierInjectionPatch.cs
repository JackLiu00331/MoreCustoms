using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(ModelDb))]
public static class GoodModifierInjectionPatch
{
  [HarmonyPatch("get_GoodModifiers")]
  [HarmonyPostfix]
  private static void AddCustomGoodBuffs(ref IReadOnlyList<ModifierModel> __result)
  {
    List<ModifierModel> updated = __result.ToList();

    TryAddModifier<GoldGainBuff>(updated);
    TryAddModifier<MultiSmithBuff>(updated);

    __result = updated;
  }

  private static void TryAddModifier<T>(List<ModifierModel> list) where T : ModifierModel
  {
    if (!ModelDb.Contains(typeof(T)))
    {
      return;
    }

    ModifierModel customModifier;
    try
    {
      customModifier = ModelDb.Modifier<T>();
    }
    catch (Exception)
    {
      return;
    }

    if (list.Any(modifier => modifier.GetType() == customModifier.GetType()))
    {
      return;
    }

    list.Add(customModifier);
  }
}
