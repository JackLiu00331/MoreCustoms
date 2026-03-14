using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using ModTemplate.Models.Events;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(RunManager))]
public static class EndlessTransitionEventNodePatch
{
  private static readonly PropertyInfo StateProperty = AccessTools.Property(typeof(RunManager), "State");

  private static readonly HashSet<RunState> PendingTransitionEventNodeRuns = new();

  [HarmonyPatch(nameof(RunManager.EnterAct))]
  [HarmonyPrefix]
  private static void MarkPendingTransitionNodeBeforeEnterAct(RunManager __instance, int currentActIndex)
  {
    RunState? state = StateProperty.GetValue(__instance) as RunState;
    if (state == null || !InfinityEndlessModeDebuff.IsActive(state))
    {
      return;
    }

    if (currentActIndex < 3)
    {
      return;
    }

    PendingTransitionEventNodeRuns.Add(state);
  }

  [HarmonyPatch("RollRoomTypeFor")]
  [HarmonyPrefix]
  private static bool ForceEventRoomForTransitionAncientNode(RunManager __instance, MegaCrit.Sts2.Core.Map.MapPointType pointType, ref RoomType __result)
  {
    RunState? state = StateProperty.GetValue(__instance) as RunState;
    if (state == null || !PendingTransitionEventNodeRuns.Contains(state))
    {
      return true;
    }

    if (!IsFirstRoomSelectionInAct(state))
    {
      return true;
    }

    if (pointType != MegaCrit.Sts2.Core.Map.MapPointType.Ancient)
    {
      return true;
    }

    __result = RoomType.Event;
    return false;
  }

  [HarmonyPatch("CreateRoom")]
  [HarmonyPrefix]
  private static bool ReplaceTransitionAncientWithEndlessChoice(
    RunManager __instance,
    RoomType roomType,
    MegaCrit.Sts2.Core.Map.MapPointType mapPointType,
    AbstractModel? model,
    ref AbstractRoom __result)
  {
    RunState? state = StateProperty.GetValue(__instance) as RunState;
    if (state == null || !PendingTransitionEventNodeRuns.Contains(state))
    {
      return true;
    }

    if (!IsFirstRoomSelectionInAct(state))
    {
      return true;
    }

    if (roomType != RoomType.Event || mapPointType != MegaCrit.Sts2.Core.Map.MapPointType.Ancient)
    {
      return true;
    }

    if (!ModelDb.Contains(typeof(EndlessChoiceEvent)))
    {
      MainFile.Logger.Warn("[Endless] EndlessChoiceEvent model not found; fallback to vanilla event.");
      PendingTransitionEventNodeRuns.Remove(state);
      return true;
    }

    __result = new EventRoom(ModelDb.Event<EndlessChoiceEvent>());
    PendingTransitionEventNodeRuns.Remove(state);
    MainFile.Logger.Info($"[Endless] Replaced transition ancient node with endless choice event for act {state.CurrentActIndex + 1}.");
    return false;
  }

  private static bool IsFirstRoomSelectionInAct(RunState state)
  {
    if (state.MapPointHistory.Count <= state.CurrentActIndex)
    {
      return true;
    }

    IReadOnlyList<MapPointHistoryEntry> historyForAct = state.MapPointHistory[state.CurrentActIndex];
    return historyForAct.Count == 0;
  }
}
