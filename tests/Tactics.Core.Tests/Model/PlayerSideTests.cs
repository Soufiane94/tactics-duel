using Tactics.Core.Model;

namespace Tactics.Core.Tests.Model;

public sealed class PlayerSideTests
{
    [Fact]
    public void Opponent_switches_sides()
    {
        Assert.Equal(PlayerSide.Two, PlayerSide.One.Opponent());
        Assert.Equal(PlayerSide.One, PlayerSide.Two.Opponent());
    }

    [Fact]
    public void Opponent_of_an_undefined_value_throws()
    {
        // C# enums can hold any int; a corrupted value must fail loudly, not pick a side.
        Assert.Throws<ArgumentOutOfRangeException>(() => ((PlayerSide)42).Opponent());
    }
}
