using CShells.Features;
using CShells.AspNetCore.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenantHub.Core.Services;
using TenantHub.Data.Services;

namespace TenantHub.Features;

[ShellFeature("Notes", DisplayName = "Notes", Description = "Rich-text note taking", DependsOn = ["Core"])]
public class NotesFeature : IWebShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<INoteService, NoteService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
    }
}
