using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly IS3Service _s3;
    private readonly IAuditService _audit;

    public DeleteModel(IS3Service s3, IAuditService audit) { _s3 = s3; _audit = audit; }

    [BindProperty(SupportsGet = true)]
    public string Bucket { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Key { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Prefix { get; set; }

    public string FileName { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Bucket) || string.IsNullOrEmpty(Key))
            return RedirectToPage("/Index", new { error = "Bucket and key are required." });
        FileName = Key.Contains('/') ? Key[(Key.LastIndexOf('/') + 1)..] : Key;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Bucket) || string.IsNullOrEmpty(Key))
            return RedirectToPage("/Index", new { error = "Bucket and key are required." });

        try
        {
            await _s3.DeleteObjectAsync(Bucket, Key, cancellationToken);
            await _audit.LogAsync("DeleteObject", resourceType: "Object", resourceKey: Key, cancellationToken: cancellationToken);
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, message = "Object deleted." });
        }
        catch (Exception ex)
        {
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, error = "Delete failed: " + ex.Message });
        }
    }
}
