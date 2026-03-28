using DocIndexService.Admin.Pages.Sources;
using DocIndexService.Admin.Pages.Users;
using Microsoft.AspNetCore.Authorization;

namespace DocIndexService.Tests.Admin;

public sealed class RoleAccessPolicyTests
{
    [Fact]
    public void SourcesPage_ShouldRequireCanManageSourcesPolicy()
    {
        var authorize = typeof(DocIndexService.Admin.Pages.Sources.IndexModel)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal("CanManageSources", authorize!.Policy);
    }

    [Fact]
    public void UsersPage_ShouldRequireCanManageUsersPolicy()
    {
        var authorize = typeof(DocIndexService.Admin.Pages.Users.IndexModel)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authorize);
        Assert.Equal("CanManageUsers", authorize!.Policy);
    }
}
