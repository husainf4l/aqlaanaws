using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class CreateFolderModel : PageModel
{
    private readonly IS3Service _s3;

    public CreateFolderModel(IS3Service s3) => _s3 = s3;

    [BindProperty(SupportsGet = true)]
    public string Bucket { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Prefix { get; set; }

    [BindProperty]
    public string FolderName { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Bucket)) return RedirectToPage("/Index", new { error = "Bucket is required." });
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Bucket))
            return RedirectToPage("/Index", new { error = "Bucket is required." });
        var name = FolderName?.Trim().Replace("/", "", StringComparison.Ordinal);
        if (string.IsNullOrEmpty(name))
        {
            TempData["Error"] = "Enter a folder name.";
            return Page();
        }

        var newPrefix = string.IsNullOrWhiteSpace(Prefix) ? name + "/" : Prefix.TrimEnd('/') + "/" + name + "/";
        try
        {
            await _s3.CreateFolderAsync(Bucket, newPrefix, cancellationToken);
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, message = "Folder created." });
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Failed to create folder: " + ex.Message;
            return Page();
        }
    }
}
