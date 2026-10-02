using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Data;
using SimpleErp.Services;

namespace SimpleErp.Pages.Products;

public class ProductsIndexModel(ErpService erp) : PageModel
{
    public List<Product> Items { get; private set; } = [];
    public async Task OnGet() => Items = await erp.ProductsAsync();
}
