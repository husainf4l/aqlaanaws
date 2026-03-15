using System.ComponentModel.DataAnnotations;
using AqlaAwsS3Manager.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AqlaAwsS3Manager.Pages.Account;

public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(Email))
            return RedirectToPage("./ForgotPassword");
        Input = new InputModel { Email = Email };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        var result = await _userManager.ResetPasswordAsync(user, Code!, Input.Password);
        if (result.Succeeded)
            return RedirectToPage("./ResetPasswordConfirmation");

        foreach (var e in result.Errors)
            ModelState.AddModelError(string.Empty, e.Description);
        return Page();
    }
}
