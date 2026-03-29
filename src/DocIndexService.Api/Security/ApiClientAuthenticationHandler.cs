using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocIndexService.Api.Security;

public sealed class ApiClientAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly DocIndexDbContext _dbContext;
    private readonly IPasswordHasher<ApiClient> _passwordHasher;

    public ApiClientAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        DocIndexDbContext dbContext,
        IPasswordHasher<ApiClient> passwordHasher)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorizationHeaderValues))
        {
            return AuthenticateResult.NoResult();
        }

        if (!AuthenticationHeaderValue.TryParse(authorizationHeaderValues, out var headerValue) ||
            !string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(headerValue.Parameter))
        {
            return AuthenticateResult.NoResult();
        }

        if (!TryParseCredentials(headerValue.Parameter, out var clientId, out var clientSecret))
        {
            return AuthenticateResult.Fail("Invalid basic authentication header.");
        }

        var apiClient = await _dbContext.ApiClientsSet
            .AsTracking()
            .FirstOrDefaultAsync(
                x => x.ClientId == clientId && x.IsEnabled,
                Context.RequestAborted);

        if (apiClient is null)
        {
            return AuthenticateResult.Fail("Invalid API client credentials.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(apiClient, apiClient.ClientSecretHash, clientSecret);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return AuthenticateResult.Fail("Invalid API client credentials.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            apiClient.ClientSecretHash = _passwordHasher.HashPassword(apiClient, clientSecret);
        }

        apiClient.LastUsedUtc = DateTime.UtcNow;
        apiClient.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiClient.Id.ToString()),
            new(ClaimTypes.Name, apiClient.ClientId),
            new("client_name", apiClient.Name)
        };

        foreach (var scope in apiClient.AllowedScopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.Append("WWW-Authenticate", "Basic realm=\"DocIndexService API\"");
        return base.HandleChallengeAsync(properties);
    }

    private static bool TryParseCredentials(string encodedCredentials, out string clientId, out string clientSecret)
    {
        clientId = string.Empty;
        clientSecret = string.Empty;

        string decodedCredentials;
        try
        {
            decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decodedCredentials.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex == decodedCredentials.Length - 1)
        {
            return false;
        }

        clientId = decodedCredentials[..separatorIndex];
        clientSecret = decodedCredentials[(separatorIndex + 1)..];
        return true;
    }
}