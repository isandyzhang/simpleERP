using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Data;
using SimpleErp.Services;

namespace SimpleErp.Pages.Orders;

public class OrderCreateModel(ErpService erp) : PageModel
{
    public List<Customer> Customers { get; private set; } = [];
    public List<StockRow> Stock { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync(string? error)
    {
        Error = error;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync(string customerId, string note)
    {
        var lines = new List<(string ProductId, int Qty)>();
        foreach (var key in Request.Form.Keys.Where(x => x.StartsWith("qty_", StringComparison.Ordinal)))
        {
            if (int.TryParse(Request.Form[key], out var qty))
                lines.Add((key["qty_".Length..], qty));
        }

        var error = await erp.CreateOrderAsync(customerId, note ?? "", lines);
        if (error is null)
            return RedirectToPage("Index");
        return RedirectToPage(new { error });
    }

    private async Task LoadAsync()
    {
        Customers = await erp.CustomersAsync();
        Stock = await erp.StockAsync();
    }
}
