using System;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace ModTemplate.Patches;

/// <summary>
/// 修复无尽联机切幕时偶发的 NMapScreen.SetMap 崩溃：
/// ObjectDisposedException(NBossMapPoint) 会中断 EnterAct 流程并表现为黑屏。
///
/// 处理策略：捕获该异常后延迟一小段时间重建地图。
/// 只对 NBossMapPoint 这一已知竞态做兜底，其他异常保持原样抛出。
/// </summary>
[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
public static class EndlessMapSetMapSafetyPatch
{
  private static int _retryInFlight;

  [HarmonyFinalizer]
  private static Exception? RetryOnDisposedBossPoint(
    Exception? __exception,
    NMapScreen __instance)
  {
    if (__exception == null)
    {
      return null;
    }

    if (__exception is ObjectDisposedException disposed &&
        string.Equals(disposed.ObjectName, "MegaCrit.Sts2.Core.Nodes.Screens.Map.NBossMapPoint", StringComparison.Ordinal))
    {
      if (Interlocked.CompareExchange(ref _retryInFlight, 1, 0) == 0)
      {
        MainFile.Logger.Warn("[Endless] NMapScreen.SetMap hit disposed NBossMapPoint; scheduling delayed GenerateMap recovery.");
        TaskHelper.RunSafely(RecoverMapAsync());
      }
      else
      {
        MainFile.Logger.Warn("[Endless] NMapScreen.SetMap recovery already in flight; suppressing duplicate disposed exception.");
      }

      return null;
    }

    return __exception;
  }

  private static async Task RecoverMapAsync()
  {
    try
    {
      await Task.Delay(120);
      await RunManager.Instance.GenerateMap();
    }
    finally
    {
      Interlocked.Exchange(ref _retryInFlight, 0);
    }
  }
}
