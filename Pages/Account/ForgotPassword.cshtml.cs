using System.ComponentModel.DataAnnotations;
using AqlaAwsS3Manager.Data;
using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            return RedirectToPage("./ForgotPasswordConfirmation");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetUrl = Url.Page("./ResetPassword", pageHandler: null, values: new { code = token, email = Input.Email }, protocol: Request.Scheme)!;
        var subject = "Reset your password - S3 Storage Manager";
        var body = $"<p>You requested a password reset.</p><p><a href=\"{resetUrl}\">Reset your password</a></p><p>If you didn't request this, ignore this email.</p><p>This link expires in 24 hours.</p>";
        await _emailSender.SendEmailAsync(Input.Email, subject, body, cancellationToken);

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
