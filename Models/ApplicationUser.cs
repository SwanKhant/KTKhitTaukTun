using Microsoft.AspNetCore.Identity;

namespace KhittoktunPortal.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
