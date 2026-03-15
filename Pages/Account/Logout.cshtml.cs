using AqlaAwsS3Manager.Data;
using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LogoutModel> _logger;
    private readonly IAuditService _audit;

    public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger, IAuditService audit)
    {
        _signInManager = signInManager;
        _logger = logger;
        _audit = audit;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        await _audit.LogAsync("Logout", cancellationToken: cancellationToken);
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        returnUrl ??= Url.Content("~/");
        return LocalRedirect(returnUrl);
    }
}
