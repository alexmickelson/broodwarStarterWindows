using BWAPI.NET;

namespace Shared;



public class MapTools
{
    private Game? Game;
    public bool IsInitialized => Game != null;
    private List<DefenseNode> MapGrid = new List<DefenseNode>();
    const int nodeSize = 12;

    public MapTools()
    {
    }

    public void Initialize(Game game)
    {
        Game = game;

        // Initialize MapGrid or other structures as needed
        // Divide the map into square nodes and note the midpoint.
        MapGrid.Clear();

        Console.WriteLine($"Initializing MapTools with map size {game.MapWidth()}x{game.MapHeight()}");

        for (int x = 0; x < game.MapWidth(); x += nodeSize)
        {
            for (int y = 0; y < game.MapHeight(); y += nodeSize)
            {
                var midpoint = new TilePosition(
                    x + nodeSize / 2,
                    y + nodeSize / 2);
                var node = new DefenseNode(midpoint);
                MapGrid.Add(node);
            }
        }
            var (up, down) = GetCorners(MapGrid.First());
        Console.WriteLine($"Created {MapGrid.Count} defense nodes. Example node corners: {up.X},{up.Y} to {down.X},{down.Y}");
    }

    public void DrawGrid()
    {
        foreach (var node in MapGrid)
        {
            var (upperCorner, lowerCorner) = GetCorners(node);
            Game?.DrawBoxMap(upperCorner, lowerCorner, Color.Blue);
        }
    }
    private (Position upperCorner, Position lowerCorner) GetCorners(DefenseNode node)
    {
        var upperCorner = new Position(
                (node.Position.X*32) - nodeSize / 2 * 32,
                (node.Position.Y*32) - nodeSize / 2 * 32);

        var lowerCorner = new Position(
            (node.Position.X*32) + nodeSize / 2 * 32,
            (node.Position.Y*32) + nodeSize / 2 * 32);
        return (upperCorner, lowerCorner);
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
