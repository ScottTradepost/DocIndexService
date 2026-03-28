using DocIndexService.Admin.Configuration;
using DocIndexService.Core.Constants;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocIndexService.Admin.Security;

public sealed class DevelopmentAdminSeedHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly IOptions<AdminSecurityOptions> _options;
    private readonly ILogger<DevelopmentAdminSeedHostedService> _logger;

    public DevelopmentAdminSeedHostedService(
        IServiceScopeFactory serviceScopeFactory,
        IWebHostEnvironment environment,
        IOptions<AdminSecurityOptions> options,
        ILogger<DevelopmentAdminSeedHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _environment = environment;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment() || !_options.Value.SeedAdmin.Enabled)
        {
            return;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DocIndexDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        var rolesByName = await EnsureRolesAsync(dbContext, cancellationToken);
        await EnsureAdminUserAsync(dbContext, hasher, rolesByName, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<Dictionary<string, Role>> EnsureRolesAsync(DocIndexDbContext dbContext, CancellationToken cancellationToken)
    {
        var roleNames = new[] { SystemRoles.SystemAdmin, SystemRoles.IndexManager, SystemRoles.Reviewer };
        var now = DateTime.UtcNow;

        var existing = await dbContext.RolesSet
            .Where(x => roleNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        var map = existing.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var roleName in roleNames)
        {
            if (map.ContainsKey(roleName))
            {
                continue;
            }

            var role = new Role
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                Description = $"System role: {roleName}",
                IsSystemRole = true,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            dbContext.RolesSet.Add(role);
            map[roleName] = role;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return map;
    }

    private async Task EnsureAdminUserAsync(
        DocIndexDbContext dbContext,
        IPasswordHasher<User> hasher,
        IReadOnlyDictionary<string, Role> rolesByName,
        CancellationToken cancellationToken)
    {
        var seed = _options.Value.SeedAdmin;
        var now = DateTime.UtcNow;
        var login = seed.UserName.Trim();
        var email = seed.Email.Trim();

        var user = await dbContext.UsersSet
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(
                x => x.UserName == login || x.Email == email,
                cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = login,
                Email = email,
                IsEnabled = true,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            user.PasswordHash = hasher.HashPassword(user, seed.Password);
            dbContext.UsersSet.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Development admin user '{UserName}' seeded.", login);
        }

        var adminRole = rolesByName[SystemRoles.SystemAdmin];
        var hasAdminRole = await dbContext.UserRolesSet.AnyAsync(
            x => x.UserId == user.Id && x.RoleId == adminRole.Id,
            cancellationToken);

        if (!hasAdminRole)
        {
            dbContext.UserRolesSet.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id,
                CreatedUtc = now,
                UpdatedUtc = now
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
