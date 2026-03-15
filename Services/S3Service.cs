using Amazon.S3;
using Amazon.S3.Model;

namespace AqlaAwsS3Manager.Services;

public class S3Service : IS3Service
{
    private readonly IUserS3ClientFactory _clientFactory;
    private const string Delimiter = "/";

    public S3Service(IUserS3ClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<IReadOnlyList<string>> ListBucketsAsync(CancellationToken cancellationToken = default)
    {
        // For per-user profiles we only expose the configured bucket.
        var (_, bucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        return new List<string> { bucket };
    }

    public async Task<IReadOnlyList<S3Item>> ListObjectsAsync(string bucket, string? prefix = null, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;

        var normalizedPrefix = NormalizePrefix(prefix);
        var request = new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = normalizedPrefix,
            Delimiter = Delimiter
        };

        var items = new List<S3Item>();
        string? continuationToken = null;

        do
        {
            request.ContinuationToken = continuationToken;
            var response = await client.ListObjectsV2Async(request, cancellationToken);

            foreach (var commonPrefix in response.CommonPrefixes)
            {
                var name = commonPrefix.TrimEnd('/');
                var lastSlash = name.LastIndexOf('/');
                if (lastSlash >= 0) name = name[(lastSlash + 1)..];
                items.Add(new S3Item
                {
                    Key = commonPrefix,
                    Name = name,
                    IsFolder = true
                });
            }

            foreach (var obj in response.S3Objects)
            {
                if (string.Equals(obj.Key, normalizedPrefix, StringComparison.Ordinal))
                    continue;
                var name = obj.Key;
                var lastSlash = name.LastIndexOf('/');
                if (lastSlash >= 0) name = name[(lastSlash + 1)..];
                items.Add(new S3Item
                {
                    Key = obj.Key,
                    Name = name,
                    IsFolder = false,
                    Size = obj.Size,
                    LastModified = obj.LastModified
                });
            }

            continuationToken = response.NextContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return items.OrderBy(i => !i.IsFolder).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<(IReadOnlyList<S3Item> Items, string? NextToken)> ListObjectsPageAsync(string bucket, string? prefix = null, string? continuationToken = null, int maxKeys = 100, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;

        var normalizedPrefix = NormalizePrefix(prefix);
        var request = new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = normalizedPrefix,
            Delimiter = Delimiter,
            MaxKeys = Math.Clamp(maxKeys, 1, 1000),
            ContinuationToken = continuationToken
        };

        var response = await client.ListObjectsV2Async(request, cancellationToken);
        var items = new List<S3Item>();

        foreach (var commonPrefix in response.CommonPrefixes)
        {
            var name = commonPrefix.TrimEnd('/');
            var lastSlash = name.LastIndexOf('/');
            if (lastSlash >= 0) name = name[(lastSlash + 1)..];
            items.Add(new S3Item { Key = commonPrefix, Name = name, IsFolder = true });
        }

        foreach (var obj in response.S3Objects)
        {
            if (string.Equals(obj.Key, normalizedPrefix, StringComparison.Ordinal)) continue;
            var name = obj.Key;
            var lastSlash = name.LastIndexOf('/');
            if (lastSlash >= 0) name = name[(lastSlash + 1)..];
            items.Add(new S3Item { Key = obj.Key, Name = name, IsFolder = false, Size = obj.Size, LastModified = obj.LastModified });
        }

        var list = items.OrderBy(i => !i.IsFolder).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
        return (list, response.NextContinuationToken);
    }

    public async Task<Stream> GetObjectAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;

        var response = await client.GetObjectAsync(bucket, key, cancellationToken);
        var ms = new MemoryStream();
        using (response)
        using (response.ResponseStream)
        {
            await response.ResponseStream.CopyToAsync(ms, cancellationToken);
        }
        ms.Position = 0;
        return ms;
    }

    public async Task<string> GetPresignedUrlAsync(string bucket, string key, bool inline, string? fileName = null, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;
        var expires = expiry ?? TimeSpan.FromMinutes(15);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Expires = DateTime.UtcNow.Add(expires)
        };
        if (inline)
            request.ResponseHeaderOverrides = new ResponseHeaderOverrides { ContentDisposition = "inline" };
        else if (!string.IsNullOrEmpty(fileName))
            request.ResponseHeaderOverrides = new ResponseHeaderOverrides { ContentDisposition = "attachment; filename=\"" + fileName.Replace("\"", "%22") + "\"" };
        else
            request.ResponseHeaderOverrides = new ResponseHeaderOverrides { ContentDisposition = "attachment" };
        return await Task.FromResult(client.GetPreSignedURL(request));
    }

