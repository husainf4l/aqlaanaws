using AqlaAwsS3Manager.Data;

namespace AqlaAwsS3Manager.Services;

/// <summary>
/// Abstraction for storing and retrieving per-user S3 credentials.
/// Default implementation uses ASP.NET Core Data Protection + database.
/// Can be replaced with AWS Secrets Manager, Azure Key Vault, etc. via configuration.
/// </summary>
public interface IUserS3CredentialStore
{
    /// <summary>Gets decrypted access and secret key for the given profile, or null if not found.</summary>
    Task<(string AccessKey, string SecretKey)?> GetAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>Stores credentials for the profile (encrypts and sets the profile's encrypted fields, or writes to external store).</summary>
    Task SetAsync(UserS3Profile profile, string accessKey, string secretKey, CancellationToken cancellationToken = default);
}
