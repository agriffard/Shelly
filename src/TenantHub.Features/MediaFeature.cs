using CShells.Features;
using CShells.AspNetCore.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenantHub.Core.Services;
using TenantHub.Data.Services;

namespace TenantHub.Features;

[ShellFeature("Media", DisplayName = "File Vault", Description = "Upload and share files within the tenant", DependsOn = ["Core"])]
public class MediaFeature : IWebShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IMediaService, MediaService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
    }
}
