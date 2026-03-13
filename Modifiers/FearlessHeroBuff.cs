using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ModTemplate.Modifiers;

public class FearlessHeroBuff : ModifierModel
{
  private const int DefaultWindowUnknownRooms = 13;

  private const float MonsterWeight = 0.38f;
  private const float EliteWeight = 0.12f;
  private const float ShopWeight = 0.10f;
  private const float RestSiteWeight = 0.08f;
  private const float TreasureWeight = 0.02f;
  private const float EventWeight = 0.30f;

  public static bool IsActive(IRunState runState)
  {
    return runState.Modifiers.Any(modifier => modifier.GetType() == typeof(FearlessHeroBuff));
  }

  public static bool TryRollUnknownRoom(IRunState runState, IEnumerable<RoomType> blacklist, out RoomType roomType)
  {
    roomType = RoomType.Unassigned;

    if (!IsActive(runState))
    {
      return false;
    }

    HashSet<RoomType> blocked = blacklist.ToHashSet();

    if (ShouldForceShop(runState, blocked))
    {
      roomType = RoomType.Shop;
      return true;
    }

    if (ShouldForceCombat(runState, blocked))
    {
      roomType = RollCombatRoom(runState.Rng.UnknownMapPoint, blocked);
      return true;
    }

    List<(RoomType roomType, float weight)> weightedChoices = new List<(RoomType roomType, float weight)>();

    TryAddWeight(weightedChoices, blocked, RoomType.Monster, MonsterWeight);
    TryAddWeight(weightedChoices, blocked, RoomType.Elite, EliteWeight);
    TryAddWeight(weightedChoices, blocked, RoomType.Shop, ShopWeight);
    TryAddWeight(weightedChoices, blocked, RoomType.RestSite, RestSiteWeight);
    TryAddWeight(weightedChoices, blocked, RoomType.Treasure, TreasureWeight);
    TryAddWeight(weightedChoices, blocked, RoomType.Event, EventWeight);

    if (TryRollWeightedRoom(runState, weightedChoices, out roomType))
    {
      return true;
    }

    if (!blocked.Contains(RoomType.Event))
    {
      roomType = RoomType.Event;
      return true;
    }

    if (!blocked.Contains(RoomType.Monster))
    {
      roomType = RoomType.Monster;
      return true;
    }

    if (!blocked.Contains(RoomType.RestSite))
    {
      roomType = RoomType.RestSite;
      return true;
    }

    roomType = RoomType.Monster;
    return true;
  }

  private static bool ShouldForceCombat(IRunState runState, IReadOnlySet<RoomType> blocked)
  {
    if (blocked.Contains(RoomType.Monster) && blocked.Contains(RoomType.Elite))
    {
      return false;
    }

    int unknownWindowRooms = GetUnknownWindowRoomCount(runState);
    int targetCombatRooms = GetTargetCombatRooms(unknownWindowRooms);

    GetUnknownWindowStats(runState, unknownWindowRooms, out int unknownCount, out int combatCount, out _);
    if (unknownCount >= unknownWindowRooms)
    {
      return false;
    }

    int remainingRooms = unknownWindowRooms - unknownCount;
    int combatNeeded = targetCombatRooms - combatCount;
    return combatNeeded > 0 && remainingRooms <= combatNeeded;
  }

  private static bool ShouldForceShop(IRunState runState, IReadOnlySet<RoomType> blocked)
  {
    if (blocked.Contains(RoomType.Shop))
    {
      return false;
    }

    int unknownWindowRooms = GetUnknownWindowRoomCount(runState);
    int targetCombatRooms = GetTargetCombatRooms(unknownWindowRooms);
    int targetShopRooms = GetTargetShopRooms(unknownWindowRooms);

    GetUnknownWindowStats(runState, unknownWindowRooms, out int unknownCount, out int combatCount, out int shopCount);
    if (unknownCount >= unknownWindowRooms)
    {
      return false;
    }

    int remainingRooms = unknownWindowRooms - unknownCount;
    int shopNeeded = targetShopRooms - shopCount;
    if (shopNeeded <= 0)
    {
      return false;
    }

    int combatNeeded = targetCombatRooms - combatCount;
    bool mustSpendCurrentRollOnCombat = combatNeeded > 0 && remainingRooms <= combatNeeded;
    if (mustSpendCurrentRollOnCombat)
    {
      return false;
    }

    return remainingRooms <= shopNeeded;
  }

