using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IS3Service _s3;
    private readonly AwsOptions _awsOptions;

    public IndexModel(IS3Service s3, IOptions<AwsOptions> awsOptions)
    {
        _s3 = s3;
        _awsOptions = awsOptions.Value;
    }

    public IReadOnlyList<string> Buckets { get; set; } = Array.Empty<string>();
    public IReadOnlyList<S3Item> Items { get; set; } = Array.Empty<S3Item>();
    public string? CurrentBucket { get; set; }
    public string? CurrentPrefix { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public List<BreadcrumbPart> Breadcrumbs { get; set; } = new();
    /// <summary>When set, we're viewing a continuation page; show "Back to first page".</summary>
    public string? ListToken { get; set; }
    /// <summary>When set, show "Load more" to fetch the next page.</summary>
    public string? NextListToken { get; set; }

    private const int PageSize = 200;

    public async Task<IActionResult> OnGetAsync(string? bucket, string? prefix, string? message, string? error, string? listToken = null)
    {
        Message = message;
        ErrorMessage = error;
        CurrentBucket = bucket ?? _awsOptions.DefaultBucket;
        CurrentPrefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim().TrimEnd('/');
        ListToken = listToken;

        if (string.IsNullOrEmpty(CurrentBucket))
        {
            try
            {
                Buckets = await _s3.ListBucketsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not list buckets. Check AWS configuration and credentials. " + ex.Message;
            }
            return Page();
        }

        BuildBreadcrumbs();
        try
        {
            var prefixForList = string.IsNullOrEmpty(CurrentPrefix) ? null : CurrentPrefix + "/";
            var (items, nextToken) = await _s3.ListObjectsPageAsync(CurrentBucket, prefixForList, listToken, PageSize);
            Items = items;
            NextListToken = nextToken;
        }
        catch (Exception ex)
        {
            ErrorMessage = "Could not list objects. " + ex.Message;
        }

        return Page();
    }

    private void BuildBreadcrumbs()
    {
        Breadcrumbs = new List<BreadcrumbPart> { new("Root", CurrentBucket!, null) };
        if (string.IsNullOrEmpty(CurrentPrefix)) return;
        var parts = CurrentPrefix.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var soFar = "";
        foreach (var part in parts)
        {
            soFar += part + "/";
            Breadcrumbs.Add(new BreadcrumbPart(part, CurrentBucket!, soFar.TrimEnd('/')));
        }
    }
}

public record BreadcrumbPart(string Label, string Bucket, string? Prefix);
