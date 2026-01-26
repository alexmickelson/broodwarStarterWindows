using BWAPI.NET;

namespace Shared;


public class MapTools
{
    private Game? Game;
    public bool IsInitialized => Game != null;


    public MapTools()
    {
    }

    public void Initialize(Game game)
    {
        Game = game;
    }
 
    public TilePosition GetBuildLocationTowardsBaseAccess(TilePosition baseLocation)
    {
        if (!IsInitialized)
            return baseLocation;

        // Simple example: just return a position offset towards the center of the map
        var mapCenter = new TilePosition(Game.MapWidth() / 2, Game.MapHeight() / 2);

        var direction = new TilePosition(
            (mapCenter.X - baseLocation.X) / 10,
            (mapCenter.Y - baseLocation.Y) / 10);

        var buildPos = new TilePosition(
            baseLocation.X + direction.X,
            baseLocation.Y + direction.Y);

        return buildPos;
    }

   
}

