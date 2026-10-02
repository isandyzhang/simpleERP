using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Services;

namespace SimpleErp.Pages;

public class SetupModel(ErpService erp) : PageModel
{
    [BindProperty] public string UserName { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    [BindProperty] public string Confirm { get; set; } = "";
    public string? Error { get; private set; }

    public IActionResult OnGet() => erp.HasUser() ? RedirectToPage("/Login") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (erp.HasUser())
            return RedirectToPage("/Login");
        if (UserName.Trim().Length < 2 || Password.Length < 8)
        {
            Error = "帳號至少 2 個字，密碼至少 8 個字。";
            return Page();
        }
        if (Password != Confirm)
        {
            Error = "兩次密碼不一樣。";
            return Page();
        }

        await erp.CreateUserAsync(UserName, Password);
        var user = await erp.CheckPasswordAsync(UserName, Password);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, user!.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            ], CookieAuthenticationDefaults.AuthenticationScheme)));
        return RedirectToPage("/Index");
    }
}
