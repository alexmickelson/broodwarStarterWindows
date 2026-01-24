using BWAPI.NET;
using Shared;
using Xunit;

namespace Tests;

public class UnitTest1
{
    [Theory]
    [InlineData(0, 0, 10, 0, 5, 5, 0)]
    [InlineData(5, 5, 2, 5, 2, 3, 5)]
    [InlineData(2, 2, 12, 2, 5, 7, 2)]
    public void GetPositionToward_MovesGivenDistanceAlongAxis(int fromX, int fromY, int toX, int toY, int distance, int expectedX, int expectedY)
    {   
        var from = new TilePosition(fromX, fromY);
        var to = new TilePosition(toX, toY);

        var result = MyStarcraftBot.GetPositionToward(from, to, distance);

        Assert.Equal(expectedX, result.X);
        Assert.Equal(expectedY, result.Y);
    }

}
