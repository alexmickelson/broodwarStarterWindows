using BWAPI.NET;

namespace Shared;

public class TileData
{
    public TileData(int x, int y)
    {
        X = x;
        Y = y;
    }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsWalkable { get; set; }
    public bool IsBuildable { get; set; }
    public bool IsInBase { get; set; }
    public bool IsChokepoint { get; set; }
    public bool HasMinerals { get; set; }
    public bool HasGas { get; set; }
    public bool CantBuildDepot { get; set; }
}

public class MapTools
{
    private TileData[,]? _tiles;
    private int _width;
    private int _height;
    private Game? _game;

    private List<List<TileData>> chokepointTiles = 
        new List<List<TileData>>();

    // Shared color definitions for tiles
    private static readonly Color ColorResources = new Color(0, 200, 255);
    private static readonly Color ColorBuildable = new Color(0, 255, 0);
    private static readonly Color ColorDepotRestricted = new Color(255, 165, 0);
    private static readonly Color ColorWalkableOnly = new Color(255, 255, 0);
    private static readonly Color ColorBlocked = new Color(255, 0, 0);
    private static readonly Color ColorChokepoint = new Color(200, 200, 200);

    public bool Initilized { get; private set; } = false;

    public void Initialize(Game game)
    {
        _game = game;
        _width = game.MapWidth();
        _height = game.MapHeight();
        _tiles = new TileData[_width, _height];

        // Initialize all tiles
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _tiles[x, y] = new TileData(x, y);
            }
        }

        AnalyzeMap();
        DeepAnalysis();
        Initilized = true;
    }

    private void AnalyzeMap()
    {
        if (_game == null || _tiles == null)
            return;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                TilePosition tilePos = new TilePosition(x, y);
                var tile = _tiles[x, y];

                if (tile.X != x || tile.Y != y)
                    throw new Exception("TileData coordinates mismatch!");

                // Basic properties
                tile.IsBuildable = _game.IsBuildable(tilePos);
                tile.IsWalkable = _game.IsWalkable(tilePos.ToWalkPosition());

                // Check for resources
                tile.HasMinerals = _game.GetStaticMinerals()
                    .Any(m => m.GetTilePosition() == tilePos);
                tile.HasGas = _game.GetStaticGeysers()
                    .Any(g => g.GetTilePosition() == tilePos);


                // Region/chokepoint analysis can be added later
                tile.CantBuildDepot = false;
                tile.IsChokepoint = false;
                tile.IsInBase = false;
            }
        }
    }

    private void DeepAnalysis()
    {
        foreach(var tile in _tiles!)
        {
            if(ThereIsAResourceWithinRadius(tile, 3))
            {
                tile.CantBuildDepot = true;
            }
            if (!tile.IsChokepoint && IsAChokepointTile(tile))
            {
                var thisSetOfChokeTiles = new List<TileData>();
                FindAllChokepointTiles(tile, thisSetOfChokeTiles);
                
                // Only consider it a chokepoint if it has at least 6 tiles
                if (thisSetOfChokeTiles.Count >= 6)
                {
                    chokepointTiles.Add(thisSetOfChokeTiles);
                }
                else
                {
                    // Not a real chokepoint, unmark the tiles
                    foreach (var t in thisSetOfChokeTiles)
                    {
                        t.IsChokepoint = false;
                    }
                }
            }
        }
    }

    private bool ThereIsAResourceWithinRadius(TileData tile, int radius)
    {
        if (tile.HasMinerals || tile.HasGas)
            return true;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int checkX = tile.X + dx;
                int checkY = tile.Y + dy;
                TileData? checkTile = GetTile(checkX, checkY);
                if (checkTile != null && (checkTile.HasMinerals || checkTile.HasGas))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsAChokepointTile(TileData tile)
    {
        if (!tile.IsWalkable)
            return false;

        // Count walkable neighbors in each direction
        int walkableNeighbors = 0;
        int unwalkableNeighbors = 0;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                TileData? neighbor = GetTile(tile.X + dx, tile.Y + dy);
                if (neighbor != null && neighbor.IsWalkable)
                {
                    walkableNeighbors++;
                }
                else
                {
                    unwalkableNeighbors++;
                }
            }
        }
        
        // Chokepoint: walkable tile with some walkable neighbors (corridor)
        // but also bordered by unwalkable tiles (narrow passage)
        // Must have at least 2 walkable neighbors (to form a corridor)
        // and at least 3 unwalkable neighbors (to be narrow)
        return walkableNeighbors >= 2 && walkableNeighbors <= 5 && unwalkableNeighbors >= 3;
    }

    private void FindAllChokepointTiles(TileData startTile, List<TileData> collectedTiles)
    {
        Queue<TileData> toVisit = new Queue<TileData>();
        HashSet<TileData> visited = new HashSet<TileData>();
        toVisit.Enqueue(startTile);
        visited.Add(startTile); 

        while (toVisit.Count > 0)
        {
            TileData current = toVisit.Dequeue();
            collectedTiles.Add(current);
            current.IsChokepoint = true;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    TileData? neighbor = GetTile(current.X + dx, current.Y + dy);
                    if (neighbor != null && !visited.Contains(neighbor) && IsAChokepointTile(neighbor))
                    {
                        toVisit.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }
        }
    }

    public TileData? GetTile(int x, int y)
    {
        if (_tiles == null || x < 0 || x >= _width || y < 0 || y >= _height)
            return null;
        return _tiles[x, y];
    }

    public TileData? GetTile(TilePosition pos)
    {
        return GetTile(pos.x, pos.y);
    }

