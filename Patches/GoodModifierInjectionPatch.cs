using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(ModelDb))]
public static class GoodModifierInjectionPatch
{
  [HarmonyPriority(Priority.Last)]
  [HarmonyPatch("get_GoodModifiers")]
  [HarmonyPostfix]
  private static void AddCustomGoodBuffs(ref IReadOnlyList<ModifierModel> __result)
  {
    List<ModifierModel> updated = __result.ToList();
    int baseCount = updated.Count;

    TryAddModifier<GoldGainBuff>(updated);
    TryAddModifier<MultiSmithBuff>(updated);

    __result = updated;
    MainFile.Logger.Info($"[MoreCustoms] GoodModifiers injected: base={baseCount}, final={updated.Count}.");
  }

  private static void TryAddModifier<T>(List<ModifierModel> list) where T : ModifierModel
  {
    if (!ModelDb.Contains(typeof(T)))
    {
      MainFile.Logger.Info($"[MoreCustoms] Good modifier type not found in ModelDb: {typeof(T).FullName}");
      return;
    }

    ModifierModel customModifier;
    try
    {
      customModifier = ModelDb.Modifier<T>();
    }
    catch (Exception ex)
    {
      MainFile.Logger.Error($"[MoreCustoms] Failed to instantiate good modifier {typeof(T).FullName}: {ex}");
      return;
    }

    if (list.Any(modifier => modifier.GetType() == customModifier.GetType()))
    {
      MainFile.Logger.Info($"[MoreCustoms] Good modifier already present, skip: {customModifier.GetType().FullName}");
      return;
    }

    if (!HasRequiredLocalization(customModifier))
    {
      return;
    }

    list.Add(customModifier);
    MainFile.Logger.Info($"[MoreCustoms] Good modifier added: {customModifier.GetType().FullName}");
  }

  private static bool HasRequiredLocalization(ModifierModel modifier)
  {
    string entry = modifier.Id.Entry;
    string titleKey = entry + ".title";
    string descriptionKey = entry + ".description";
    bool hasTitle = LocString.GetIfExists("modifiers", titleKey) != null;
    bool hasDescription = LocString.GetIfExists("modifiers", descriptionKey) != null;

    if (!hasTitle || !hasDescription)
    {
      MainFile.Logger.Error($"[MoreCustoms] Missing localization for modifier {modifier.GetType().FullName}. Required keys: modifiers.{titleKey}, modifiers.{descriptionKey}. Skipping injection.");
      return false;
    }

    return true;
  }
}
