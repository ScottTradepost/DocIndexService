using System.ComponentModel.DataAnnotations;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Admin.Pages.Users;

[Authorize(Policy = "CanManageUsers")]
public sealed class IndexModel : PageModel
{
    private readonly DocIndexDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public IndexModel(DocIndexDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    [BindProperty]
    public CreateUserInput NewUser { get; set; } = new();

    public IReadOnlyList<UserListItem> Users { get; private set; } = Array.Empty<UserListItem>();
    public IReadOnlyList<RoleOption> Roles { get; private set; } = Array.Empty<RoleOption>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadPageAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        if (NewUser.SelectedRoleNames.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Select at least one role.");
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        var normalizedUserName = NewUser.UserName.Trim();
        var normalizedEmail = NewUser.Email.Trim();

        var userNameExists = await _dbContext.UsersSet
            .AnyAsync(x => x.UserName == normalizedUserName, cancellationToken);
        if (userNameExists)
        {
            ModelState.AddModelError(string.Empty, "Username already exists.");
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        var emailExists = await _dbContext.UsersSet
            .AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            ModelState.AddModelError(string.Empty, "Email already exists.");
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        var selectedRoleNames = NewUser.SelectedRoleNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var roles = await _dbContext.RolesSet
            .Where(x => selectedRoleNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        if (roles.Count != selectedRoleNames.Length)
        {
            ModelState.AddModelError(string.Empty, "One or more selected roles are invalid.");
            await LoadPageAsync(cancellationToken);
            return Page();
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = normalizedUserName,
            Email = normalizedEmail,
            IsEnabled = NewUser.IsEnabled,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, NewUser.Password);

        _dbContext.UsersSet.Add(user);
        foreach (var role in roles)
        {
            _dbContext.UserRolesSet.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedUtc = now,
                UpdatedUtc = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        StatusMessage = $"User '{user.UserName}' created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSetEnabledAsync(Guid userId, bool isEnabled, CancellationToken cancellationToken)
    {
        var user = await _dbContext.UsersSet.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
        {
            StatusMessage = "User not found.";
            return RedirectToPage();
        }

        user.IsEnabled = isEnabled;
        user.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var action = isEnabled ? "enabled" : "disabled";
        StatusMessage = $"User '{user.UserName}' has been {action}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid userId, string? newPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            StatusMessage = "Password must be at least 8 characters.";
            return RedirectToPage();
        }

        var user = await _dbContext.UsersSet.FindAsync(new object[] { userId }, cancellationToken: cancellationToken);
        if (user is null)
        {
            StatusMessage = "User not found.";
            return RedirectToPage();
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        StatusMessage = $"Password for user '{user.UserName}' has been reset.";
        return RedirectToPage();
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken)
    {
        Roles = await _dbContext.RolesSet
            .OrderBy(x => x.Name)
            .Select(x => new RoleOption(x.Name, x.Description ?? string.Empty))
            .ToListAsync(cancellationToken);

        Users = await _dbContext.UsersSet
            .AsNoTracking()
            .OrderBy(x => x.UserName)
            .Select(x => new UserListItem(
                x.Id,
                x.UserName,
                x.Email,
                x.IsEnabled,
                x.UserRoles
                    .Where(ur => ur.Role != null)
                    .OrderBy(ur => ur.Role!.Name)
                    .Select(ur => ur.Role!.Name)
                    .ToArray(),
                x.LastLoginUtc))
            .ToListAsync(cancellationToken);
    }

    public sealed class CreateUserInput
    {
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public List<string> SelectedRoleNames { get; set; } = new();
    }

    public sealed record UserListItem(
        Guid Id,
        string UserName,
        string Email,
        bool IsEnabled,
        IReadOnlyList<string> Roles,
        DateTime? LastLoginUtc);

    public sealed record RoleOption(string Name, string Description);
}
