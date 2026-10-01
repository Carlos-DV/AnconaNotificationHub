using Domain;

namespace Application.UnitTests.Domain;

public class GroupNameTests
{
    [Theory]
    [InlineData("ancona", "user", "38", "ancona:user:38")]
    [InlineData("Ancona", "TOPIC", "Warranty.Returns", "ancona:topic:warranty.returns")]
    [InlineData("ancona", "perm", "Permissions.Warranty.View", "ancona:perm:permissions.warranty.view")]
    [InlineData("ancona", "entity", "warranty.return:0199-abc", "ancona:entity:warranty.return:0199-abc")]
    [InlineData("ancona", "all", null, "ancona:all")]
    [InlineData("ancona", "all", "ignored", "ancona:all")]
    public void TryCreate_builds_normalized_name(string tenant, string type, string? value, string expected)
    {
        Assert.True(GroupName.TryCreate(tenant, type, value, out var group));
        Assert.Equal(expected, group!.Value);
    }

    [Theory]
    [InlineData("", "user", "38")]
    [InlineData("an:cona", "user", "38")]
    [InlineData("ancona", "", "38")]
    [InlineData("ancona", "us3r", "38")]
    [InlineData("ancona", "user", "")]
    [InlineData("ancona", "user", "3 8")]
    [InlineData("ancona", "topic", "warranty/returns")]
    public void TryCreate_rejects_invalid_parts(string tenant, string type, string value)
    {
        Assert.False(GroupName.TryCreate(tenant, type, value, out var group));
        Assert.Null(group);
    }

    [Fact]
    public void TryCreate_rejects_value_longer_than_limit()
    {
        var value = new string('a', GroupName.MaxValueLength + 1);
        Assert.False(GroupName.TryCreate("ancona", "topic", value, out _));
    }

    [Fact]
    public void Groups_with_same_value_are_equal()
    {
        GroupName.TryCreate("ancona", "branch", "001", out var a);
        GroupName.TryCreate("ANCONA", "branch", "001", out var b);
        Assert.Equal(a, b);
    }
}
