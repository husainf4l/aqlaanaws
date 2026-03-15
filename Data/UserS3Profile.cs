using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AqlaAwsS3Manager.Data;

public class UserS3Profile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = "Default";

    [Required]
    [MaxLength(100)]
    public string Region { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string BucketName { get; set; } = string.Empty;

    [Required]
    public string AccessKeyEncrypted { get; set; } = string.Empty;

    [Required]
    public string SecretKeyEncrypted { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

