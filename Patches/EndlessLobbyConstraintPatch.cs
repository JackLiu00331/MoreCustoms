using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
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

    modifiers = new List<ModifierModel>
    {
      endless
    };

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