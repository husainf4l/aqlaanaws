using System.ComponentModel.DataAnnotations;
using AqlaAwsS3Manager.Data;
using AqlaAwsS3Manager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginModel> _logger;
    private readonly IAuditService _audit;

    public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, IAuditService audit)
    {
        _signInManager = signInManager;
        _logger = logger;
        _audit = audit;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in.", Input.Email);
            await _audit.LogAsync("Login", details: Input.Email);
            return LocalRedirect(ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} locked out.", Input.Email);
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }
}
