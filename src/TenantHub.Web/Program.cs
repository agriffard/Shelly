using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TenantHub.Core.Services;
using TenantHub.Data;
using TenantHub.Data.Models;
using TenantHub.Data.Providers;
using TenantHub.Data.Services;
using TenantHub.Features;
using TenantHub.Web.Data;
using TenantHub.Web.Middleware;
using TenantHub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core: AdminDbContext ---
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- ASP.NET Core Identity ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AdminDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/accessdenied";
});

// --- Authorization policies ---
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
});

// --- Admin-level services ---
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ITenantManagementService, TenantManagementService>();
builder.Services.AddScoped<IShellReloadService, CShellsReloadService>();

// --- CShells with ASP.NET Core integration ---
// Pass the Features assembly so CShells discovers all [ShellFeature] classes
builder.Services.AddCShellsAspNetCore(cshells =>
{
    // Add a Default shell (required by CShells) with no tenant features
    cshells.AddShell("Default", shell => shell.WithFeatures("Core"));
    cshells.WithProvider<DatabaseShellSettingsProvider>();
}, [typeof(CoreFeature).Assembly]);

// --- Blazor ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Store root provider for Blazor components to access IShellHost
TenantHub.Web.Components.AppServices.RootProvider = app.Services;

// --- Apply admin migrations and seed data ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

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
app.UseAntiforgery();
app.UseMiddleware<SuspendedTenantMiddleware>();

// --- Protect /admin routes at the middleware level ---
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/admin")
        && !path.StartsWithSegments("/_blazor")
        && !path.StartsWithSegments("/_framework"))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.Redirect($"/account/login?returnUrl={Uri.EscapeDataString(path)}");
            return;
        }
        if (!context.User.IsInRole("SuperAdmin"))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied.");
            return;
        }
    }
    await next();
});

// --- Auth endpoints (must be plain HTTP, not Blazor interactive) ---
// These endpoints swap HttpContext.RequestServices to the root container so that
// Identity/Auth services resolve correctly even if CShells shell middleware has
// already set a shell-scoped provider on the request.
app.MapPost("/api/account/login", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    // Swap to root scope so SignInManager (and its internal auth calls) resolve correctly
    var originalServices = context.RequestServices;
    using var scope = app.Services.CreateScope();
    context.RequestServices = scope.ServiceProvider;
    try
    {
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/admin/tenants" : returnUrl);
        }
        return Results.Redirect("/account/login?error=1");
    }
    finally
    {
        context.RequestServices = originalServices;
    }
});

app.MapPost("/api/account/logout", async (HttpContext context) =>
{
    var originalServices = context.RequestServices;
    using var scope = app.Services.CreateScope();
    context.RequestServices = scope.ServiceProvider;
    try
    {
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        await signInManager.SignOutAsync();
        return Results.Redirect("/");
    }
    finally
    {
        context.RequestServices = originalServices;
    }
});

// Debug endpoint: check what IShellHost sees for a tenant
app.MapGet("/api/debug/shell/{slug}", (string slug) =>
{
    var shellHost = app.Services.GetRequiredService<CShells.Hosting.IShellHost>();
    try
    {
        var shell = shellHost.GetShell(new CShells.ShellId(slug));
        using var scope = shell.ServiceProvider.CreateScope();
        var hasNoteService = scope.ServiceProvider.GetService(typeof(TenantHub.Core.Services.INoteService)) is not null;
        var hasTaskService = scope.ServiceProvider.GetService(typeof(TenantHub.Core.Services.ITaskService)) is not null;
        var hasTenantService = scope.ServiceProvider.GetService(typeof(TenantHub.Core.Services.ICurrentTenantService)) is not null;
        return Results.Ok(new
        {
            id = shell.Id.Name,
            settingsFeatures = shell.Settings.EnabledFeatures,
            activatedFeatures = shell.EnabledFeatures,
            allShellIds = shellHost.AllShells.Select(s => s.Id.Name).ToList(),
            services = new { hasNoteService, hasTaskService, hasTenantService }
        });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = $"Shell '{slug}' not found", allShellIds = shellHost.AllShells.Select(s => s.Id.Name).ToList() });
    }
});

app.MapRazorComponents<TenantHub.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapShells();

app.Run();
