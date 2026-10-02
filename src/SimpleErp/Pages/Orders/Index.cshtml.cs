using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Services;

namespace SimpleErp.Pages.Orders;

public class OrdersIndexModel(ErpService erp) : PageModel
{
    public List<OrderSummary> Items { get; private set; } = [];
    public async Task OnGet() => Items = await erp.OrdersAsync();
}
