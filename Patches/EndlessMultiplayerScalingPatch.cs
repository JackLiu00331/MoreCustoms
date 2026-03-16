using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Rooms;

namespace ModTemplate.Patches;

/// <summary>
/// 修复无尽模式联机下进入战斗黑屏：
/// 原版 MultiplayerScalingModel.GetMultiplayerScaling 仅接受 actIndex 0..2，
/// 无尽第4幕后会传入 3+，触发 ArgumentOutOfRangeException 并中断进房流程。
///
/// 处理策略：对越界 actIndex 做安全映射，沿用原版第三幕后的倍率档位：
/// - Boss 房：1.3
/// - 非 Boss 房：1.2
/// </summary>
[HarmonyPatch(typeof(MultiplayerScalingModel), nameof(MultiplayerScalingModel.GetMultiplayerScaling))]
public static class EndlessMultiplayerScalingPatch
{
  [HarmonyPrefix]
  private static bool ClampActIndexForEndless(EncounterModel? encounter, int actIndex, ref decimal __result)
  {
    if (actIndex is >= 0 and <= 2)
    {
      return true;
    }

    __result = encounter != null && encounter.RoomType == RoomType.Boss ? 1.3m : 1.2m;
    MainFile.Logger.Warn($"[Endless] Clamped multiplayer scaling actIndex {actIndex} to fallback tier (boss={encounter?.RoomType == RoomType.Boss}).");
    return false;
  }
}
