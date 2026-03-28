namespace DocIndexService.Admin.Configuration;

public sealed class AdminSecurityOptions
{
    public const string SectionName = "AdminSecurity";

    public int SessionTimeoutMinutes { get; init; } = 60;
    public SeedAdminOptions SeedAdmin { get; init; } = new();
}

public sealed class SeedAdminOptions
{
    public bool Enabled { get; init; }
    public string UserName { get; init; } = "admin";
    public string Email { get; init; } = "admin@localhost";
    public string Password { get; init; } = "ChangeMe123!";
}
