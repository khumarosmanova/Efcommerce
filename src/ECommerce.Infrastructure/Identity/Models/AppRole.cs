using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity.Models;

public class AppRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public AppRole() : base()
    {
    }

    public AppRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
