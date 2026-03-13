using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using ModTemplate.Config;

namespace ModTemplate.Modifiers;

public class MultiSmithBuff : ModifierModel
{
  public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
  {
    int smithCount = Math.Max(1, MoreCustomsConfig.Current.RestSiteSmithCount);
    List<SmithRestSiteOption> smithOptions = options.OfType<SmithRestSiteOption>().ToList();

    foreach (SmithRestSiteOption smithOption in smithOptions)
    {
      smithOption.SmithCount = smithCount;
    }

    return true;
  }
}
