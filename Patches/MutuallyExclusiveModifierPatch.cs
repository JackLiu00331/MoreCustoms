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
    if (!ModelDb.Contains(typeof(FearlessHeroBuff)) || !ModelDb.Contains(typeof(DeadlyEvents)))
    {
      return;
    }

    List<IReadOnlySet<ModifierModel>> updated = __result.ToList();

    ModifierModel fearless;
    ModifierModel deadly;
    try
    {
      fearless = ModelDb.Modifier<FearlessHeroBuff>();
      deadly = ModelDb.Modifier<DeadlyEvents>();
    }
    catch (Exception)
    {
      return;
    }

    bool alreadyExists = updated.Any(group =>
      group.Any(modifier => modifier.GetType() == typeof(FearlessHeroBuff)) &&
      group.Any(modifier => modifier.GetType() == typeof(DeadlyEvents)));

    if (alreadyExists)
    {
      return;
    }

    updated.Add(new HashSet<ModifierModel>
    {
      fearless,
      deadly
    });

    __result = updated;
  }
}
