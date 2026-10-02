using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Data;
using SimpleErp.Services;

namespace SimpleErp.Pages.Products;

public class ProductEditModel(ErpService erp) : PageModel
{
    [BindProperty] public Product Item { get; set; } = new() { Unit = "盒", Temperature = Temps.Ambient };
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return Page();
        var found = await erp.ProductAsync(id);
        if (found is null)
            return RedirectToPage("Index");
        Item = found;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Error = await erp.SaveProductAsync(Item);
        return Error is null ? RedirectToPage("Index") : Page();
    }
}
