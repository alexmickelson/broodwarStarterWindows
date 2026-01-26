using BWAPI.NET;

namespace Shared;



public class MapTools
{
    private Game? Game;
    public bool IsInitialized => Game != null;
    private List<DefenseNode> MapGrid = new List<DefenseNode>();
    public const int NODE_SIZE = 12;
    private TilePosition startLocation = new TilePosition(0, 0);

    public MapTools()
    {
    }

    public void Initialize(Game game)
    {
        Game = game;

        // Initialize MapGrid or other structures as needed
        // Divide the map into square nodes and note the midpoint.
        MapGrid.Clear();

        startLocation = game.Self().GetStartLocation();

        Console.WriteLine($"Initializing MapTools with map size {game.MapWidth()}x{game.MapHeight()}");

        for (int x = 0; x < game.MapWidth(); x += NODE_SIZE)
        {
            for (int y = 0; y < game.MapHeight(); y += NODE_SIZE)
            {
                var midpoint = new TilePosition(
                    x + NODE_SIZE / 2,
                    y + NODE_SIZE / 2);
                var node = new DefenseNode(midpoint);
                var walkableTiles = GetWalkableTileCount(
                    new TilePosition(x, y),
                    new TilePosition(
                        Math.Min(x + NODE_SIZE - 1, game.MapWidth() - 1),
                        Math.Min(y + NODE_SIZE - 1, game.MapHeight() - 1)));
                if (walkableTiles < NODE_SIZE * NODE_SIZE / 1.44)
                {
                    node.IsWalkable = false;
                }
                MapGrid.Add(node);
            }
        }
        var (up, down) = GetCorners(MapGrid.First());
        Console.WriteLine($"Created {MapGrid.Count} defense nodes. Example node corners: {up.X},{up.Y} to {down.X},{down.Y}");
    }

    public int GetWalkableTileCount(TilePosition topLeft, TilePosition bottomRight)
    {
        if (Game == null)
            return 0;

        int walkableCount = 0;
        for (int x = topLeft.X; x <= bottomRight.X; x++)
        {
            for (int y = topLeft.Y; y <= bottomRight.Y; y++)
            {
                var tilePos = new TilePosition(x, y);
                bool hasResource = Game.GetUnitsOnTile(tilePos)
                    .Any(u => u.GetUnitType() == UnitType.Resource_Mineral_Field ||
                              u.GetUnitType() == UnitType.Resource_Vespene_Geyser);
                if (Game.IsWalkable(tilePos.ToWalkPosition()) && !hasResource)
                {
                    walkableCount++;
                }
            }
        }
        return walkableCount;
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
                (node.Position.X * 32) - NODE_SIZE / 2 * 32,
                (node.Position.Y * 32) - NODE_SIZE / 2 * 32);

        var lowerCorner = new Position(
            (node.Position.X * 32) + NODE_SIZE / 2 * 32,
            (node.Position.Y * 32) + NODE_SIZE / 2 * 32);
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

    public DefenseNode GetDefenseNodeAtTilePosition(TilePosition tilePosition)
    {
        if (!IsInitialized)
            return new DefenseNode(tilePosition);

        // Find the closest node to the given tile position using LINQ
        var closestNode = MapGrid
            .MinBy(node =>
                Math.Pow(node.Position.X - tilePosition.X, 2) +
                Math.Pow(node.Position.Y - tilePosition.Y, 2));

        return closestNode ?? new DefenseNode(tilePosition);
    }

    public DefenseNode MoveTowardsCenter(DefenseNode node)
    {
        if (!IsInitialized)
            return node;

        var mapCenter = new TilePosition(Game.MapWidth() / 2, Game.MapHeight() / 2);

        var direction = new TilePosition(
            (mapCenter.X - node.Position.X) / 10,
            (mapCenter.Y - node.Position.Y) / 10);

        var newPosition = new TilePosition(
            node.Position.X + direction.X,
            node.Position.Y + direction.Y);

        return GetDefenseNodeAtTilePosition(newPosition);
    }

    public DefenseNode GetEmptyDefenseNodeNextToPopulatedGrid()
    {
        if (!IsInitialized || Game == null)
            return new DefenseNode(new TilePosition(0, 0));

        foreach (var node in MapGrid.Where(n => n.IsWalkable))
        {
            node.Cannons = Game.Self().GetUnits()
                .Where(u => u.GetUnitType() == UnitType.Protoss_Photon_Cannon)
                .Count(u =>
                {
                    var unitTilePos = u.GetTilePosition();
                    return Math.Abs(unitTilePos.X - node.Position.X) <= NODE_SIZE / 2 &&
                           Math.Abs(unitTilePos.Y - node.Position.Y) <= NODE_SIZE / 2;
                });
            node.Pylons = Game.Self().GetUnits()
                .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon)
                .Count(u =>
                {
                    var unitTilePos = u.GetTilePosition();
                    return Math.Abs(unitTilePos.X - node.Position.X) <= NODE_SIZE / 2 &&
                           Math.Abs(unitTilePos.Y - node.Position.Y) <= NODE_SIZE / 2;
                });

            if (node.Pylons == 0)
            {
                // Check neighboring nodes for population
                var neighbors = MapGrid.Where(n =>
                    Math.Abs(n.Position.X - node.Position.X) <= NODE_SIZE &&
                    Math.Abs(n.Position.Y - node.Position.Y) <= NODE_SIZE &&
                    n != node);

                node.HasPopulatedNeighbor = neighbors.Any(n => n.Pylons > 0);
            }
        }

        // Perform BFS to find all walkable nodes reachable from populated nodes
        var populatedNodes = MapGrid.Where(n => n.Pylons > 0 && n.IsWalkable);
        HashSet<DefenseNode> reachable = new HashSet<DefenseNode>();
        Queue<DefenseNode> queue = new Queue<DefenseNode>();
        foreach (var p in populatedNodes)
        {
            queue.Enqueue(p);
            reachable.Add(p);
        }
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var neighbors = MapGrid.Where(n => n.IsWalkable &&
                Math.Abs(n.Position.X - current.Position.X) <= NODE_SIZE &&
                Math.Abs(n.Position.Y - current.Position.Y) <= NODE_SIZE &&
                n != current);
            foreach (var neigh in neighbors)
            {
                if (!reachable.Contains(neigh))
                {
                    reachable.Add(neigh);
                    queue.Enqueue(neigh);
                }
            }
        }

        // Get empty nodes next to populated ones that are reachable, sorted by distance to start location
        var emptyNodesWithNeighbors = MapGrid
            .Where(n => n.Pylons == 0 && n.HasPopulatedNeighbor && reachable.Contains(n))
            .OrderBy(n =>
                Math.Pow(n.Position.X - startLocation.X, 2) +
                Math.Pow(n.Position.Y - startLocation.Y, 2));

        var bestNode = emptyNodesWithNeighbors.FirstOrDefault();
        if (bestNode != null)
            return bestNode;

        // Fallback: return the closest empty reachable node to start location
        return MapGrid
            .Where(n => n.Pylons == 0 && reachable.Contains(n))
            .MinBy(n =>
                Math.Pow(n.Position.X - startLocation.X, 2) +
                Math.Pow(n.Position.Y - startLocation.Y, 2))
            ?? new DefenseNode(new TilePosition(0, 0));
    }
}
