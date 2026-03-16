using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace ModTemplate.Patches;

/// <summary>
/// 修复无尽联机切幕时偶发的 NMapScreen.SetMap 崩溃：
/// ObjectDisposedException(NBossMapPoint) 会中断 EnterAct 流程并表现为黑屏。
///
/// 处理策略：捕获该异常并在下一帧 deferred 重试 SetMap。
/// 只对 NBossMapPoint 这一已知竞态做兜底，其他异常保持原样抛出。
/// </summary>
[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
public static class EndlessMapSetMapSafetyPatch
{
  private static bool _retryQueued;

  [HarmonyFinalizer]
  private static Exception? RetryOnDisposedBossPoint(
    Exception? __exception,
    NMapScreen __instance,
    ActMap map,
    uint seed,
    bool clearDrawings)
  {
    if (__exception == null)
    {
      _retryQueued = false;
      return null;
    }

    if (__exception is ObjectDisposedException disposed &&
        string.Equals(disposed.ObjectName, "MegaCrit.Sts2.Core.Nodes.Screens.Map.NBossMapPoint", StringComparison.Ordinal))
    {
      if (!_retryQueued)
      {
        _retryQueued = true;
        MainFile.Logger.Warn("[Endless] NMapScreen.SetMap hit disposed NBossMapPoint; scheduling deferred SetMap retry.");
        __instance.CallDeferred("RemoveAllMapPointsAndPaths");
        TaskHelper.RunSafely(RunManager.Instance.GenerateMap());
      }
      else
      {
        MainFile.Logger.Warn("[Endless] NMapScreen.SetMap retry already queued; suppressing duplicate disposed exception.");
      }

      return null;
    }

    return __exception;
  }
}
