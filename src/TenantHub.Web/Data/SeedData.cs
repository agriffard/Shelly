using Microsoft.AspNetCore.Identity;
using TenantHub.Data.Models;

namespace TenantHub.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure SuperAdmin role exists
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
        }

        if (!await roleManager.RoleExistsAsync("TenantAdmin"))
        {
            await roleManager.CreateAsync(new IdentityRole("TenantAdmin"));
        }

        if (!await roleManager.RoleExistsAsync("TenantUser"))
        {
            await roleManager.CreateAsync(new IdentityRole("TenantUser"));
        }

        // Create default SuperAdmin user
        const string adminEmail = "admin@tenanthub.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }
    }
}
