using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Services;

namespace SimpleErp.Pages;

public class LoginModel(ErpService erp) : PageModel
{
    [BindProperty] public string UserName { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; private set; }

    public IActionResult OnGet() => erp.HasUser() ? Page() : RedirectToPage("/Setup");

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await erp.CheckPasswordAsync(UserName, Password);
        if (user is null)
        {
            Error = "帳號或密碼不對。";
            return Page();
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            ], CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToPage("/Index");
    }
}
