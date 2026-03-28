using System.Security.Claims;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Admin.Security;

public sealed class LocalAdminAuthService : ILocalAdminAuthService
{
    private readonly DocIndexDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public LocalAdminAuthService(DocIndexDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string userNameOrEmail, string password, CancellationToken cancellationToken)
    {
        var login = userNameOrEmail.Trim().ToLowerInvariant();

        var user = await _dbContext.UsersSet
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.IsEnabled &&
                     (x.UserName.ToLower() == login || x.Email.ToLower() == login),
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(user.UserRoles
            .Where(x => x.Role is not null)
            .Select(x => new Claim(ClaimTypes.Role, x.Role!.Name)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
