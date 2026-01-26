using BWAPI.NET;

namespace Shared;

class DefenseNode
{
    public TilePosition Position;
    public int Cannons;

    public DefenseNode(TilePosition position)
    {
        Position = position;
        Cannons = 0;
    }
}