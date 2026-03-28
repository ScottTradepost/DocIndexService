using DocIndexService.Application.DependencyInjection;
using DocIndexService.Infrastructure.Configuration;
using DocIndexService.Infrastructure.DependencyInjection;
using DocIndexService.Worker.HostedServices;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddDocIndexSharedConfiguration(builder.Environment);

builder.Services
	.AddDocIndexSerilog(builder.Configuration)
	.AddApplicationServices()
	.AddInfrastructureServices(builder.Configuration)
	.AddHostedService<ScanWorkerService>();

var host = builder.Build();
host.Run();
