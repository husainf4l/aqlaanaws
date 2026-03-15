using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class DeleteFolderModel : PageModel
{
    private readonly IS3Service _s3;
    private readonly IAuditService _audit;

    public DeleteFolderModel(IS3Service s3, IAuditService audit) { _s3 = s3; _audit = audit; }

    [BindProperty(SupportsGet = true)]
    public string Bucket { get; set; } = string.Empty;

    /// <summary>Folder prefix (e.g. uploads/2024/) — all objects under this will be deleted.</summary>
    [BindProperty(SupportsGet = true)]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Current path we're in (for back link).</summary>
    [BindProperty(SupportsGet = true)]
    public string? CurrentPrefix { get; set; }

    public string FolderName { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Bucket) || string.IsNullOrEmpty(Prefix))
            return RedirectToPage("/Index", new { error = "Bucket and folder are required." });
        var normalized = Prefix.TrimEnd('/');
        FolderName = string.IsNullOrEmpty(normalized) ? "Root" : (normalized.Contains('/') ? normalized[(normalized.LastIndexOf('/') + 1)..] : normalized);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Bucket) || string.IsNullOrEmpty(Prefix))
            return RedirectToPage("/Index", new { error = "Bucket and folder are required." });

        try
        {
            await _s3.DeleteFolderAsync(Bucket, Prefix, cancellationToken);
            await _audit.LogAsync("DeleteFolder", resourceType: "Folder", resourceKey: Prefix, cancellationToken: cancellationToken);
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = CurrentPrefix, message = "Folder and all its contents deleted." });
        }
        catch (Exception ex)
        {
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = CurrentPrefix, error = "Delete folder failed: " + ex.Message });
        }
    }
}
