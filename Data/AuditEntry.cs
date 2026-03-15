using System.ComponentModel.DataAnnotations;

namespace AqlaAwsS3Manager.Data;

public class AuditEntry
{
    public long Id { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ResourceType { get; set; }

    [MaxLength(2000)]
    public string? ResourceKey { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }
}
