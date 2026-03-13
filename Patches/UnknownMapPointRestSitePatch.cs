using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(UnknownMapPointOdds), nameof(UnknownMapPointOdds.Roll))]
public static class UnknownMapPointRestSitePatch
{
  [HarmonyPrefix]
  private static bool AllowRestSiteFromUnknown(IEnumerable<RoomType> blacklist, IRunState runState, ref RoomType __result)
  {
    if (!FearlessHeroBuff.TryRollUnknownRoom(runState, blacklist, out RoomType roomType))
    {
      return true;
    }

    __result = roomType;
    return false;
  }
}
