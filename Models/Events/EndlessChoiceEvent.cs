using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ModTemplate.Modifiers;

namespace ModTemplate.Models.Events;

public class EndlessChoiceEvent : EventModel
{
  private const string DefaultEventLayoutPath = "res://scenes/events/default_event_layout.tscn";

  public const string PrimaryCustomPortraitPath = "res://Resources/images/endless_choice_event.png";

  public const string SecondaryCustomPortraitPath = "res://images/events/endless_choice_event.png";

  public const string GuaranteedPortraitPath = "res://images/packed/modifiers/scaling_plating_debuff.png";

  public static string GetPreferredPortraitPath()
  {
    if (ResourceLoader.Exists(PrimaryCustomPortraitPath))
    {
      return PrimaryCustomPortraitPath;
    }

    if (ResourceLoader.Exists(SecondaryCustomPortraitPath))
    {
      return SecondaryCustomPortraitPath;
    }

    return GuaranteedPortraitPath;
  }

  private static readonly FieldInfo ModifiersBackingField = AccessTools.Field(typeof(RunState), "<Modifiers>k__BackingField");

  private static readonly IReadOnlyList<Type> EndlessChoiceModifierPool = new List<Type>
  {
    typeof(GoldGainBuff),
    typeof(MultiSmithBuff),
    typeof(BossHpDoubleDebuff),
    typeof(ScalingPlatingDebuff),
    typeof(NoPotionDebuff),
    typeof(CloneRunDebuff)
  };

  public override LocString InitialDescription => L10NLookup("ENDLESS_CHOICE_EVENT.pages.INITIAL.description");

  public override IEnumerable<string> GetAssetPaths(IRunState runState)
  {
    return new[]
    {
      DefaultEventLayoutPath,
      GuaranteedPortraitPath
    };
  }

  protected override IReadOnlyList<EventOption> GenerateInitialOptions()
  {
    if (Owner?.RunState is not RunState runState)
    {
      SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.no_choices"));
      return Array.Empty<EventOption>();
    }

    List<Type> candidates = EndlessChoiceModifierPool
      .Where(ModelDb.Contains)
      .Where(type => !runState.Modifiers.Any(modifier => modifier.GetType() == type))
      .ToList();

    if (candidates.Count == 0)
    {
      SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.no_choices"));
      return Array.Empty<EventOption>();
    }

    List<Type> selected = candidates
      .OrderBy(_ => Rng.NextInt(int.MaxValue))
      .Take(3)
      .ToList();

    List<EventOption> options = new List<EventOption>(selected.Count);
    foreach (Type type in selected)
    {
      ModifierModel canonical = ModelDb.GetById<ModifierModel>(ModelDb.GetId(type));
      ModifierModel mutable = canonical.ToMutable();
      options.Add(new EventOption(
        this,
        () => ChooseModifier(mutable),
        mutable.Title,
        mutable.Description,
        $"ENDLESS_CHOICE_EVENT.pages.INITIAL.options.{mutable.Id.Entry}",
        mutable.HoverTips));
    }

    return options;
  }

  private Task ChooseModifier(ModifierModel modifier)
  {
    if (Owner?.RunState is not RunState runState)
    {
      SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.no_choices"));
      return Task.CompletedTask;
    }

    List<ModifierModel> updated = runState.Modifiers.ToList();
    if (updated.Any(existing => existing.GetType() == modifier.GetType()))
    {
      SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.already_has"));
      return Task.CompletedTask;
    }

    updated.Add(modifier);
    ModifiersBackingField.SetValue(runState, updated);
    modifier.OnRunLoaded(runState);

    MainFile.Logger.Info($"[Endless] Endless choice picked modifier: {modifier.Id}.");
    SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.picked"));
    return Task.CompletedTask;
  }
}
