using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(ModelDb))]
public static class BadModifierInjectionPatch
{
  [HarmonyPatch("get_BadModifiers")]
  [HarmonyPostfix]
  private static void AddCustomBadDebuffs(ref IReadOnlyList<ModifierModel> __result)
  {
    List<ModifierModel> updated = __result.ToList();

    TryAddModifier<InfinityEndlessModeDebuff>(updated);
    TryAddModifier<BossHpDoubleDebuff>(updated);
    TryAddModifier<ScalingPlatingDebuff>(updated);
    TryInsertAfter<DeadlyEvents, FearlessHeroBuff>(updated);
    TryInsertAfter<FearlessHeroBuff, NoPotionDebuff>(updated);
    TryInsertAfter<NoPotionDebuff, CloneRunDebuff>(updated);

    __result = updated;
  }

  private static void TryInsertAfter<TAnchor, T>(List<ModifierModel> list)
    where TAnchor : ModifierModel
    where T : ModifierModel
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

    int anchorIndex = list.FindIndex(modifier => modifier.GetType() == typeof(TAnchor));
    if (anchorIndex < 0)
    {
      list.Add(customModifier);
      return;
    }

    list.Insert(anchorIndex + 1, customModifier);
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
