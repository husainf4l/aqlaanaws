using Microsoft.AspNetCore.Identity;

namespace AqlaAwsS3Manager.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
