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
public static class BadModifierInjectionPatch
{
  [HarmonyPriority(Priority.Last)]
  [HarmonyPatch("get_BadModifiers")]
  [HarmonyPostfix]
  private static void AddCustomBadDebuffs(ref IReadOnlyList<ModifierModel> __result)
  {
    List<ModifierModel> updated = __result.ToList();
    int baseCount = updated.Count;

    TryAddModifier<InfinityEndlessModeDebuff>(updated);
    TryAddModifier<BossHpDoubleDebuff>(updated);
    TryAddModifier<ScalingPlatingDebuff>(updated);
    TryInsertAfter<DeadlyEvents, FearlessHeroBuff>(updated);
    TryInsertAfter<FearlessHeroBuff, NoPotionDebuff>(updated);
    TryInsertAfter<NoPotionDebuff, CloneRunDebuff>(updated);

    __result = updated;
    MainFile.Logger.Info($"[MoreCustoms] BadModifiers injected: base={baseCount}, final={updated.Count}.");
  }

  private static void TryInsertAfter<TAnchor, T>(List<ModifierModel> list)
    where TAnchor : ModifierModel
    where T : ModifierModel
  {
    if (!ModelDb.Contains(typeof(T)))
    {
      MainFile.Logger.Info($"[MoreCustoms] Bad modifier type not found in ModelDb: {typeof(T).FullName}");
      return;
    }

    ModifierModel customModifier;
    try
    {
      customModifier = ModelDb.Modifier<T>();
    }
    catch (Exception ex)
    {
      MainFile.Logger.Error($"[MoreCustoms] Failed to instantiate bad modifier {typeof(T).FullName}: {ex}");
      return;
    }

    if (list.Any(modifier => modifier.GetType() == customModifier.GetType()))
    {
      MainFile.Logger.Info($"[MoreCustoms] Bad modifier already present, skip: {customModifier.GetType().FullName}");
      return;
    }

    if (!HasRequiredLocalization(customModifier))
    {
      return;
    }

    int anchorIndex = list.FindIndex(modifier => modifier.GetType() == typeof(TAnchor));
    if (anchorIndex < 0)
    {
      list.Add(customModifier);
      MainFile.Logger.Info($"[MoreCustoms] Anchor {typeof(TAnchor).FullName} not found. Appended bad modifier: {customModifier.GetType().FullName}");
      return;
    }

    list.Insert(anchorIndex + 1, customModifier);
    MainFile.Logger.Info($"[MoreCustoms] Inserted bad modifier {customModifier.GetType().FullName} after {typeof(TAnchor).FullName}");
  }

  private static void TryAddModifier<T>(List<ModifierModel> list) where T : ModifierModel
  {
    if (!ModelDb.Contains(typeof(T)))
    {
      MainFile.Logger.Info($"[MoreCustoms] Bad modifier type not found in ModelDb: {typeof(T).FullName}");
      return;
    }

    ModifierModel customModifier;
    try
    {
      customModifier = ModelDb.Modifier<T>();
    }
    catch (Exception ex)
    {
      MainFile.Logger.Error($"[MoreCustoms] Failed to instantiate bad modifier {typeof(T).FullName}: {ex}");
      return;
    }

    if (list.Any(modifier => modifier.GetType() == customModifier.GetType()))
    {
      MainFile.Logger.Info($"[MoreCustoms] Bad modifier already present, skip: {customModifier.GetType().FullName}");
      return;
    }

    if (!HasRequiredLocalization(customModifier))
    {
      return;
    }

    list.Add(customModifier);
    MainFile.Logger.Info($"[MoreCustoms] Bad modifier added: {customModifier.GetType().FullName}");
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
