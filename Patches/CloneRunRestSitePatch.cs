using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using ModTemplate.Modifiers;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(CloneRestSiteOption), nameof(CloneRestSiteOption.OnSelect))]
public static class CloneRunRestSitePatch
{
  private static readonly System.Reflection.PropertyInfo OwnerProperty = AccessTools.Property(typeof(RestSiteOption), "Owner");

  [HarmonyPrefix]
  private static bool ApplyCloneHpLoss(CloneRestSiteOption __instance, ref Task<bool> __result)
  {
    Player? player = OwnerProperty.GetValue(__instance) as Player;
    if (player == null)
    {
      return true;
    }

    if (!CloneRunDebuff.IsActiveForPlayer(player))
    {
      return true;
    }

    __result = HandleCloneSelectionWithHpLoss(player);
    return false;
  }

  private static async Task<bool> HandleCloneSelectionWithHpLoss(Player player)
  {
    if (player.Creature.CurrentHp <= 1)
    {
      return false;
    }

    int hpLoss = CloneRunDebuff.CalculateCloneHpLoss(player.Creature.CurrentHp);
    if (hpLoss > 0)
    {
      await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), player.Creature, hpLoss, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }

    if (!player.Creature.IsAlive)
    {
      return false;
    }

    IEnumerable<CardModel> cloneTargets = player.Deck.Cards.Where(card => card.Enchantment is Clone).ToList();
    List<CardPileAddResult> results = new List<CardPileAddResult>();

    foreach (CardModel original in cloneTargets)
    {
      CardModel clonedCard = player.RunState.CloneCard(original);
      results.Add(await CardPileCmd.Add(clonedCard, PileType.Deck));
    }

    CardCmd.PreviewCardPileAdd(results, 1.2f, CardPreviewStyle.MessyLayout);
    return true;
  }
}