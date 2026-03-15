using Amazon;
using Amazon.S3;
using AqlaAwsS3Manager.Data;
using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AqlaAwsS3Manager.Pages.Account;

[Authorize]
public class S3ProfileModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserS3CredentialStore _credentialStore;
    private readonly IAuditService _audit;

    public S3ProfileModel(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        IUserS3CredentialStore credentialStore,
        IAuditService audit)
    {
        _db = db;
        _userManager = userManager;
        _credentialStore = credentialStore;
        _audit = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [Display(Name = "Display name")]
        [StringLength(100)]
        public string DisplayName { get; set; } = "Default";

        [Required]
        [Display(Name = "Region")]
        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bucket name")]
        [StringLength(200)]
        public string BucketName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Access key")]
        public string AccessKey { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Secret key")]
        public string SecretKey { get; set; } = string.Empty;
    }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User)
                   ?? throw new InvalidOperationException("User not found.");

        var profile = await _db.UserS3Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        if (profile is not null)
        {
            Input = new InputModel
            {
                DisplayName = profile.DisplayName,
                Region = profile.Region,
                BucketName = profile.BucketName,
                // Do not show existing keys; require re-entry for security.
                AccessKey = string.Empty,
                SecretKey = string.Empty
            };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Validate credentials and bucket by calling AWS before saving.
        try
        {
            var region = string.IsNullOrWhiteSpace(Input.Region)
                ? RegionEndpoint.USEast1
                : RegionEndpoint.GetBySystemName(Input.Region.Trim());

            using var testClient = new AmazonS3Client(Input.AccessKey, Input.SecretKey, region);
            // This will throw if keys or bucket are invalid or region mismatch.
            await testClient.GetBucketLocationAsync(Input.BucketName.Trim());
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Could not validate S3 credentials or bucket: {ex.Message}");
            return Page();
        }

        var user = await _userManager.GetUserAsync(User)
                   ?? throw new InvalidOperationException("User not found.");

        var profile = await _db.UserS3Profiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        var now = DateTime.UtcNow;
        if (profile is null)
        {
            profile = new UserS3Profile
            {
                UserId = user.Id,
                DisplayName = Input.DisplayName,
                Region = Input.Region.Trim(),
                BucketName = Input.BucketName.Trim(),
                AccessKeyEncrypted = string.Empty,
                SecretKeyEncrypted = string.Empty,
                CreatedAt = now
            };
            _db.UserS3Profiles.Add(profile);
        }
        else
        {
            profile.DisplayName = Input.DisplayName;
            profile.Region = Input.Region.Trim();
            profile.BucketName = Input.BucketName.Trim();
            profile.UpdatedAt = now;
        }

        await _credentialStore.SetAsync(profile, Input.AccessKey, Input.SecretKey);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("S3ProfileSave", resourceType: "S3Profile", details: profile.BucketName);

        TempData["S3ProfileMessage"] = "S3 profile saved.";
        return RedirectToPage("/Account/S3Profile");
    }
}

