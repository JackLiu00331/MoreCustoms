using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.sts2.Core.Nodes.TopBar;
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

  private static readonly IReadOnlyList<Type> EndlessChoiceDebuffPool = new List<Type>
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

  private const int EndlessDebuffChoiceCount = 3;

  private static readonly LocString EndlessBoonGoldTitle = new("events", "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.gold.title");
  private static readonly LocString EndlessBoonGoldDescription = new("events", "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.gold.description");
  private static readonly LocString EndlessBoonHealTitle = new("events", "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.heal.title");
  private static readonly LocString EndlessBoonHealDescription = new("events", "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.heal.description");
  private static readonly LocString EndlessBoonRelicTitle = new("events", "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.relic.title");
  private static readonly LocString EndlessBoonRelicDescription = new("events", "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.relic.description");

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

    List<Type> candidates = EndlessChoiceDebuffPool
      .Where(ModelDb.Contains)
      .Where(type => !runState.Modifiers.Any(modifier => modifier.GetType() == type))
      .ToList();

    if (candidates.Count == 0)
    {
      return GenerateFallbackEndlessBoonOptions(runState);
    }

    List<Type> selected = candidates
      .OrderBy(_ => Rng.NextInt(int.MaxValue))
      .Take(EndlessDebuffChoiceCount)
      .ToList();

    List<EventOption> options = new List<EventOption>(selected.Count);
    foreach (Type type in selected)
    {
      ModifierModel canonical = ModelDb.GetById<ModifierModel>(ModelDb.GetId(type));
      ModifierModel mutable = canonical.ToMutable();
      options.Add(new EventOption(
        this,
        () => ChooseDebuff(mutable),
        mutable.Title,
        mutable.Description,
        $"ENDLESS_CHOICE_EVENT.pages.INITIAL.options.{mutable.Id.Entry}",
        mutable.HoverTips));
    }

    return options;
  }

  private IReadOnlyList<EventOption> GenerateFallbackEndlessBoonOptions(RunState runState)
  {
    return new List<EventOption>
    {
      new(
        this,
        () => ChooseGoldBoon(runState),
        EndlessBoonGoldTitle,
        EndlessBoonGoldDescription,
        "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.gold",
        Array.Empty<IHoverTip>()),
      new(
        this,
        () => ChooseHealBoon(runState),
        EndlessBoonHealTitle,
        EndlessBoonHealDescription,
        "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.heal",
        Array.Empty<IHoverTip>()),
      new(
        this,
        () => ChooseRelicBoon(runState),
        EndlessBoonRelicTitle,
        EndlessBoonRelicDescription,
        "ENDLESS_CHOICE_EVENT.pages.INITIAL.boons.relic",
        Array.Empty<IHoverTip>())
    };
  }

  private async Task ChooseDebuff(ModifierModel modifier)
  {
    if (Owner?.RunState is not RunState runState)
    {
      SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.no_choices"));
      return;
    }

    List<ModifierModel> updated = runState.Modifiers.ToList();
    if (updated.Any(existing => existing.GetType() == modifier.GetType()))
    {
      SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.already_has"));
      return;
    }

    ApplyRuntimeSafetyBeforeDebuffActivation(modifier, runState);

    updated.Add(modifier);
    ModifiersBackingField.SetValue(runState, updated);
    modifier.OnRunLoaded(runState);
    TryAddModifierIconToTopBar(modifier);

    MainFile.Logger.Info($"[Endless] Endless choice picked modifier: {modifier.Id}.");
    SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.picked"));
  }

  private static void ApplyRuntimeSafetyBeforeDebuffActivation(ModifierModel modifier, RunState runState)
  {
    if (modifier.GetType() != typeof(NoPotionDebuff))
    {
      return;
    }

    foreach (var player in runState.Players)
    {
      foreach (var potion in player.PotionSlots.Where(potion => potion != null).OfType<PotionModel>().ToList())
      {
        player.DiscardPotionInternal(potion);
      }

      NoPotionDebuff.ForceSetPotionSlots(player, 0);
    }
  }

  private static void TryAddModifierIconToTopBar(ModifierModel modifier)
  {
    try
    {
      var topBar = NRun.Instance?.GlobalUi?.TopBar;
      if (topBar == null)
      {
        return;
      }

      Control? modifiersContainer = topBar.GetNodeOrNull<Control>("%Modifiers");
      if (modifiersContainer == null)
      {
        return;
      }

      NTopBarModifier? iconNode = NTopBarModifier.Create(modifier);
      if (iconNode == null)
      {
        return;
      }

      modifiersContainer.Visible = true;
      modifiersContainer.AddChild(iconNode);
    }
    catch (Exception ex)
    {
      MainFile.Logger.Warn($"[Endless] Failed to update top bar modifier icon dynamically: {ex.Message}");
    }
  }

  private async Task ChooseGoldBoon(RunState runState)
  {
    int endlessDepth = Math.Max(1, runState.CurrentActIndex - 2);
    int goldAmount = 60 + endlessDepth * 20;

    foreach (var player in runState.Players)
    {
      await PlayerCmd.GainGold(goldAmount, player);
    }

    MainFile.Logger.Info($"[Endless] Endless boon picked: gold +{goldAmount} for each player.");
    SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.picked"));
  }

  private async Task ChooseHealBoon(RunState runState)
  {
    foreach (var player in runState.Players)
    {
      int healAmount = Math.Max(1, (int)Math.Ceiling(player.Creature.MaxHp * 0.20m));
      await CreatureCmd.Heal(player.Creature, healAmount);
    }

    MainFile.Logger.Info("[Endless] Endless boon picked: heal each player by 20% max HP.");
    SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.picked"));
  }

  private async Task ChooseRelicBoon(RunState runState)
  {
    foreach (var player in runState.Players)
    {
      var relic = RelicFactory.PullNextRelicFromFront(player).ToMutable();
      await RelicCmd.Obtain(relic, player);
    }

    MainFile.Logger.Info("[Endless] Endless boon picked: each player gained a relic.");
    SetEventFinished(L10NLookup("ENDLESS_CHOICE_EVENT.pages.DONE.picked"));
  }
}
