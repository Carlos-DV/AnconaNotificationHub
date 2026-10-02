using Domain;

namespace Application.UnitTests.Domain;

public class GroupNameTests
{
    [Theory]
    [InlineData("ancona", "user", "38", "ancona:user:38")]
    [InlineData("Ancona", "TOPIC", "Warranty.Returns", "ancona:topic:warranty.returns")]
    [InlineData("ancona", "perm", "Permissions.Warranty.View", "ancona:perm:permissions.warranty.view")]
    [InlineData("ancona", "perm", "Permission.Ajuste de inventario.View", "ancona:perm:permission.ajuste-de-inventario.view")]
    [InlineData("ancona", "perm", "Permission.Auditorías.View", "ancona:perm:permission.auditorias.view")]
    [InlineData("ancona", "perm", " Permission.Diseño  de Etiquetas.View ", "ancona:perm:permission.diseno-de-etiquetas.view")]
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
    [InlineData("ancona", "topic", "warranty returns")]
    [InlineData("ancona", "topic", "garantías")]
    [InlineData("ancona", "perm", "Permission.Ventas/Caja.View")]
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

    [Fact]
    public void Permission_with_and_without_accents_lands_in_same_group()
    {
        GroupName.TryCreate("ancona", "perm", "Permission.Auditorías.View", out var fromDatabase);
        GroupName.TryCreate("ancona", "perm", "permission.auditorias.view", out var typedByHand);
        Assert.Equal(fromDatabase, typedByHand);
    }
}
