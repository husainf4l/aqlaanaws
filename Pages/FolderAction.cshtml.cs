using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class FolderActionModel : PageModel
{
    private readonly IS3Service _s3;
    private readonly IAuditService _audit;

    public FolderActionModel(IS3Service s3, IAuditService audit) { _s3 = s3; _audit = audit; }

    [BindProperty(SupportsGet = true)]
    public string Bucket { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Prefix { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Action { get; set; } = "copy"; // copy | move

    [BindProperty]
    public string? DestPrefix { get; set; }

    public string SourceFolderName { get; set; } = string.Empty;
    public string? ParentPrefix { get; set; }

    public IActionResult OnGet(string? bucket, string? prefix, string? action)
    {
        Bucket = bucket ?? "";
        Prefix = prefix?.Trim().TrimEnd('/');
        Action = string.Equals(action, "move", StringComparison.OrdinalIgnoreCase) ? "move" : "copy";
        if (string.IsNullOrEmpty(Bucket) || string.IsNullOrEmpty(Prefix))
            return RedirectToPage("/Index", new { bucket = Bucket, error = "Bucket and folder path required." });
        var lastSlash = Prefix!.LastIndexOf('/');
        SourceFolderName = lastSlash >= 0 ? Prefix[(lastSlash + 1)..] : Prefix;
        ParentPrefix = lastSlash >= 0 ? Prefix[..lastSlash] : null;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Bucket) || string.IsNullOrEmpty(Prefix))
            return RedirectToPage("/Index", new { bucket = Bucket, error = "Bucket and folder path required." });
        var dest = (DestPrefix ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(dest))
        {
            ModelState.AddModelError(nameof(DestPrefix), "Destination path is required.");
            SetNamesFromPrefix();
            return Page();
        }
        var sourcePrefix = Prefix!.EndsWith("/", StringComparison.Ordinal) ? Prefix : Prefix + "/";
        var destPrefix = dest.EndsWith("/", StringComparison.Ordinal) ? dest : dest + "/";
        if (string.Equals(sourcePrefix.TrimEnd('/'), destPrefix.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(DestPrefix), "Destination must be different from source.");
            SetNamesFromPrefix();
            return Page();
        }

        try
        {
            if (string.Equals(Action, "move", StringComparison.OrdinalIgnoreCase))
            {
                await _s3.MovePrefixAsync(Bucket, sourcePrefix, destPrefix, null, cancellationToken);
                await _audit.LogAsync("MoveFolder", resourceType: "Folder", resourceKey: sourcePrefix, details: "to " + destPrefix, cancellationToken: cancellationToken);
                var destParent = destPrefix.TrimEnd('/');
                var lastSlashIdx = destParent.LastIndexOf('/');
                var redirectPrefix = lastSlashIdx >= 0 ? destParent[..lastSlashIdx] : null;
                return RedirectToPage("/Index", new { bucket = Bucket, prefix = redirectPrefix, message = "Folder moved." });
            }
            else
            {
                await _s3.CopyPrefixAsync(Bucket, sourcePrefix, destPrefix, null, cancellationToken);
                await _audit.LogAsync("CopyFolder", resourceType: "Folder", resourceKey: sourcePrefix, details: "to " + destPrefix, cancellationToken: cancellationToken);
                return RedirectToPage("/Index", new { bucket = Bucket, prefix = destPrefix.TrimEnd('/'), message = "Folder copied." });
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Operation failed: " + ex.Message);
            SetNamesFromPrefix();
            return Page();
        }
    }

    private void SetNamesFromPrefix()
    {
        if (string.IsNullOrEmpty(Prefix)) return;
        var lastSlash = Prefix!.LastIndexOf('/');
        SourceFolderName = lastSlash >= 0 ? Prefix[(lastSlash + 1)..] : Prefix;
        ParentPrefix = lastSlash >= 0 ? Prefix[..lastSlash] : null;
    }
}
