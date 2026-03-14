using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(NCustomRunModifiersList), "AfterModifiersChanged")]
public static class ModifierToggleExclusionSafetyPatch
{
  private static readonly System.Reflection.FieldInfo TickboxesField = AccessTools.Field(typeof(NCustomRunModifiersList), "_modifierTickboxes");

  [HarmonyPostfix]
  private static void EnforceModifierExclusionRules(NCustomRunModifiersList __instance, NRunModifierTickbox tickbox)
  {
    if (tickbox?.Modifier == null || !tickbox.IsTicked)
    {
      return;
    }

    List<NRunModifierTickbox>? tickboxes = TickboxesField.GetValue(__instance) as List<NRunModifierTickbox>;
    if (tickboxes == null)
    {
      return;
    }

    bool isEndless = tickbox.Modifier.GetType() == typeof(InfinityEndlessModeDebuff);
    if (isEndless)
    {
      foreach (NRunModifierTickbox other in tickboxes)
      {
        if (other == tickbox || other.Modifier == null)
        {
          continue;
        }

        if (other.IsTicked)
        {
          other.IsTicked = false;
        }
      }

      return;
    }

    NRunModifierTickbox? endlessTickbox = tickboxes.FirstOrDefault(other => other.Modifier?.GetType() == typeof(InfinityEndlessModeDebuff));
    if (endlessTickbox?.IsTicked == true)
    {
      tickbox.IsTicked = false;
      return;
    }

    bool isFearless = tickbox.Modifier.GetType() == typeof(FearlessHeroBuff);
    bool isBigGameHunter = tickbox.Modifier.GetType() == typeof(BigGameHunter);
    if (!isFearless && !isBigGameHunter)
    {
      return;
    }

    foreach (NRunModifierTickbox other in tickboxes)
    {
      if (other == tickbox || other.Modifier == null)
      {
        continue;
      }

      bool shouldUntick = isFearless
        ? other.Modifier.GetType() == typeof(BigGameHunter)
        : other.Modifier.GetType() == typeof(FearlessHeroBuff);

      if (shouldUntick && other.IsTicked)
      {
        other.IsTicked = false;
      }
    }
  }
}