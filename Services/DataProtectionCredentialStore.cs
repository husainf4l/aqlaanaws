using System.Text;
using AqlaAwsS3Manager.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AqlaAwsS3Manager.Services;

/// <summary>
/// Stores S3 credentials in the database, encrypted with ASP.NET Core Data Protection.
/// Suitable for single-server or dev. For multi-machine or audit requirements, consider
/// implementing IUserS3CredentialStore with AWS Secrets Manager or Azure Key Vault.
/// </summary>
public class DataProtectionCredentialStore : IUserS3CredentialStore
{
    private readonly AppDbContext _db;
    private readonly IDataProtector _protector;

    public DataProtectionCredentialStore(AppDbContext db, IDataProtectionProvider dataProtectionProvider)
    {
        _db = db;
        _protector = dataProtectionProvider.CreateProtector("UserS3Profile");
    }

    public async Task<(string AccessKey, string SecretKey)?> GetAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.UserS3Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);
        if (profile is null || string.IsNullOrEmpty(profile.AccessKeyEncrypted))
            return null;
        var accessKeyBytes = _protector.Unprotect(Convert.FromBase64String(profile.AccessKeyEncrypted));
        var secretKeyBytes = _protector.Unprotect(Convert.FromBase64String(profile.SecretKeyEncrypted));
        return (Encoding.UTF8.GetString(accessKeyBytes), Encoding.UTF8.GetString(secretKeyBytes));
    }

    public Task SetAsync(UserS3Profile profile, string accessKey, string secretKey, CancellationToken cancellationToken = default)
    {
        profile.AccessKeyEncrypted = Convert.ToBase64String(_protector.Protect(Encoding.UTF8.GetBytes(accessKey)));
        profile.SecretKeyEncrypted = Convert.ToBase64String(_protector.Protect(Encoding.UTF8.GetBytes(secretKey)));
        return Task.CompletedTask;
    }
}
