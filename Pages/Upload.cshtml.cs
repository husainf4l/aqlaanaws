using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class UploadModel : PageModel
{
    private readonly IS3Service _s3;
    private readonly IAuditService _audit;

    public UploadModel(IS3Service s3, IAuditService audit) { _s3 = s3; _audit = audit; }

    [BindProperty(SupportsGet = true)]
    public string Bucket { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Prefix { get; set; }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Bucket)) return RedirectToPage("/Index", new { error = "Bucket is required." });
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Bucket))
            return RedirectToPage("/Index", new { error = "Bucket is required." });
        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            TempData["Error"] = "Please select a file.";
            return Page();
        }

        var key = string.IsNullOrWhiteSpace(Prefix)
            ? UploadedFile.FileName
            : Prefix.TrimEnd('/') + "/" + UploadedFile.FileName;

        try
        {
            await using var stream = UploadedFile.OpenReadStream();
            await _s3.UploadObjectAsync(Bucket, key, stream, UploadedFile.ContentType, cancellationToken);
            await _audit.LogAsync("Upload", resourceType: "Object", resourceKey: key, cancellationToken: cancellationToken);
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, message = "File uploaded successfully." });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Upload failed: " + ex.Message;
            return Page();
        }
    }
}
