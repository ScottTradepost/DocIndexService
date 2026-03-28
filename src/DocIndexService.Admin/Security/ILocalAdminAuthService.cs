using System.Security.Claims;

namespace DocIndexService.Admin.Security;

public interface ILocalAdminAuthService
{
    Task<ClaimsPrincipal?> AuthenticateAsync(string userNameOrEmail, string password, CancellationToken cancellationToken);
}
