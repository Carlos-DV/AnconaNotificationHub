namespace Application.Abstractions.Identity;

public interface IUserGroupResolver
{
    Task<UserGroups> ResolveAsync(string tenant, int userId, CancellationToken cancellationToken);
}
