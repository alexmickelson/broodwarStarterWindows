using BWAPI.NET;

namespace Shared;


public class Tools
{
    public static int GetUnitCount(Game game, UnitType unitType, bool includeIncomplete = false)
    {
        if (game == null) return 0;
        if (includeIncomplete && unitType.IsBuilding())
        {
            var builder = game.Self().GetUnits().Where(u => u.GetUnitType().IsWorker() &&
                u.IsConstructing() && u.GetBuildType() == unitType);
            return GetUnits(game, unitType, true).Count + builder.Count();
        }
        return game.Self().GetUnits().Count(u => u.GetUnitType() == unitType
            && (includeIncomplete || u.IsCompleted()));
    }

    public static List<Unit> GetUnits(Game game, UnitType unitType, bool includeIncomplete = false)
    {
        if (game == null) return new List<Unit>();
        if (includeIncomplete && unitType.IsBuilding())
        {
            return game.Self().GetUnits().Where(u => u.GetUnitType() == unitType).ToList();
        }
        return game.Self().GetUnits().Where(u => u.GetUnitType() == unitType
            && (includeIncomplete || u.IsCompleted())).ToList();
    }

    public static bool NexusIsTrainingProbes(Game game)
    {
        if (game == null) return false;
        var nexus = game.Self().GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Nexus);
        if (nexus != null)
        {
            return nexus.IsTraining();
        }
        return false;
    }

    public static bool CanAfford(Game game, UnitType unitType)
    {
        if (game == null) return false;
        return unitType.MineralPrice() <= game.Self().Minerals()
            && unitType.GasPrice() <= game.Self().Gas();
    }

    public static TilePosition GetBuildLocationTowardBaseAccess(Game game, MapTools mapTools, UnitType unitType)
    {
        if (game == null) return new TilePosition(0, 0);
        var location = mapTools.GetBuildLocationTowardsBaseAccess(
                            game.Self().GetStartLocation());
        var buildLocation = game.GetBuildLocation(
            unitType, location, 5, false);
        return buildLocation;
    }

    public static List<Unit> GetPylons(Game game, bool includeIncomplete = false)
    {
        if (game == null) return new List<Unit>();
        var pylonsBuiltOrInProgress = game.Self().GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon
            && (includeIncomplete || u.IsCompleted()));
        return pylonsBuiltOrInProgress.ToList();
    }

    public static Unit GetBuilder(Game game, int buildId)
    {
        if (game == null) return null;
        var builder = game.Self().GetUnits()
            .FirstOrDefault(u => u.GetID() == buildId);
        return builder;
    }

    public static TilePosition GetBuildLocationByPylon(Game game, UnitType unitType, Unit pylon)
    {
        if (game == null) return new TilePosition(0, 0);
        if (pylon.GetUnitType() != UnitType.Protoss_Pylon || !pylon.IsCompleted())
            return new TilePosition(0, 0);

        var buildLocation = game.GetBuildLocation(unitType, pylon.GetTilePosition(), 6, false);
        return buildLocation;


    }

    public static void BuildProbe(Game game, Unit nexus)
    {
        if (game == null || nexus == null) return;
        if (CanAfford(game, UnitType.Protoss_Probe) && !NexusIsTrainingProbes(game))
        {
            nexus.Train(UnitType.Protoss_Probe);
        }
    }
}