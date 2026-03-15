using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class PreviewModel : PageModel
{
    private readonly IS3Service _s3;

    public PreviewModel(IS3Service s3) => _s3 = s3;

    [BindProperty(SupportsGet = true)]
    public string Bucket { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Key { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Bucket) || string.IsNullOrEmpty(Key))
            return RedirectToPage("/Index", new { error = "Bucket and key are required." });

        try
        {
            var url = await _s3.GetPresignedUrlAsync(Bucket, Key, true, null, TimeSpan.FromMinutes(15), cancellationToken);
            return Redirect(url);
        }
        catch (Exception)
        {
            return RedirectToPage("/Index", new { bucket = Bucket, error = "Preview failed." });
        }
    }

    private static string GetContentType(string key)
    {
        var ext = Path.GetExtension(key).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".bmp" => "image/bmp",
            ".ico" => "image/x-icon",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain; charset=utf-8",
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".json" => "application/json",
            ".xml" => "application/xml",
            _ => "application/octet-stream"
        };
    }
}
