using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Models;
using ModTemplate.Models.Events;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(EventModel), nameof(EventModel.CreateInitialPortrait))]
public static class EndlessChoiceEventPortraitPatch
{
  [HarmonyPrefix]
  private static bool UseFallbackPortraitForEndlessChoice(EventModel __instance, ref Texture2D __result)
  {
    if (__instance is not EndlessChoiceEvent)
    {
      return true;
    }

    try
    {
      __result = PreloadManager.Cache.GetTexture2D(EndlessChoiceEvent.GuaranteedPortraitPath);
      MainFile.Logger.Info($"[Endless] Using fixed portrait '{EndlessChoiceEvent.GuaranteedPortraitPath}'.");
    }
    catch (System.Exception ex)
    {
      MainFile.Logger.Error($"[Endless] Failed to load fallback portrait '{EndlessChoiceEvent.GuaranteedPortraitPath}': {ex.Message}");
      throw;
    }

    return false;
  }
}
