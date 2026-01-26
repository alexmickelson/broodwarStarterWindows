using BWAPI.NET;

namespace Shared;

public class BuildLocation
{
    /// <summary>
    /// Smart building placement that auto-detects race requirements.
    /// Uses custom logic for Protoss (pylon power), falls back to library for Terran/Zerg.
    /// </summary>
    public static TilePosition Get(Game game, UnitType unitType, TilePosition seedPosition, 
        int maxRange)
    {
        if (game == null) return new TilePosition(0, 0);
        
        var creep = unitType.RequiresCreep();
        
        // For Protoss buildings requiring pylon power, use custom implementation
        if (unitType.RequiresPsi())
        {
            return FindProtossBuildLocation(game, unitType, seedPosition, maxRange);
        }
        
        // For Terran, Zerg, and Protoss buildings without power requirements
        return game.GetBuildLocation(unitType, seedPosition, maxRange, creep);
    }
    
    /// <summary>
    /// Custom Protoss building placement with pylon power validation.
    /// Uses spiral search to find valid buildable location within pylon range.
    /// </summary>
    private static TilePosition FindProtossBuildLocation(
        Game game,
        UnitType unitType,
        TilePosition seedPosition,
        int maxRange)
    {
        if (!unitType.RequiresPsi())
            return seedPosition;
        
        int buildWidth = unitType.TileWidth();
        int buildHeight = unitType.TileHeight();
        
        // Spiral search pattern from seed position
        for (int radius = 0; radius <= maxRange; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    // Only check perimeter of current radius (optimization)
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                        continue;
                    
                    var testPos = new TilePosition(
                        seedPosition.X + dx,
                        seedPosition.Y + dy);
                    
                    // Check if position is valid and buildable
                    if (IsValidBuildLocation(game, unitType, testPos, buildWidth, buildHeight))
                    {
                        return testPos;
                    }
                }
            }
        }
        
        // Fallback to seed position if nothing found
        return seedPosition;
    }
    
    /// <summary>
    /// Validates if a tile position is suitable for Protoss building placement.
    /// </summary>
    private static bool IsValidBuildLocation(
        Game game,
        UnitType unitType,
        TilePosition position,
        int buildWidth,
        int buildHeight)
    {
        // Check map bounds
        if (position.X < 0 || position.Y < 0 ||
            position.X + buildWidth > game.MapWidth() ||
            position.Y + buildHeight > game.MapHeight())
        {
            return false;
        }
        
        // Check if we can build here (terrain, existing buildings, etc.)
        if (!game.CanBuildHere(position, unitType))
        {
            return false;
        }
        
        // For Protoss buildings requiring power, verify pylon coverage
        if (unitType.RequiresPsi())
        {
            if (!game.HasPower(position, buildWidth, buildHeight))
            {
                return false;
            }
        }
        
        return true;
    }
}