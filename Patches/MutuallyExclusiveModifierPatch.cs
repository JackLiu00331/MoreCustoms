using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(ModelDb))]
public static class MutuallyExclusiveModifierPatch
{
  [HarmonyPriority(Priority.Last)]
  [HarmonyPatch("get_MutuallyExclusiveModifiers")]
  [HarmonyPostfix]
  private static void AddCustomMutualExclusion(ref IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
  {
    List<IReadOnlySet<ModifierModel>> updated = __result.ToList();

    AddFearlessExclusions(updated);
    AddEndlessExclusions(updated);

    __result = updated;
  }

  private static void AddFearlessExclusions(List<IReadOnlySet<ModifierModel>> groups)
  {
    if (!ModelDb.Contains(typeof(FearlessHeroBuff)))
    {
      return;
    }

    ModifierModel fearless;
    try
    {
      fearless = ModelDb.Modifier<FearlessHeroBuff>();
    }
    catch (Exception)
    {
      return;
    }

    if (ModelDb.Contains(typeof(DeadlyEvents)))
    {
      TryAddMutualExclusionGroup<DeadlyEvents>(groups, fearless);
    }

    TryAddMutualExclusionGroup<BigGameHunter>(groups, fearless);
  }

  private static void AddEndlessExclusions(List<IReadOnlySet<ModifierModel>> groups)
  {
    if (!ModelDb.Contains(typeof(InfinityEndlessModeDebuff)))
    {
      return;
    }

    ModifierModel endless;
    try
    {
      endless = ModelDb.Modifier<InfinityEndlessModeDebuff>();
    }
    catch (Exception)
    {
      return;
    }

    List<ModifierModel> allModifiers = ModelDb.GoodModifiers
      .Concat(ModelDb.BadModifiers)
      .Where(modifier => modifier.GetType() != typeof(InfinityEndlessModeDebuff))
      .GroupBy(modifier => modifier.GetType())
      .Select(group => group.First())
      .ToList();

    foreach (ModifierModel modifier in allModifiers)
    {
      TryAddMutualExclusionGroup(groups, endless, modifier);
    }
  }

  private static void TryAddMutualExclusionGroup<T>(List<IReadOnlySet<ModifierModel>> groups, ModifierModel fearless)
    where T : ModifierModel
  {
    if (!ModelDb.Contains(typeof(T)))
    {
      return;
    }

    ModifierModel other;
    try
    {
      other = ModelDb.Modifier<T>();
    }
    catch (Exception)
    {
      return;
    }

    bool alreadyExists = groups.Any(group =>
      group.Any(modifier => modifier.GetType() == typeof(FearlessHeroBuff)) &&
      group.Any(modifier => modifier.GetType() == typeof(T)));

    if (alreadyExists)
    {
      return;
    }

    groups.Add(new HashSet<ModifierModel>
    {
      fearless,
      other
    });
  }

  private static void TryAddMutualExclusionGroup(List<IReadOnlySet<ModifierModel>> groups, ModifierModel anchor, ModifierModel other)
  {
    Type otherType = other.GetType();

    bool alreadyExists = groups.Any(group =>
      group.Any(modifier => modifier.GetType() == anchor.GetType()) &&
      group.Any(modifier => modifier.GetType() == otherType));

    if (alreadyExists)
    {
      return;
    }

    groups.Add(new HashSet<ModifierModel>
    {
      anchor,
      other
    });
  }
}
