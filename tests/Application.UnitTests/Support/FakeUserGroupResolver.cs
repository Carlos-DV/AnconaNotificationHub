using Application.Abstractions.Identity;

namespace Application.UnitTests.Support;

internal sealed class FakeUserGroupResolver(UserGroups? result = null, Exception? error = null) : IUserGroupResolver
{
    public Task<UserGroups> ResolveAsync(string tenant, int userId, CancellationToken cancellationToken) =>
        error is not null ? Task.FromException<UserGroups>(error) : Task.FromResult(result ?? new UserGroups([], []));
}
