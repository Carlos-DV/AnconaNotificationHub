using Application.Abstractions.Identity;
using Application.Features.Connections;
using Application.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.UnitTests.Features;

public class ResolveConnectionGroupsHandlerTests
{
    [Fact]
    public async Task Returns_user_all_branches_and_permissions()
    {
        var resolver = new FakeUserGroupResolver(new UserGroups(["001", "014"], ["Permissions.Warranty.View"]));

        var groups = await CreateHandler(resolver).HandleAsync("ancona", 38, CancellationToken.None);

        Assert.Equal(
            ["ancona:user:38", "ancona:all", "ancona:branch:001", "ancona:branch:014", "ancona:perm:permissions.warranty.view"],
            groups.Select(g => g.Value));
    }

    [Fact]
    public async Task Normalizes_permissions_with_spaces_and_accents()
    {
        var resolver = new FakeUserGroupResolver(new UserGroups([], ["Permission.Ajuste de inventario.View", "Permission.Auditorías.View"]));

        var groups = await CreateHandler(resolver).HandleAsync("ancona", 38, CancellationToken.None);

        Assert.Equal(
            ["ancona:user:38", "ancona:all", "ancona:perm:permission.ajuste-de-inventario.view", "ancona:perm:permission.auditorias.view"],
            groups.Select(g => g.Value));
    }

    [Fact]
    public async Task Falls_back_to_user_and_all_when_resolver_fails()
    {
        var resolver = new FakeUserGroupResolver(error: new InvalidOperationException("BD caída"));

        var groups = await CreateHandler(resolver).HandleAsync("ancona", 38, CancellationToken.None);

        Assert.Equal(["ancona:user:38", "ancona:all"], groups.Select(g => g.Value));
    }

    [Fact]
    public async Task Skips_invalid_values_and_duplicates()
    {
        var resolver = new FakeUserGroupResolver(new UserGroups(["001", "001", " "], ["¿?", "warranty.view"]));

        var groups = await CreateHandler(resolver).HandleAsync("ancona", 38, CancellationToken.None);

        Assert.Equal(["ancona:user:38", "ancona:all", "ancona:branch:001", "ancona:perm:warranty.view"],
            groups.Select(g => g.Value));
    }

    [Fact]
    public async Task Propagates_cancellation()
    {
        var resolver = new FakeUserGroupResolver(error: new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateHandler(resolver).HandleAsync("ancona", 38, CancellationToken.None));
    }

    private static ResolveConnectionGroupsHandler CreateHandler(IUserGroupResolver resolver) =>
        new(resolver, NullLogger<ResolveConnectionGroupsHandler>.Instance);
}
