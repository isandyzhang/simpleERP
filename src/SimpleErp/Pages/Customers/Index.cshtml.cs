using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Data;
using SimpleErp.Services;

namespace SimpleErp.Pages.Customers;

public class CustomersIndexModel(ErpService erp) : PageModel
{
    public List<Customer> Items { get; private set; } = [];
    public async Task OnGet() => Items = await erp.CustomersAsync();
}
