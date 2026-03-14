using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(Pael), "GenerateInitialOptions")]
public static class CloneRunPaelOptionsPatch
{
  private static readonly System.Reflection.MethodInfo RelicOptionMethod = AccessTools.Method(
    typeof(AncientEventModel),
    "RelicOption",
    new[] { typeof(RelicModel), typeof(string), typeof(string) });

  [HarmonyPrefix]
  private static bool ForceSingleGrowthOption(Pael __instance, ref IReadOnlyList<EventOption> __result)
  {
    if (!CloneRunDebuff.IsActiveForPlayer(__instance.Owner))
    {
      return true;
    }

    object? reflectedOption = RelicOptionMethod.Invoke(
      __instance,
      new object?[] { ModelDb.Relic<PaelsGrowth>().ToMutable(), "INITIAL", null });

    if (reflectedOption is not EventOption growthOption)
    {
      return true;
    }

    __result = new List<EventOption>
    {
      growthOption
    };
    return false;
  }
}