public void DrawGrid(Game game)
{
    for (int x = 0; x < _width; x++)
    {
        for (int y = 0; y < _height; y++)
        {
            DrawTile(game, x, y);
        }
    }
    
    // Draw legend
    DrawLegend(game);
}

private void DrawLegend(Game game)
{
    int legendX = 10;
    int legendY = 150;
    int boxSize = 16;
    int spacing = 20;
    
    // Define legend entries (color, label)
    var legendEntries = new[]
    {
        (ColorResources, "Resources"),
        (ColorBuildable, "Buildable"),
        (ColorDepotRestricted, "Depot restricted"),
        (ColorWalkableOnly, "Walkable only"),
        (ColorBlocked, "Blocked"),
        (ColorChokepoint, "Chokepoint")
    };
    
    int legendHeight = 15 + legendEntries.Length * spacing + 5;
    
    // Background for legend
    game.DrawBox(CoordinateType.Screen, legendX - 5, legendY - 5, 
                 legendX + 200, legendY + legendHeight, 
                 new Color(0, 0, 0), true);
    game.DrawBox(CoordinateType.Screen, legendX - 5, legendY - 5, 
                 legendX + 200, legendY + legendHeight, 
                 new Color(255, 255, 255), false);
    
    // Title
    game.DrawTextScreen(legendX, legendY, "Map Legend:");
    legendY += 15;
    
    // Draw each legend entry
    foreach (var (color, label) in legendEntries)
    {
        game.DrawBox(CoordinateType.Screen, legendX, legendY, 
                     legendX + boxSize, legendY + boxSize, 
                     color, true);
        game.DrawTextScreen(legendX + boxSize + 5, legendY + 3, label);
        legendY += spacing;
    }
}

    public void DrawTile(Game game, int tileX, int tileY)
    {
        TileData? tile = GetTile(tileX, tileY);
        if (tile == null)
            return;
        const int padding = 2;
        const int d = 32 - 2 * padding;

        int px = tileX * 32 + padding;
        int py = tileY * 32 + padding;

        Color color = ColorBlocked; // Default for unwalkable/unbuildable
        
        if (tile.IsChokepoint)
        {
            color = ColorChokepoint;
            if (!tile.IsWalkable){
                Console.WriteLine($"Error: Chokepoint tile is not walkable! {tile.X}, {tile.Y}");
            }
        }
        // Set color based on tile properties
        else if (tile.HasMinerals || tile.HasGas)
        {
            color = ColorResources;
        }
        else if (tile.IsBuildable && tile.CantBuildDepot)
        {
            color = ColorDepotRestricted; // Can build but not depot
        }
        else if (tile.IsBuildable)
        {
            color = ColorBuildable; // Can build including depot
        }
        else if (tile.IsWalkable)
        {
            color = ColorWalkableOnly;
        }

        game.DrawBox(CoordinateType.Map, px, py, px + d, py + d, color, false);
    }

    public int Width => _width;
    public int Height => _height;
}