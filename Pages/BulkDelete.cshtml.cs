using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class BulkDeleteModel : PageModel
{
    private readonly IS3Service _s3;
    private readonly IAuditService _audit;

    public BulkDeleteModel(IS3Service s3, IAuditService audit) { _s3 = s3; _audit = audit; }

    [BindProperty(SupportsGet = true)]
    public string Bucket { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Prefix { get; set; }

    [BindProperty]
    public List<string> Keys { get; set; } = new();

    public IActionResult OnGet(string? bucket, string? prefix, [FromQuery(Name = "key")] List<string>? keys = null)
    {
        Bucket = bucket ?? "";
        Prefix = prefix;
        if (string.IsNullOrEmpty(Bucket) || keys == null || keys.Count == 0)
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, error = "Select at least one item." });
        // Validate keys: must be non-empty, no path traversal, max 1000
        var sanitized = keys
            .Where(k => !string.IsNullOrWhiteSpace(k) && !k.Contains("..") && k.Length <= 1024)
            .Take(1000)
            .ToList();
        if (sanitized.Count == 0)
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, error = "No valid keys selected." });
        Keys = sanitized;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(Bucket) || Keys.Count == 0)
            return RedirectToPage("/Index", new { error = "Bucket and keys required." });

        try
        {
            await _s3.BulkDeleteAsync(Bucket, Keys, cancellationToken);
            await _audit.LogAsync("BulkDelete", resourceType: "Object", details: Keys.Count + " items", cancellationToken: cancellationToken);
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, message = Keys.Count + " item(s) deleted." });
        }
        catch (Exception ex)
        {
            return RedirectToPage("/Index", new { bucket = Bucket, prefix = Prefix, error = "Bulk delete failed: " + ex.Message });
        }
    }
}
