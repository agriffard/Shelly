using CShells.Features;
using CShells.AspNetCore.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenantHub.Core.Services;
using TenantHub.Data.Services;

namespace TenantHub.Features;

[ShellFeature("Tasks", DisplayName = "Tasks", Description = "Lightweight task tracker", DependsOn = ["Core"])]
public class TasksFeature : IWebShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
    }
}
