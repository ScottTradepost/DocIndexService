using System.ComponentModel.DataAnnotations;
using DocIndexService.Admin.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocIndexService.Admin.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly ILocalAdminAuthService _localAdminAuthService;

    public LoginModel(ILocalAdminAuthService localAdminAuthService)
    {
        _localAdminAuthService = localAdminAuthService;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Dashboard/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var principal = await _localAdminAuthService.AuthenticateAsync(
            Input.UserNameOrEmail,
            Input.Password,
            cancellationToken);

        if (principal is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return RedirectToPage("/Dashboard/Index");
    }

    public sealed class LoginInput
    {
        [Required]
        [Display(Name = "Username or Email")]
        public string UserNameOrEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