    public async Task UploadObjectAsync(string bucket, string key, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType ?? "application/octet-stream"
        };
        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteObjectAsync(string bucket, string key, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;

        await client.DeleteObjectAsync(bucket, key, cancellationToken);
    }

    public async Task DeleteFolderAsync(string bucket, string prefix, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;

        var folderPrefix = NormalizePrefix(prefix);
        if (string.IsNullOrEmpty(folderPrefix)) return;

        var keysToDelete = new List<KeyVersion>();
        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = folderPrefix
        };
        string? continuationToken = null;
        do
        {
            listRequest.ContinuationToken = continuationToken;
            var listResponse = await client.ListObjectsV2Async(listRequest, cancellationToken);
            foreach (var obj in listResponse.S3Objects)
                keysToDelete.Add(new KeyVersion { Key = obj.Key });
            continuationToken = listResponse.NextContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        const int batchSize = 1000;
        for (var i = 0; i < keysToDelete.Count; i += batchSize)
        {
            var batch = keysToDelete.Skip(i).Take(batchSize).ToList();
            var deleteRequest = new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = batch
            };
            await client.DeleteObjectsAsync(deleteRequest, cancellationToken);
        }
    }

    public async Task BulkDeleteAsync(string bucket, IReadOnlyList<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0) return;
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;
        const int batchSize = 1000;
        for (var i = 0; i < keys.Count; i += batchSize)
        {
            var batch = keys.Skip(i).Take(batchSize).Select(k => new KeyVersion { Key = k }).ToList();
            await client.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = bucket, Objects = batch }, cancellationToken);
        }
    }

    public async Task CreateFolderAsync(string bucket, string prefix, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;

        var folderKey = NormalizePrefix(prefix);
        if (string.IsNullOrEmpty(folderKey)) return;
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = folderKey,
            InputStream = new MemoryStream(Array.Empty<byte>()),
            ContentType = "application/x-directory"
        };
        await client.PutObjectAsync(request, cancellationToken);
    }

    public async Task CopyObjectAsync(string bucket, string sourceKey, string destKey, string? destBucket = null, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;
        var dest = destBucket ?? bucket;
        var request = new CopyObjectRequest
        {
            SourceBucket = bucket,
            SourceKey = sourceKey,
            DestinationBucket = dest,
            DestinationKey = destKey
        };
        await client.CopyObjectAsync(request, cancellationToken);
    }

    public async Task CopyPrefixAsync(string bucket, string sourcePrefix, string destPrefix, string? destBucket = null, CancellationToken cancellationToken = default)
    {
        var (client, resolvedBucket) = await _clientFactory.GetClientForCurrentUserAsync(cancellationToken);
        bucket = resolvedBucket;
        var dest = destBucket ?? bucket;
        var src = NormalizePrefix(sourcePrefix) ?? "";
        var dst = NormalizePrefix(destPrefix) ?? "";

        var listRequest = new ListObjectsV2Request { BucketName = bucket, Prefix = src };
        var keys = new List<string>();
        string? continuationToken = null;
        do
        {
            listRequest.ContinuationToken = continuationToken;
            var listResponse = await client.ListObjectsV2Async(listRequest, cancellationToken);
            foreach (var obj in listResponse.S3Objects)
                keys.Add(obj.Key);
            continuationToken = listResponse.NextContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        foreach (var key in keys)
        {
            var suffix = key.StartsWith(src, StringComparison.Ordinal) ? key[src.Length..] : key;
            var destKey = dst + suffix;
            await client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = bucket,
                SourceKey = key,
                DestinationBucket = dest,
                DestinationKey = destKey
            }, cancellationToken);
        }
    }

    public async Task MovePrefixAsync(string bucket, string sourcePrefix, string destPrefix, string? destBucket = null, CancellationToken cancellationToken = default)
    {
        await CopyPrefixAsync(bucket, sourcePrefix, destPrefix, destBucket, cancellationToken);
        await DeleteFolderAsync(bucket, sourcePrefix, cancellationToken);
    }

    private static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return null;
        prefix = prefix.Trim();
        if (!prefix.EndsWith("/", StringComparison.Ordinal))
            prefix += "/";
        return prefix;
    }
}
