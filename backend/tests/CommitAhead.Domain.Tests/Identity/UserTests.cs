using CommitAhead.Domain.Identity;

namespace CommitAhead.Domain.Tests.Identity;

public class UserTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_WithValidArguments_CreatesEnabledUser()
    {
        var user = new User(Guid.NewGuid(), "supabase-sub-123", "owner@example.com", Now);

        Assert.True(user.IsEnabled);
        Assert.Equal("supabase-sub-123", user.SupabaseUserId);
        Assert.Equal("owner@example.com", user.Email);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new User(Guid.Empty, "sub", "owner@example.com", Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithoutSupabaseUserId_Throws(string? supabaseUserId)
    {
        Assert.Throws<ArgumentException>(() => new User(Guid.NewGuid(), supabaseUserId!, "owner@example.com", Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithoutEmail_Throws(string? email)
    {
        Assert.Throws<ArgumentException>(() => new User(Guid.NewGuid(), "sub", email!, Now));
    }

    [Fact]
    public void Disable_SetsIsEnabledToFalse()
    {
        var user = new User(Guid.NewGuid(), "sub", "owner@example.com", Now);

        user.Disable();

        Assert.False(user.IsEnabled);
    }

    [Fact]
    public void Constructor_NormalizesEmail_TrimAndLowercase()
    {
        var user = new User(Guid.NewGuid(), "sub", "  Owner@Example.COM  ", Now);

        Assert.Equal("owner@example.com", user.Email);
    }

    [Theory]
    [InlineData("Owner@Example.com", "owner@example.com")]
    [InlineData("  spaced@example.com  ", "spaced@example.com")]
    public void Normalize_TrimsAndLowercases(string input, string expected)
    {
        Assert.Equal(expected, User.Normalize(input));
    }
}
