using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Data;
using SimpleErp.Services;

namespace SimpleErp.Pages.Stock;

public class StockIndexModel(ErpService erp) : PageModel
{
    public List<Product> Products { get; private set; } = [];
    public List<StockMovement> Movements { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync(string? error)
    {
        Error = error;
        await LoadAsync();
    }

    public Task<IActionResult> OnPostReceiveAsync(string productId, int qty, string note) =>
        Finish(erp.ReceiveAsync(productId, qty, note ?? ""));

    public Task<IActionResult> OnPostAdjustAsync(string productId, int qty, string note) =>
        Finish(erp.AdjustAsync(productId, qty, note ?? ""));

    private async Task<IActionResult> Finish(Task<string?> action)
    {
        var error = await action;
        return RedirectToPage(new { error });
    }

    private async Task LoadAsync()
    {
        Products = await erp.ProductsAsync();
        Movements = await erp.RecentMovementsAsync();
    }
}
