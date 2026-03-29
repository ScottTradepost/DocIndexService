using DocIndexService.Admin.Pages.Users;
using DocIndexService.Core.Constants;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Tests.Admin;

public sealed class UserManagementWorkflowTests
{
    [Fact]
    public async Task OnPostCreateAsync_ShouldCreateUserAndAssignSelectedRoles()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db, SystemRoles.SystemAdmin, SystemRoles.IndexManager);

        var hasher = new PasswordHasher<User>();
        var pageModel = new IndexModel(db, hasher)
        {
            NewUser = new IndexModel.CreateUserInput
            {
                UserName = "jane",
                Email = "jane@localhost",
                Password = "Strong#12345",
                IsEnabled = true,
                SelectedRoleNames = new List<string> { SystemRoles.IndexManager }
            }
        };

        var result = await pageModel.OnPostCreateAsync(CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);

        var savedUser = await db.UsersSet.FirstOrDefaultAsync(x => x.UserName == "jane");
        Assert.NotNull(savedUser);
        Assert.True(savedUser!.IsEnabled);

        var verification = hasher.VerifyHashedPassword(savedUser, savedUser.PasswordHash, "Strong#12345");
        Assert.NotEqual(PasswordVerificationResult.Failed, verification);

        var roleNames = await db.UserRolesSet
            .Where(x => x.UserId == savedUser.Id)
            .Join(db.RolesSet, ur => ur.RoleId, r => r.Id, (_, role) => role.Name)
            .ToListAsync();

        Assert.Single(roleNames);
        Assert.Contains(SystemRoles.IndexManager, roleNames);
    }

    [Fact]
    public async Task OnPostCreateAsync_ShouldReturnPage_WhenRoleSelectionMissing()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db, SystemRoles.SystemAdmin);

        var pageModel = new IndexModel(db, new PasswordHasher<User>())
        {
            NewUser = new IndexModel.CreateUserInput
            {
                UserName = "no-role-user",
                Email = "norole@localhost",
                Password = "Strong#12345",
                IsEnabled = true,
                SelectedRoleNames = new List<string>()
            }
        };

        var result = await pageModel.OnPostCreateAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(pageModel.ModelState, entry =>
            entry.Value is not null &&
            entry.Value.Errors.Any(error => error.ErrorMessage.Contains("at least one role", StringComparison.OrdinalIgnoreCase)));

        var saved = await db.UsersSet.FirstOrDefaultAsync(x => x.UserName == "no-role-user");
        Assert.Null(saved);
    }

    [Fact]
    public async Task OnPostCreateAsync_ShouldReturnPage_WhenUsernameAlreadyExists()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db, SystemRoles.SystemAdmin);

        var now = DateTime.UtcNow;
        db.UsersSet.Add(new User
        {
            Id = Guid.NewGuid(),
            UserName = "duplicate",
            Email = "existing@localhost",
            PasswordHash = "hash",
            IsEnabled = true,
            CreatedUtc = now,
            UpdatedUtc = now
        });
        await db.SaveChangesAsync();

        var pageModel = new IndexModel(db, new PasswordHasher<User>())
        {
            NewUser = new IndexModel.CreateUserInput
            {
                UserName = "duplicate",
                Email = "new@localhost",
                Password = "Strong#12345",
                IsEnabled = true,
                SelectedRoleNames = new List<string> { SystemRoles.SystemAdmin }
            }
        };

        var result = await pageModel.OnPostCreateAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.Contains(pageModel.ModelState, entry =>
            entry.Value is not null &&
            entry.Value.Errors.Any(error => error.ErrorMessage.Contains("Username already exists", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task OnPostSetEnabledAsync_ShouldDisableEnabledUser()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.UsersSet.Add(new User
        {
            Id = userId,
            UserName = "active",
            Email = "active@localhost",
            PasswordHash = "hash",
            IsEnabled = true,
            CreatedUtc = now,
            UpdatedUtc = now
        });
        await db.SaveChangesAsync();

        var pageModel = new IndexModel(db, new PasswordHasher<User>());
        var result = await pageModel.OnPostSetEnabledAsync(userId, false, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Contains("disabled", pageModel.StatusMessage, StringComparison.OrdinalIgnoreCase);

        var updated = await db.UsersSet.FirstOrDefaultAsync(x => x.Id == userId);
        Assert.NotNull(updated);
        Assert.False(updated!.IsEnabled);
    }

    [Fact]
    public async Task OnPostSetEnabledAsync_ShouldEnableDisabledUser()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.UsersSet.Add(new User
        {
            Id = userId,
            UserName = "inactive",
            Email = "inactive@localhost",
            PasswordHash = "hash",
            IsEnabled = false,
            CreatedUtc = now,
            UpdatedUtc = now
        });
        await db.SaveChangesAsync();

        var pageModel = new IndexModel(db, new PasswordHasher<User>());
        var result = await pageModel.OnPostSetEnabledAsync(userId, true, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Contains("enabled", pageModel.StatusMessage, StringComparison.OrdinalIgnoreCase);

        var updated = await db.UsersSet.FirstOrDefaultAsync(x => x.Id == userId);
        Assert.NotNull(updated);
        Assert.True(updated!.IsEnabled);
    }

    [Fact]
    public async Task OnPostResetPasswordAsync_ShouldHashNewPassword()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var hasher = new PasswordHasher<User>();

        var originalPassword = "OldPassword#123";
        var originalUser = new User
        {
            Id = userId,
            UserName = "user",
            Email = "user@localhost",
            IsEnabled = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        originalUser.PasswordHash = hasher.HashPassword(originalUser, originalPassword);
        db.UsersSet.Add(originalUser);
        await db.SaveChangesAsync();

        var pageModel = new IndexModel(db, hasher);
        var newPassword = "NewSecret#456";
        var result = await pageModel.OnPostResetPasswordAsync(userId, newPassword, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Contains("password", pageModel.StatusMessage, StringComparison.OrdinalIgnoreCase);

        var updated = await db.UsersSet.FirstOrDefaultAsync(x => x.Id == userId);
        Assert.NotNull(updated);

        // Verify old password no longer works
        var verifyOld = hasher.VerifyHashedPassword(updated, updated!.PasswordHash, originalPassword);
        Assert.Equal(PasswordVerificationResult.Failed, verifyOld);

        // Verify new password works
        var verifyNew = hasher.VerifyHashedPassword(updated, updated.PasswordHash, newPassword);
        Assert.NotEqual(PasswordVerificationResult.Failed, verifyNew);
    }

    [Fact]
    public async Task OnPostResetPasswordAsync_ShouldReturnError_WhenPasswordTooShort()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.UsersSet.Add(new User
        {
            Id = userId,
            UserName = "user",
            Email = "user@localhost",
            PasswordHash = "hash",
            IsEnabled = true,
            CreatedUtc = now,
            UpdatedUtc = now
        });
        await db.SaveChangesAsync();

        var pageModel = new IndexModel(db, new PasswordHasher<User>());
        var result = await pageModel.OnPostResetPasswordAsync(userId, "short", CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Contains("at least 8 characters", pageModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnPostSetEnabledAsync_ShouldReturnError_WhenUserNotFound()
    {
        await using var db = CreateDbContext();

        var pageModel = new IndexModel(db, new PasswordHasher<User>());
        var result = await pageModel.OnPostSetEnabledAsync(Guid.NewGuid(), true, CancellationToken.None);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Contains("not found", pageModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static DocIndexDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DocIndexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new DocIndexDbContext(options);
    }

    private static async Task SeedRolesAsync(DocIndexDbContext dbContext, params string[] roleNames)
    {
        var now = DateTime.UtcNow;
        foreach (var roleName in roleNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            dbContext.RolesSet.Add(new Role
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                Description = roleName,
                IsSystemRole = true,
                CreatedUtc = now,
                UpdatedUtc = now
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
