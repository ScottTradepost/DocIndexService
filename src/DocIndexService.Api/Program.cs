using DocIndexService.Application.DependencyInjection;
using DocIndexService.Api.Security;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Configuration;
using DocIndexService.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDocIndexSharedConfiguration(builder.Environment);

builder.Services
    .AddDocIndexSerilog(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddScoped<IPasswordHasher<ApiClient>, PasswordHasher<ApiClient>>();
builder.Services
    .AddAuthentication(ApiAuthenticationDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiClientAuthenticationHandler>(
        ApiAuthenticationDefaults.SchemeName,
        _ => { });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
