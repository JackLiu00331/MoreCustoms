using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(SmithRestSiteOption), nameof(SmithRestSiteOption.OnSelect))]
public static class SmithRestSiteOptionPatch
{
  [HarmonyPrefix]
  private static bool AllowSelectingUpToSmithCount(SmithRestSiteOption __instance, ref Task<bool> __result)
  {
    __result = OnSelectAllowUpToAsync(__instance);
    return false;
  }

  private static async Task<bool> OnSelectAllowUpToAsync(SmithRestSiteOption smithOption)
  {
    Player owner = Traverse.Create(smithOption).Property("Owner").GetValue<Player>();

    int maxSelect = smithOption.SmithCount;
    if (maxSelect < 1)
    {
      maxSelect = 1;
    }

    CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, maxSelect)
    {
      Cancelable = true,
      RequireManualConfirmation = true
    };

    List<CardModel> selection = (await CardSelectCmd.FromDeckGeneric(owner, prefs, card => card.IsUpgradable)).ToList();
    Traverse.Create(smithOption).Field("_selection").SetValue(selection);

    if (selection.Count == 0)
    {
      return false;
    }

    foreach (CardModel card in selection)
    {
      CardCmd.Upgrade(card, CardPreviewStyle.None);
    }

    await Hook.AfterRestSiteSmith(owner.RunState, owner);
    return true;
  }
}
