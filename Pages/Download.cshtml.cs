using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages;

[Authorize]
public class DownloadModel : PageModel
{
    private readonly IS3Service _s3;

    public DownloadModel(IS3Service s3) => _s3 = s3;

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
            var fileName = Key.Contains('/') ? Key[(Key.LastIndexOf('/') + 1)..] : Key;
            var url = await _s3.GetPresignedUrlAsync(Bucket, Key, inline: false, fileName, expiry: TimeSpan.FromMinutes(15), cancellationToken);
            return Redirect(url);
        }
        catch (Exception)
        {
            return RedirectToPage("/Index", new { bucket = Bucket, error = "Download failed." });
        }
    }
}
