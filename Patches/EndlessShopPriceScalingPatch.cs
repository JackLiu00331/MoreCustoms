using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

// 无尽模式商店通货膨胀补丁
// 每进入一个无尽深度（act 3 起 depth=1），商品涨价 10%，最高翻倍（depth=10+）。
// 适配怪物血量/力量复利增长，使商店后期移牌/遗物选购决策仍具意义。
[HarmonyPatch(typeof(MerchantEntry), "get_Cost")]
public static class EndlessShopPriceScalingPatch
{
  private static readonly FieldInfo PlayerField = AccessTools.Field(typeof(MerchantEntry), "_player");

  [HarmonyPostfix]
  private static void ScalePriceForEndlessDepth(MerchantEntry __instance, ref int __result)
  {
    Player? player = PlayerField.GetValue(__instance) as Player;
    if (player == null) return;

    var runState = player.RunState;
    if (!InfinityEndlessModeDebuff.IsActive(runState)) return;

    int endlessDepth = runState.CurrentActIndex - 2;
    if (endlessDepth <= 0) return;

    // 每层 +10%，最高 +100%（即第 10 层及以后价格翻倍）
    double multiplier = Math.Min(1.0 + endlessDepth * 0.10, 2.0);
    __result = (int)Math.Round(__result * multiplier);
  }
}
