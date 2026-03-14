using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(RunManager), "CreateRoom")]
public static class EndlessTreasureRoomPatch
{
  private static readonly PropertyInfo StateProperty = AccessTools.Property(typeof(RunManager), "State");

  [HarmonyPrefix]
  private static bool ClampTreasureActIndexForEndless(
    RunManager __instance,
    RoomType roomType,
    MapPointType mapPointType,
    AbstractModel? model,
    ref AbstractRoom __result)
  {
    if (roomType != RoomType.Treasure)
    {
      return true;
    }

    RunState? runState = StateProperty.GetValue(__instance) as RunState;
    if (runState == null || !InfinityEndlessModeDebuff.IsActive(runState) || runState.CurrentActIndex <= 2)
    {
      return true;
    }

    int safeActIndex = MapActToTreasureIndex(runState.Act);
    __result = new TreasureRoom(safeActIndex);
    MainFile.Logger.Info($"[Endless] Remapped TreasureRoom act index {runState.CurrentActIndex} -> {safeActIndex} for act {runState.Act.Id}.");
    return false;
  }

  private static int MapActToTreasureIndex(ActModel act)
  {
    if (act is Overgrowth)
    {
      return 0;
    }

    if (act is Hive)
    {
      return 1;
    }

    if (act is Glory)
    {
      return 2;
    }

    if (act is Underdocks)
    {
      return 0;
    }

    return 0;
  }
}