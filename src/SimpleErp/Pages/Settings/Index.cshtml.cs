using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Data;
using SimpleErp.Services;

namespace SimpleErp.Pages.Settings;

public class SettingsIndexModel(ErpService erp) : PageModel
{
    [BindProperty] public SellerSetting Item { get; set; } = new();
    public bool Saved { get; private set; }

    public async Task OnGetAsync() => Item = await erp.SettingAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await erp.SaveSettingAsync(Item);
        Item = await erp.SettingAsync();
        Saved = true;
        return Page();
    }
}
