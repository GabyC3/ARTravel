namespace ARTravel.Application.Interfaces.Identity;

public interface IAccessTokenSessionValidator
{
    Task<IReadOnlyList<string>?> GetActiveRolesAsync(
        string tokenId,
        string sessionId,
        int userId,
        int tokenVersion,
        CancellationToken cancellationToken);
}
