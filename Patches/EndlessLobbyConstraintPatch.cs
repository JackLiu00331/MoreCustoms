using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using ModTemplate.Models;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(StartRunLobby))]
public static class EndlessLobbyConstraintPatch
{
  [HarmonyPatch(nameof(StartRunLobby.SetModifiers))]
  [HarmonyPrefix]
  private static void EnforceEndlessExclusiveModifiers(StartRunLobby __instance, ref List<ModifierModel> modifiers)
  {
    if (modifiers == null || modifiers.Count == 0)
    {
      return;
    }

    ModifierModel? endless = modifiers.FirstOrDefault(modifier => modifier.GetType() == typeof(InfinityEndlessModeDebuff));
    if (endless == null)
    {
      return;
    }

    List<ModifierModel> filteredModifiers = modifiers
      .Where(modifier => modifier.GetType() == typeof(InfinityEndlessModeDebuff) || EndlessCompatibleModifierRegistry.IsCompatibleWithEndless(modifier))
      .ToList();

    if (filteredModifiers.Count != modifiers.Count)
    {
      List<string> removedModifiers = modifiers
        .Except(filteredModifiers)
        .Select(modifier => modifier.GetType().Name)
        .Distinct()
        .OrderBy(name => name)
        .ToList();

      MainFile.Logger.Info($"[Endless] Removed incompatible modifiers while endless mode is active: {string.Join(", ", removedModifiers)}");
    }

    modifiers = filteredModifiers;

    if (__instance.Ascension != 0)
    {
      __instance.SyncAscensionChange(0);
      MainFile.Logger.Info("[Endless] Forced ascension to 0 because endless mode was selected.");
    }
  }

  [HarmonyPatch(nameof(StartRunLobby.SyncAscensionChange))]
  [HarmonyPrefix]
  private static void BlockAscensionWhenEndlessActive(StartRunLobby __instance, ref int ascension)
  {
    if (ascension <= 0)
    {
      return;
    }

    if (!__instance.Modifiers.Any(modifier => modifier.GetType() == typeof(InfinityEndlessModeDebuff)))
    {
      return;
    }

    MainFile.Logger.Info($"[Endless] Ignored ascension change {ascension} because endless mode is active. Using 0 instead.");
    ascension = 0;
  }
}