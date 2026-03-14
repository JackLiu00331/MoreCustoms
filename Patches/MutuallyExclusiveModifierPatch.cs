using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(ModelDb))]
public static class MutuallyExclusiveModifierPatch
{
  [HarmonyPatch("get_MutuallyExclusiveModifiers")]
  [HarmonyPostfix]
  private static void AddCustomMutualExclusion(ref IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
  {
    if (!ModelDb.Contains(typeof(FearlessHeroBuff)))
    {
      return;
    }

    List<IReadOnlySet<ModifierModel>> updated = __result.ToList();

    ModifierModel fearless;
    try
    {
      fearless = ModelDb.Modifier<FearlessHeroBuff>();
    }
    catch (Exception)
    {
      return;
    }

    if (ModelDb.Contains(typeof(DeadlyEvents)))
    {
      TryAddMutualExclusionGroup<DeadlyEvents>(updated, fearless);
    }

    TryAddMutualExclusionGroup<BigGameHunter>(updated, fearless);

    __result = updated;
  }

  private static void TryAddMutualExclusionGroup<T>(List<IReadOnlySet<ModifierModel>> groups, ModifierModel fearless)
    where T : ModifierModel
  {
    if (!ModelDb.Contains(typeof(T)))
    {
      return;
    }

    ModifierModel other;
    try
    {
      other = ModelDb.Modifier<T>();
    }
    catch (Exception)
    {
      return;
    }

    bool alreadyExists = groups.Any(group =>
      group.Any(modifier => modifier.GetType() == typeof(FearlessHeroBuff)) &&
      group.Any(modifier => modifier.GetType() == typeof(T)));

    if (alreadyExists)
    {
      return;
    }

    groups.Add(new HashSet<ModifierModel>
    {
      fearless,
      other
    });
  }
}
