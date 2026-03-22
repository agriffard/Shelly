using CShells.Features;
using CShells.AspNetCore.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenantHub.Core.Services;
using TenantHub.Data.Services;

namespace TenantHub.Features;

[ShellFeature("Announcements", DisplayName = "Announcements", Description = "Broadcast messages to tenant users", DependsOn = ["Core"])]
public class AnnouncementsFeature : IWebShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IAnnouncementService, AnnouncementService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
    }
}
