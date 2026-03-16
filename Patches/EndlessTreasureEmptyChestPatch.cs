using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace ModTemplate.Patches;

/// <summary>
/// 修复无尽模式下宝袋耗尽时开宝箱卡死的问题。
///
/// 根本原因：TreasureRoomRelicSynchronizer.BeginRelicPicking() 调用
/// _sharedGrabBag.PullFromFront() 时，当宝袋已空会返回 null，但代码直接
/// _currentRelics.Add(null)，导致 CurrentRelics 包含 null 项。UI 在
/// InitializeRelics() 中读取时发现 Count > 0，尝试渲染 null 遗物 → 崩溃。
///
/// 修复思路：在 BeginRelicPicking() 完成后，通过 Postfix 清除 _currentRelics
/// 中的 null 项。当列表清空时，NTreasureRoomRelicCollection.InitializeRelics()
/// 会检测到 Count == 0，走原版"空宝箱"分支：显示提示文字，并通过
/// CompleteWithNoRelics() 正常结束宝箱流程。金币奖励由 DoNormalRewards() 独立
/// 发放，不受此影响，玩家仍会获得正常的 42–53 金币。
/// </summary>
[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), "BeginRelicPicking")]
public static class EndlessTreasureEmptyChestPatch
{
  private static readonly FieldInfo CurrentRelicsField =
    AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_currentRelics");

  private static readonly FieldInfo PlayerCollectionField =
    AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_playerCollection");

  [HarmonyPostfix]
  private static void RemoveNullRelicsFromPool(TreasureRoomRelicSynchronizer __instance)
  {
    if (AnyPlayerHasAllTreasureRelics(__instance))
    {
      CurrentRelicsField.SetValue(__instance, new List<RelicModel>());
      MainFile.Logger.Info("[TreasureRoom] At least one player has all eligible relics. Forcing empty chest state.");
      return;
    }

    if (CurrentRelicsField.GetValue(__instance) is not List<RelicModel> currentRelics)
    {
      return;
    }

    int before = currentRelics.Count;
    currentRelics.RemoveAll(r => r == null);
    int removed = before - currentRelics.Count;

    if (removed > 0)
    {
      MainFile.Logger.Info(
        $"[TreasureRoom] Relic pool exhausted: removed {removed} null relic slot(s). " +
        "Chest will show empty state with gold reward instead.");
    }
  }

  private static bool AnyPlayerHasAllTreasureRelics(TreasureRoomRelicSynchronizer synchronizer)
  {
    if (PlayerCollectionField.GetValue(synchronizer) is not IPlayerCollection playerCollection)
    {
      return false;
    }

    foreach (Player player in playerCollection.Players)
    {
      if (PlayerHasAllEligibleTreasureRelics(player))
      {
        return true;
      }
    }

    return false;
  }

  private static bool PlayerHasAllEligibleTreasureRelics(Player player)
  {
    RunState? runState = player.RunState as RunState;
    if (runState == null)
    {
      return false;
    }

    HashSet<ModelId> owned = player.Relics.Select(r => r.Id).ToHashSet();
    IEnumerable<RelicModel> unlocked = ModelDb.RelicPool<SharedRelicPool>().GetUnlockedRelics(player.UnlockState)
      .Concat(player.Character.RelicPool.GetUnlockedRelics(player.UnlockState));

    foreach (RelicModel relic in unlocked)
    {
      if (relic.Rarity is not (RelicRarity.Common or RelicRarity.Uncommon or RelicRarity.Rare or RelicRarity.Shop))
      {
        continue;
      }

      if (!relic.IsAllowed(runState))
      {
        continue;
      }

      if (!owned.Contains(relic.Id))
      {
        return false;
      }
    }

    return true;
  }
}
