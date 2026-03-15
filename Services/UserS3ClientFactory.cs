using Amazon;
using Amazon.S3;
using AqlaAwsS3Manager.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AqlaAwsS3Manager.Services;

public interface IUserS3ClientFactory
{
    Task<(IAmazonS3 Client, string Bucket)> GetClientForCurrentUserAsync(CancellationToken cancellationToken = default);
}

public class UserS3ClientFactory : IUserS3ClientFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IUserS3CredentialStore _credentialStore;

    public UserS3ClientFactory(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IUserS3CredentialStore credentialStore)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _db = db;
        _credentialStore = credentialStore;
    }

    public async Task<(IAmazonS3 Client, string Bucket)> GetClientForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var httpUser = _httpContextAccessor.HttpContext?.User
                      ?? throw new InvalidOperationException("No current HTTP user.");

        var user = await _userManager.GetUserAsync(httpUser)
                   ?? throw new InvalidOperationException("User not found.");

        var profile = await _db.UserS3Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken)
            ?? throw new InvalidOperationException("No S3 profile configured for this user.");

        var credentials = await _credentialStore.GetAsync(profile.Id, cancellationToken)
            ?? throw new InvalidOperationException("No S3 profile configured for this user.");
        var accessKey = credentials.AccessKey;
        var secretKey = credentials.SecretKey;

        // Detect the actual bucket region once, then cache it in the profile.
        var regionSystemName = profile.Region;
        if (string.IsNullOrWhiteSpace(regionSystemName))
        {
            using var probeClient = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.USEast1);
            var locationResponse = await probeClient.GetBucketLocationAsync(profile.BucketName, cancellationToken);
            regionSystemName = locationResponse.Location?.Value;

            if (string.IsNullOrEmpty(regionSystemName) || string.Equals(regionSystemName, "US", StringComparison.OrdinalIgnoreCase))
            {
                regionSystemName = "us-east-1";
            }

            // Persist detected region — check EF local cache before issuing a second query.
            var trackedProfile = _db.UserS3Profiles.Local.FirstOrDefault(p => p.Id == profile.Id)
                ?? await _db.UserS3Profiles.FirstOrDefaultAsync(p => p.Id == profile.Id, cancellationToken);
            if (trackedProfile is not null)
            {
                trackedProfile.Region = regionSystemName;
                trackedProfile.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        var region = RegionEndpoint.GetBySystemName(regionSystemName);

        var client = new AmazonS3Client(accessKey, secretKey, region);
        return (client, profile.BucketName);
    }
}