  private static RoomType RollCombatRoom(MegaCrit.Sts2.Core.Random.Rng rng, IReadOnlySet<RoomType> blocked)
  {
    bool monsterAllowed = !blocked.Contains(RoomType.Monster);
    bool eliteAllowed = !blocked.Contains(RoomType.Elite);

    if (monsterAllowed && eliteAllowed)
    {
      return rng.NextFloat() <= 0.78f ? RoomType.Monster : RoomType.Elite;
    }

    if (monsterAllowed)
    {
      return RoomType.Monster;
    }

    return RoomType.Elite;
  }

  private static int GetUnknownWindowRoomCount(IRunState runState)
  {
    int roomRows = runState.Map
      .GetAllMapPoints()
      .Where(point => point.CanBeModified)
      .Select(point => point.coord.row)
      .Distinct()
      .Count();

    return Math.Max(1, roomRows > 0 ? roomRows : DefaultWindowUnknownRooms);
  }

  private static int GetTargetCombatRooms(int unknownWindowRooms)
  {
    return Math.Max(1, (int)Math.Round(unknownWindowRooms * 0.5f, MidpointRounding.AwayFromZero));
  }

  private static int GetTargetShopRooms(int unknownWindowRooms)
  {
    return Math.Max(1, (int)Math.Round(unknownWindowRooms / 12f, MidpointRounding.AwayFromZero));
  }

  private static void GetUnknownWindowStats(IRunState runState, int unknownWindowRooms, out int unknownCount, out int combatCount, out int shopCount)
  {
    IReadOnlyList<MapPointHistoryEntry> currentActHistory = runState.MapPointHistory.ElementAtOrDefault(runState.CurrentActIndex)
      ?? Array.Empty<MapPointHistoryEntry>();

    IEnumerable<MapPointHistoryEntry> unknownEntries = currentActHistory
      .Where(entry => entry.MapPointType == MapPointType.Unknown)
      .Take(unknownWindowRooms);

    unknownCount = 0;
    combatCount = 0;
    shopCount = 0;

    foreach (MapPointHistoryEntry entry in unknownEntries)
    {
      unknownCount++;
      if (entry.HasRoomOfType(RoomType.Monster) || entry.HasRoomOfType(RoomType.Elite))
      {
        combatCount++;
      }

      if (entry.HasRoomOfType(RoomType.Shop))
      {
        shopCount++;
      }
    }
  }

  private static void TryAddWeight(List<(RoomType roomType, float weight)> weightedChoices, IReadOnlySet<RoomType> blocked, RoomType roomType, float weight)
  {
    if (weight <= 0f || blocked.Contains(roomType))
    {
      return;
    }

    weightedChoices.Add((roomType, weight));
  }

  private static bool TryRollWeightedRoom(IRunState runState, List<(RoomType roomType, float weight)> weightedChoices, out RoomType roomType)
  {
    roomType = RoomType.Unassigned;
    if (weightedChoices.Count == 0)
    {
      return false;
    }

    float totalWeight = weightedChoices.Sum(choice => choice.weight);
    if (totalWeight <= 0f)
    {
      return false;
    }

    float roll = runState.Rng.UnknownMapPoint.NextFloat() * totalWeight;
    float cursor = 0f;
    foreach ((RoomType candidateType, float weight) in weightedChoices)
    {
      cursor += weight;
      if (roll <= cursor)
      {
        roomType = candidateType;
        return true;
      }
    }

    roomType = weightedChoices[^1].roomType;
    return true;
  }

  public override ActMap ModifyGeneratedMap(IRunState runState, ActMap map, int actIndex)
  {
    foreach (MapPoint point in map.GetAllMapPoints())
    {
      if (!point.CanBeModified)
      {
        continue;
      }

      switch (point.PointType)
      {
        case MapPointType.Boss:
        case MapPointType.Ancient:
        case MapPointType.Unassigned:
          continue;
        default:
          point.PointType = MapPointType.Unknown;
          break;
      }
    }

    return map;
  }
}
