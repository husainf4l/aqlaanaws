namespace AqlaAwsS3Manager.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? resourceType = null, string? resourceKey = null, string? details = null, CancellationToken cancellationToken = default);
}
