using System.Linq;
using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Monsters;
using ModTemplate.Config;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(TestSubject), "Revive")]
public static class TestSubjectBossPatch
{
  [HarmonyPrefix]
  private static void DoubleReviveHpWhenDebuffActive(TestSubject __instance, ref int baseRespawnHp)
  {
    if (__instance.Creature?.CombatState?.Modifiers == null)
    {
      return;
    }

    bool hasDebuff = __instance.Creature.CombatState.Modifiers.Any(modifier => modifier.GetType() == typeof(BossHpDoubleDebuff));
    if (!hasDebuff)
    {
      return;
    }

    decimal multiplier = MoreCustomsConfig.Current.BossHpMultiplier;
    if (multiplier <= 0m)
    {
      return;
    }

    baseRespawnHp = Math.Max(1, (int)Math.Round(baseRespawnHp * multiplier, MidpointRounding.AwayFromZero));
  }
}
