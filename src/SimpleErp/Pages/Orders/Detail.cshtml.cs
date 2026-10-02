using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimpleErp.Services;

namespace SimpleErp.Pages.Orders;

public class OrderDetailModel(ErpService erp) : PageModel
{
    public OrderDetail? Detail { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string id, string? error)
    {
        Error = error;
        Detail = await erp.OrderAsync(id);
        return Detail is null ? RedirectToPage("Index") : Page();
    }

    public async Task<IActionResult> OnPostShipAsync(string id, string channel, string recipientName, string recipientPhone, string recipientAddress, string storeId, string storeName, string tracking)
    {
        var lines = new List<ShipLineInput>();
        foreach (var key in Request.Form.Keys.Where(x => x.StartsWith("qty_", StringComparison.Ordinal)))
        {
            if (int.TryParse(Request.Form[key], out var qty))
                lines.Add(new ShipLineInput { OrderLineId = key["qty_".Length..], Qty = qty });
        }

        var error = await erp.ShipAsync(new ShipInput
        {
            OrderId = id,
            Channel = channel,
            RecipientName = recipientName ?? "",
            RecipientPhone = recipientPhone ?? "",
            RecipientAddress = recipientAddress ?? "",
            StoreId = storeId ?? "",
            StoreName = storeName ?? "",
            Tracking = tracking ?? "",
            Lines = lines
        });
        return RedirectToPage(new { id, error });
    }

    public async Task<IActionResult> OnPostTrackAsync(string id, string shipmentId, string tracking)
    {
        var error = await erp.UpdateTrackingAsync(shipmentId, tracking ?? "");
        return RedirectToPage(new { id, error });
    }

    public async Task<IActionResult> OnPostDoneAsync(string id, string shipmentId)
    {
        var error = await erp.MarkDoneAsync(shipmentId);
        return RedirectToPage(new { id, error });
    }

    public async Task<IActionResult> OnPostCancelAsync(string id)
    {
        var error = await erp.CancelRemainingAsync(id);
        return RedirectToPage(new { id, error });
    }
}
