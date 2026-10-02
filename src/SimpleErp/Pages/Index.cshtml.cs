using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Services;

namespace SimpleErp.Pages;

public class IndexModel(ErpService erp) : PageModel
{
    public List<StockRow> Stock { get; private set; } = [];
    public List<OrderSummary> OpenOrders { get; private set; } = [];

    public async Task OnGet()
    {
        Stock = await erp.StockAsync();
        OpenOrders = (await erp.OrdersAsync())
            .Where(x => !x.Cancelled && x.Remaining > 0)
            .ToList();
    }
}
