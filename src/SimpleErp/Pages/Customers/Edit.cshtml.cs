using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Data;
using SimpleErp.Services;

namespace SimpleErp.Pages.Customers;

public class CustomerEditModel(ErpService erp) : PageModel
{
    [BindProperty] public Customer Item { get; set; } = new();
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return Page();
        var found = await erp.CustomerAsync(id);
        if (found is null)
            return RedirectToPage("Index");
        Item = found;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Error = await erp.SaveCustomerAsync(Item);
        return Error is null ? RedirectToPage("Index") : Page();
    }
}
