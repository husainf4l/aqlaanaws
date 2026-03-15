namespace AqlaAwsS3Manager.Services;

public class S3Item
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public long? Size { get; set; }
    public DateTime? LastModified { get; set; }
}

public interface IS3Service
{
    Task<IReadOnlyList<string>> ListBucketsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<S3Item>> ListObjectsAsync(string bucket, string? prefix = null, CancellationToken cancellationToken = default);
    /// <summary>One page of listing. Returns items and next continuation token (null if no more).</summary>
    Task<(IReadOnlyList<S3Item> Items, string? NextToken)> ListObjectsPageAsync(string bucket, string? prefix = null, string? continuationToken = null, int maxKeys = 100, CancellationToken cancellationToken = default);
    Task<Stream> GetObjectAsync(string bucket, string key, CancellationToken cancellationToken = default);
    /// <summary>Presigned URL for GetObject. inline=true for browser display, false for download. Expiry default 15 min.</summary>
    Task<string> GetPresignedUrlAsync(string bucket, string key, bool inline, string? fileName = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task UploadObjectAsync(string bucket, string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default);
    Task DeleteObjectAsync(string bucket, string key, CancellationToken cancellationToken = default);
    /// <summary>Deletes all objects under the given prefix (folder). Uses batch delete; prefix should end with /.</summary>
    Task DeleteFolderAsync(string bucket, string prefix, CancellationToken cancellationToken = default);
    /// <summary>Delete multiple objects by key. Does not support folder prefixes; delete keys one by one.</summary>
    Task BulkDeleteAsync(string bucket, IReadOnlyList<string> keys, CancellationToken cancellationToken = default);
    Task CreateFolderAsync(string bucket, string prefix, CancellationToken cancellationToken = default);
    /// <summary>Copy one object to another key (same or different bucket).</summary>
    Task CopyObjectAsync(string bucket, string sourceKey, string destKey, string? destBucket = null, CancellationToken cancellationToken = default);
    /// <summary>Copy all objects under sourcePrefix to destPrefix (key path rewritten). Does not delete source.</summary>
    Task CopyPrefixAsync(string bucket, string sourcePrefix, string destPrefix, string? destBucket = null, CancellationToken cancellationToken = default);
    /// <summary>Move folder: copy prefix to dest then delete source.</summary>
    Task MovePrefixAsync(string bucket, string sourcePrefix, string destPrefix, string? destBucket = null, CancellationToken cancellationToken = default);
}
