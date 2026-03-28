using DocIndexService.Admin.Configuration;
using DocIndexService.Admin.Security;
using DocIndexService.Application.DependencyInjection;
using DocIndexService.Core.Constants;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Configuration;
using DocIndexService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDocIndexSharedConfiguration(builder.Environment);

builder.Services
    .AddOptions<AdminSecurityOptions>()
    .Bind(builder.Configuration.GetSection(AdminSecurityOptions.SectionName))
    .ValidateOnStart();

var sessionTimeout = builder.Configuration.GetValue<int>("AdminSecurity:SessionTimeoutMinutes", 60);

builder.Services
    .AddDocIndexSerilog(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "DocIndexService.Admin.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeout);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireRole(SystemRoles.SystemAdmin));

    options.AddPolicy("CanManageSources", policy =>
        policy.RequireRole(SystemRoles.SystemAdmin, SystemRoles.IndexManager));
});

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ILocalAdminAuthService, LocalAdminAuthService>();
builder.Services.AddHostedService<DevelopmentAdminSeedHostedService>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToFolder("/Account");

    options.Conventions.AuthorizeFolder("/Users", "CanManageUsers");
    options.Conventions.AuthorizeFolder("/Sources", "CanManageSources");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
