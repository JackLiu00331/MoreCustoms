using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
public static class EndlessEnterNextActPatch
{
  internal static readonly IReadOnlyList<ActModel> EndlessActPool = new List<ActModel>
  {
    ModelDb.Act<Overgrowth>(),
    ModelDb.Act<Hive>(),
    ModelDb.Act<Glory>()
  };

  private sealed class EndlessProgress
  {
    public int InitialActCount { get; set; }

    public int LoopCount { get; set; }
  }

  private static readonly PropertyInfo StateProperty = AccessTools.Property(typeof(RunManager), "State");

  internal static readonly FieldInfo ActsBackingField = AccessTools.Field(typeof(RunState), "<Acts>k__BackingField");

  private static readonly Dictionary<RunState, EndlessProgress> ProgressByRun = new();

  private static readonly HashSet<RunState> TransitioningRuns = new();

  private static readonly object TransitionLock = new();

  [HarmonyPrefix]
  private static bool ContinueIntoEndlessLoop(RunManager __instance, ref Task __result)
  {
    RunState? runState = StateProperty.GetValue(__instance) as RunState;
    if (!InfinityEndlessModeDebuff.IsActive(runState))
    {
      return true;
    }

    if (runState == null || runState.CurrentActIndex < runState.Acts.Count - 1)
    {
      return true;
    }

    lock (TransitionLock)
    {
      if (!TransitioningRuns.Add(runState))
      {
        MainFile.Logger.Warn($"[Endless] Ignored duplicate EnterNextAct while endless transition is already in progress. actIndex={runState.CurrentActIndex}");
        __result = Task.CompletedTask;
        return false;
      }
    }

    MainFile.Logger.Info($"[Endless] Intercepted EnterNextAct at final act index {runState.CurrentActIndex}. Skipping terminal architect flow and continuing loop.");

    __result = EnterEndlessActAsync(__instance, runState);
    return false;
  }

  private static async Task EnterEndlessActAsync(RunManager runManager, RunState runState)
  {
    try
    {
      using (new NetLoadingHandle(runManager.NetService))
      {
        EndlessProgress progress = GetOrCreateProgress(runState);
        progress.LoopCount++;
        AppendLoopAct(runState, progress.LoopCount);

        int nextActIndex = runState.CurrentActIndex + 1;
        MainFile.Logger.Info($"[Endless] Loop #{progress.LoopCount} appended (initial acts={progress.InitialActCount}). Entering act index {nextActIndex}.");
        await EnterActWithRetry(runManager, nextActIndex);
      }
    }
    finally
    {
      lock (TransitionLock)
      {
        TransitioningRuns.Remove(runState);
      }
    }
  }

  private static async Task EnterActWithRetry(RunManager runManager, int nextActIndex)
  {
    const int maxAttempts = 6;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
      try
      {
        await runManager.EnterAct(nextActIndex);
        return;
      }
      catch (ObjectDisposedException ex) when (attempt < maxAttempts)
      {
        int delayMs = 120 * attempt;
        MainFile.Logger.Warn($"[Endless] EnterAct hit disposed object ({ex.ObjectName}) on attempt {attempt}/{maxAttempts}. Retrying in {delayMs}ms for act {nextActIndex}.");
        await Task.Yield();
        await Task.Delay(delayMs);
      }
    }

    await runManager.EnterAct(nextActIndex);
  }

  private static EndlessProgress GetOrCreateProgress(RunState runState)
  {
    if (ProgressByRun.TryGetValue(runState, out EndlessProgress? progress))
    {
      return progress;
    }

    progress = new EndlessProgress
    {
      InitialActCount = runState.Acts.Count,
      LoopCount = 0
    };
    ProgressByRun[runState] = progress;
    return progress;
  }

  internal static void AppendLoopAct(RunState runState, int loopCount)
  {
    List<ActModel> acts = runState.Acts.ToList();
    ActModel canonical = runState.Rng.UpFront.NextItem(EndlessActPool) ?? ModelDb.Act<Overgrowth>();
    ActModel loopAct = canonical.ToMutable();
    loopAct.GenerateRooms(runState.Rng.UpFront, runState.UnlockState, runState.Players.Count > 1);
    MainFile.Logger.Info($"[Endless] Loop #{loopCount} act selected: {canonical.Id}");

    if ((loopCount - 1) % 4 == 0)
    {
      EncounterModel? secondBoss = runState.Rng.UpFront.NextItem(loopAct.AllBossEncounters.Where(encounter => encounter.Id != loopAct.BossEncounter.Id));
      if (secondBoss != null)
      {
        loopAct.SetSecondBossEncounter(secondBoss);
        MainFile.Logger.Info($"[Endless] Loop #{loopCount} configured with second boss: {secondBoss.Id} (act #{runState.Acts.Count + 1}).");
      }
    }

    acts.Add(loopAct);
    ActsBackingField.SetValue(runState, acts);
  }
}