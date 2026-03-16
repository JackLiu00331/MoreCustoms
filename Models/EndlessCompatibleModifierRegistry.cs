using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Models;

public static class EndlessCompatibleModifierRegistry
{
  private static readonly HashSet<Type> CompatibleModifierTypes = new()
  {
    typeof(Flight),
    typeof(CharacterCards),
    typeof(GoldGainBuff),
    typeof(MultiSmithBuff)
  };

  private static readonly IReadOnlyList<Type> EndlessAncientNegativeModifierTypes = new List<Type>
  {
    typeof(BigGameHunter),
    typeof(DeadlyEvents),
    typeof(Murderous),
    typeof(NightTerrors),
    typeof(Terminal),
    typeof(CursedRun),
    typeof(BossHpDoubleDebuff),
    typeof(ScalingPlatingDebuff),
    typeof(NoPotionDebuff)
  };

  public static bool IsCompatibleWithEndless(ModifierModel modifier)
  {
    return IsCompatibleWithEndless(modifier.GetType());
  }

  public static bool IsCompatibleWithEndless(Type modifierType)
  {
    return CompatibleModifierTypes.Contains(modifierType);
  }

  public static IReadOnlyList<ModifierModel> CreateEndlessAncientChoiceModifiers(RunState runState)
  {
    List<ModifierModel> choices = new();

    AddSimpleModifiers(runState, choices, EndlessAncientNegativeModifierTypes);
    AddSimpleModifiers(runState, choices, CompatibleModifierTypes.Where(type => type != typeof(CharacterCards)));
    AddCharacterCardChoices(runState, choices);

    return choices;
  }

  private static void AddSimpleModifiers(RunState runState, List<ModifierModel> choices, IEnumerable<Type> modifierTypes)
  {
    foreach (Type modifierType in modifierTypes.Distinct())
    {
      ModifierModel? modifier = TryCreateMutableModifier(modifierType);
      if (modifier == null || RunAlreadyHasEquivalentModifier(runState, modifier))
      {
        continue;
      }

      choices.Add(modifier);
    }
  }

  private static void AddCharacterCardChoices(RunState runState, List<ModifierModel> choices)
  {
    if (!ModelDb.Contains(typeof(CharacterCards)))
    {
      return;
    }

    HashSet<ModelId> excludedCharacterIds = runState.Players
      .Select(player => player.Character.Id)
      .ToHashSet();

    foreach (CharacterCards activeModifier in runState.Modifiers.OfType<CharacterCards>())
    {
      excludedCharacterIds.Add(activeModifier.CharacterModel);
    }

    foreach (CharacterModel character in ModelDb.AllCharacters)
    {
      if (excludedCharacterIds.Contains(character.Id))
      {
        continue;
      }

      CharacterCards? modifier = TryCreateMutableModifier<CharacterCards>();
      if (modifier == null)
      {
        continue;
      }

      modifier.CharacterModel = character.Id;
      if (RunAlreadyHasEquivalentModifier(runState, modifier))
      {
        continue;
      }

      choices.Add(modifier);
    }
  }

  private static bool RunAlreadyHasEquivalentModifier(RunState runState, ModifierModel candidate)
  {
    return runState.Modifiers.Any(existing => existing.IsEquivalent(candidate));
  }

  private static T? TryCreateMutableModifier<T>() where T : ModifierModel
  {
    return TryCreateMutableModifier(typeof(T)) as T;
  }

  private static ModifierModel? TryCreateMutableModifier(Type modifierType)
  {
    if (!ModelDb.Contains(modifierType))
    {
      return null;
    }

    try
    {
      return ModelDb.GetById<ModifierModel>(ModelDb.GetId(modifierType)).ToMutable();
    }
    catch
    {
      return null;
    }
  }
}