using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

// 无尽模式 Boss 奖励注入补丁
// 原版在"当前是最后一幕"时 WithRewardsFromRoom 会提前返回（空奖励列表）。
// 本补丁在此之后注入按无尽深度缩放的奖励：
//   普通幕Boss : 金币(随深度递增) + 概率药水 + 3张卡牌 + 1遗物
//   双Boss幕(4/8/12...) : 金币额外+50 + 必出药水 + 3张卡牌 + 2遗物
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
public static class EndlessTerminalBossRewardInjectionPatch
{
  // 双Boss幕节奏：actIndex 4, 8, 12, 16 ...
  private static bool IsDoubleBossAct(int actIndex) => actIndex >= 4 && actIndex % 4 == 0;

  [HarmonyPostfix]
  private static void InjectRewardsForTerminalBoss(RewardsSet __instance, AbstractRoom room)
  {
    if (room is not CombatRoom combatRoom || room.RoomType != RoomType.Boss)
      return;

    Player player = __instance.Player;
    IRunState? runState = player.RunState;
    if (!InfinityEndlessModeDebuff.IsActive(runState) || runState == null || runState.CurrentActIndex < 2)
      return;

    // 只在原版短路（奖励为空）时介入
    if (__instance.Rewards.Count > 0)
      return;

    int actIndex = runState.CurrentActIndex;
    int endlessDepth = actIndex - 2;           // 第3幕起 depth=1，每幕+1
    bool isDoubleBoss = IsDoubleBossAct(actIndex);

    // ── 金币：基础100 + 每深度+15，双Boss额外+50 ──────────────────────
    int goldAmount = combatRoom.Encounter.MinGoldReward + endlessDepth * 15 + (isDoubleBoss ? 50 : 0);
    __instance.Rewards.Add(new GoldReward(goldAmount, goldAmount, player));

    // ── 药水：双Boss必掉，普通按原版概率 ────────────────────────────────
    bool givePotion = isDoubleBoss;
    if (!givePotion && RunManager.Instance?.AscensionManager != null)
      givePotion = player.PlayerOdds.PotionReward.Roll(player, RunManager.Instance.AscensionManager, RoomType.Boss);
    if (givePotion)
      __instance.Rewards.Add(new PotionReward(player));

    // ── 卡牌：始终3张Boss级选择 ─────────────────────────────────────────
    __instance.Rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, RoomType.Boss), 3, player));

    // ── 遗物：普通幕1个，双Boss幕2个（补偿复利怪物缩放）─────────────────
    __instance.Rewards.Add(new RelicReward(player));
    if (isDoubleBoss)
      __instance.Rewards.Add(new RelicReward(player));

    int relicCount = isDoubleBoss ? 2 : 1;
    MainFile.Logger.Info($"[Endless] Boss rewards injected: gold={goldAmount} potion={givePotion} relics={relicCount} doubleBoss={isDoubleBoss} actIndex={actIndex} depth={endlessDepth}");
  }
}
