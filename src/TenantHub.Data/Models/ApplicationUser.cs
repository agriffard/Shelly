using Microsoft.AspNetCore.Identity;

namespace TenantHub.Data.Models;

public class ApplicationUser : IdentityUser
{
    public Guid? TenantId { get; set; }
}
