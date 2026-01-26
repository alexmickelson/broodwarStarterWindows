using BWAPI.NET;

namespace Shared;

public class DefenseNode
{
    public TilePosition Position;
    public int Cannons;
    public int Pylons;
    public bool HasPopulatedNeighbor;
    public bool IsWalkable;
    public bool IsDud = false;

    public DefenseNode(TilePosition position, bool walkable = true)
    {
        Position = position;
        Cannons = 0;
        Pylons = 0;
        HasPopulatedNeighbor = false;
        IsWalkable = walkable;
    }
}