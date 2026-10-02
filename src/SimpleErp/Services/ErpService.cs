using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleErp.Data;

namespace SimpleErp.Services;

public sealed class ErpService(ErpDb db, IPasswordHasher<UserAccount> passwords)
{
    public bool HasUser() => db.Users.Any();

    public async Task CreateUserAsync(string userName, string password)
    {
        var user = new UserAccount { Id = Guid.NewGuid().ToString("N"), UserName = userName.Trim() };
        user.PasswordHash = passwords.HashPassword(user, password);
        db.Users.Add(user);
        if (!await db.Settings.AnyAsync())
            db.Settings.Add(new SellerSetting());
        await db.SaveChangesAsync();
    }

    public async Task<UserAccount?> CheckPasswordAsync(string userName, string password)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == userName.Trim());
        if (user is null)
            return null;
        var result = passwords.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public Task<SellerSetting> SettingAsync() =>
        db.Settings.SingleAsync(x => x.Id == "default");

    public async Task SaveSettingAsync(SellerSetting input)
    {
        var row = await SettingAsync();
        row.SenderName = input.SenderName.Trim();
        row.SenderPhone = input.SenderPhone.Trim();
        row.SenderAddress = input.SenderAddress.Trim();
        row.SenderMobile = input.SenderMobile.Trim();
        row.TcatCustomerId = input.TcatCustomerId.Trim();
        row.EcpayMerchantId = input.EcpayMerchantId.Trim();
        await db.SaveChangesAsync();
    }

    public Task<List<Product>> ProductsAsync() =>
        db.Products.OrderBy(x => x.Sku).ToListAsync();

    public Task<Product?> ProductAsync(string id) =>
        db.Products.SingleOrDefaultAsync(x => x.Id == id);

    public async Task<string?> SaveProductAsync(Product input)
    {
        input.Sku = input.Sku.Trim();
        input.Name = input.Name.Trim();
        input.ShortName = input.ShortName.Trim();
        input.Unit = input.Unit.Trim();
        if (input.Sku.Length == 0 || input.Name.Length == 0)
            return "請填編號和名稱。";
        if (input.ShortName.Length == 0)
            input.ShortName = input.Name.Length <= 20 ? input.Name : input.Name[..20];
        if (input.ShortName.Length > 20)
            return "短品名最多 20 個字。";
        if (await db.Products.AnyAsync(x => x.Sku == input.Sku && x.Id != input.Id))
            return "這個商品編號已經用過。";

        if (string.IsNullOrEmpty(input.Id))
        {
            input.Id = Guid.NewGuid().ToString("N");
            db.Products.Add(input);
        }
        else
        {
            var row = await db.Products.SingleAsync(x => x.Id == input.Id);
            row.Sku = input.Sku;
            row.Name = input.Name;
            row.Unit = input.Unit;
            row.Temperature = input.Temperature;
            row.ShortName = input.ShortName;
        }

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<List<StockRow>> StockAsync()
    {
        var products = await ProductsAsync();
        var rows = new List<StockRow>();
        foreach (var product in products)
        {
            var onHand = await OnHandAsync(product.Id);
            var committed = await CommittedAsync(product.Id);
            rows.Add(new StockRow(product, onHand, committed, onHand - committed));
        }
        return rows;
    }

    public async Task<string?> ReceiveAsync(string productId, int qty, string note)
    {
        if (qty <= 0)
            return "進貨數量要大於 0。";
        if (!await db.Products.AnyAsync(x => x.Id == productId))
            return "找不到商品。";
        db.StockMovements.Add(Movement(productId, qty, "inbound", null, note));
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> AdjustAsync(string productId, int qty, string note)
    {
        if (qty == 0)
            return "調整數量不能是 0。";
        if (!await db.Products.AnyAsync(x => x.Id == productId))
            return "找不到商品。";
        var onHand = await OnHandAsync(productId);
        var committed = await CommittedAsync(productId);
        if (onHand + qty < committed)
            return $"調整後在庫會低於已承諾未出（{committed}）。";
        db.StockMovements.Add(Movement(productId, qty, "adjust", null, note));
        await db.SaveChangesAsync();
        return null;
    }

    public Task<List<StockMovement>> RecentMovementsAsync() =>
        db.StockMovements.Include(x => x.Product).OrderByDescending(x => x.CreatedAt).Take(30).ToListAsync();

    public Task<List<Customer>> CustomersAsync() =>
        db.Customers.OrderBy(x => x.Name).ToListAsync();

    public Task<Customer?> CustomerAsync(string id) =>
        db.Customers.SingleOrDefaultAsync(x => x.Id == id);

    public async Task<string?> SaveCustomerAsync(Customer input)
    {
        input.Name = input.Name.Trim();
        input.Phone = input.Phone.Trim();
        input.Address = input.Address.Trim();
        input.StoreId = input.StoreId.Trim();
        input.StoreName = input.StoreName.Trim();
        if (input.Name.Length == 0)
            return "請填客戶姓名。";

        if (string.IsNullOrEmpty(input.Id))
        {
            input.Id = Guid.NewGuid().ToString("N");
            db.Customers.Add(input);
        }
        else
        {
            var row = await db.Customers.SingleAsync(x => x.Id == input.Id);
            row.Name = input.Name;
            row.Phone = input.Phone;
            row.Address = input.Address;
            row.StoreId = input.StoreId;
            row.StoreName = input.StoreName;
        }

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<List<OrderSummary>> OrdersAsync()
    {
        var orders = await db.Orders.Include(x => x.Customer).Include(x => x.Lines)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();
        var shipped = await ShippedByLineAsync();
        return orders.Select(order => Summarize(order, shipped)).ToList();
    }

    public async Task<OrderDetail?> OrderAsync(string id)
    {
        var order = await db.Orders.Include(x => x.Customer).Include(x => x.Lines).ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (order is null)
            return null;
        var shipped = await ShippedByLineAsync(order.Lines.Select(x => x.Id).ToList());
        var shipments = await db.Shipments.Include(x => x.Lines)
            .Where(x => x.OrderId == id).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var summary = Summarize(order, shipped);
        var lines = order.Lines.Select(line =>
        {
            var done = shipped.GetValueOrDefault(line.Id);
            return new OrderLineView(line, done, line.Quantity - done);
        }).ToList();
        return new OrderDetail(order, summary, lines, shipments);
    }

    public async Task<string?> CreateOrderAsync(string customerId, string note, IReadOnlyList<(string ProductId, int Qty)> lines)
    {
        var wanted = lines.Where(x => x.Qty > 0).ToList();
        if (wanted.Count == 0)
            return "至少要有一個品項數量大於 0。";
        if (!await db.Customers.AnyAsync(x => x.Id == customerId))
            return "找不到客戶。";

        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var (productId, qty) in wanted)
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId);
            if (product is null)
                return "找不到商品。";
            var available = await OnHandAsync(productId) - await CommittedAsync(productId);
            if (qty > available)
                return $"{product.Name} 可再賣只有 {available}。";
        }

        var order = new Order
        {
            Id = Guid.NewGuid().ToString("N"),
            CustomerId = customerId,
            Note = note.Trim(),
            CreatedAt = Clock.UtcNow()
        };
        db.Orders.Add(order);
        foreach (var (productId, qty) in wanted)
        {
            db.OrderLines.Add(new OrderLine
            {
                Id = Guid.NewGuid().ToString("N"),
                OrderId = order.Id,
                ProductId = productId,
                Quantity = qty
            });
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return null;
    }

    public async Task<string?> ShipAsync(ShipInput input)
    {
        var order = await db.Orders.Include(x => x.Lines).ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == input.OrderId);
        if (order is null)
            return "找不到訂單。";
        if (order.CancelledAt is not null)
            return "這張訂單的未出數量已取消。";

        var picks = input.Lines.Where(x => x.Qty > 0).ToList();
        if (picks.Count == 0)
            return "出貨數量要大於 0。";

        var pickedLines = new List<(OrderLine Line, int Qty)>();
        foreach (var pick in picks)
        {
            var line = order.Lines.SingleOrDefault(x => x.Id == pick.OrderLineId);
            if (line is null)
                return "找不到訂單品項。";
            var remaining = line.Quantity - await ShippedAsync(line.Id);
            if (pick.Qty > remaining)
                return $"{line.Product.Name} 未出只剩 {remaining}。";
            var onHand = await OnHandAsync(line.ProductId);
            if (pick.Qty > onHand)
                return $"{line.Product.Name} 在庫只有 {onHand}。";
            pickedLines.Add((line, pick.Qty));
        }

        var temps = pickedLines.Select(x => x.Line.Product.Temperature).Distinct().ToList();
        if (input.Channel == Channels.Tcat && temps.Count > 1)
            return "同一張黑貓託運單不能混溫層。請分成兩筆出貨。";
        if (input.Channel == Channels.Unimart && temps.Any(x => x != Temps.Ambient))
            return "交貨便只寄常溫商品。";

        input.RecipientName = input.RecipientName.Trim();
        input.RecipientPhone = input.RecipientPhone.Trim();
        input.RecipientAddress = input.RecipientAddress.Trim();
        input.StoreId = input.StoreId.Trim();
        input.StoreName = input.StoreName.Trim();
        input.Tracking = input.Tracking.Trim();

        if (input.RecipientName.Length == 0)
            return "請填收件人姓名。";

        if (input.Channel == Channels.Tcat)
        {
            if (input.RecipientPhone.Length == 0 || input.RecipientAddress.Length == 0)
                return "黑貓要填電話和地址。";
        }
        else if (input.Channel == Channels.Unimart)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(input.RecipientPhone, "^09\\d{8}$"))
                return "交貨便手機要是 09 開頭的 10 碼。";
            if (input.StoreId.Length == 0 || input.StoreName.Length == 0)
                return "請填取貨門市代碼和名稱。";
            if (input.Tracking.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(input.Tracking, "^\\d{12}$"))
                return "交貨便代碼要是 12 個數字，或先留空。";
        }
        else
        {
            return "請選出貨方式。";
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        var shipment = new Shipment
        {
            Id = Guid.NewGuid().ToString("N"),
            OrderId = order.Id,
            Channel = input.Channel,
            RecipientName = input.RecipientName,
            RecipientPhone = input.RecipientPhone,
            RecipientAddress = input.Channel == Channels.Tcat ? input.RecipientAddress : "",
            Temperature = temps[0],
            StoreId = input.Channel == Channels.Unimart ? input.StoreId : "",
            StoreName = input.Channel == Channels.Unimart ? input.StoreName : "",
            Succeeded = true,
            Status = "created",
            CreatedAt = Clock.UtcNow()
        };
        ApplyTracking(shipment, input.Tracking);
        db.Shipments.Add(shipment);
        foreach (var (line, qty) in pickedLines)
        {
            db.ShipmentLines.Add(new ShipmentLine
            {
                Id = Guid.NewGuid().ToString("N"),
                ShipmentId = shipment.Id,
                OrderLineId = line.Id,
                Quantity = qty
            });
            db.StockMovements.Add(Movement(line.ProductId, -qty, "shipment", shipment.Id, $"{line.Product.Name} 出貨"));
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return null;
    }

    public async Task<string?> UpdateTrackingAsync(string shipmentId, string tracking)
    {
        var shipment = await db.Shipments.SingleOrDefaultAsync(x => x.Id == shipmentId);
        if (shipment is null)
            return "找不到出貨。";
        tracking = tracking.Trim();
        if (shipment.Channel == Channels.Unimart && tracking.Length > 0 &&
            !System.Text.RegularExpressions.Regex.IsMatch(tracking, "^\\d{12}$"))
            return "交貨便代碼要是 12 個數字。";
        ApplyTracking(shipment, tracking);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> MarkDoneAsync(string shipmentId)
    {
        var shipment = await db.Shipments.SingleOrDefaultAsync(x => x.Id == shipmentId);
        if (shipment is null)
            return "找不到出貨。";
        shipment.Status = "done";
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> CancelRemainingAsync(string orderId)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == orderId);
        if (order is null)
            return "找不到訂單。";
        if (order.CancelledAt is not null)
            return "未出數量已經取消。";
        order.CancelledAt = Clock.UtcNow();
        await db.SaveChangesAsync();
        return null;
    }

    public static string OrderStatus(OrderSummary summary)
    {
        if (summary.Cancelled && summary.Shipped == 0)
            return "已取消";
        if (summary.Cancelled)
            return "已取消剩餘";
        if (summary.Shipped == 0)
            return "未出貨";
        if (summary.Remaining == 0)
            return "已出完";
        return "部分出貨";
    }

    public static string ShipmentStatus(Shipment shipment) =>
        shipment.Status == "done"
            ? (shipment.Channel == Channels.Unimart ? "已取貨" : "已配達")
            : "已建單";

    public static string Reference(Shipment shipment) =>
        shipment.Channel == Channels.Tcat ? shipment.TcatWaybillNo : shipment.CvsCode;

    private async Task<int> OnHandAsync(string productId) =>
        await db.StockMovements.Where(x => x.ProductId == productId).SumAsync(x => (int?)x.Quantity) ?? 0;

    private async Task<int> CommittedAsync(string productId)
    {
        var lines = await db.OrderLines
            .Where(x => x.ProductId == productId && x.Order.CancelledAt == null)
            .Select(x => new { x.Id, x.Quantity })
            .ToListAsync();
        if (lines.Count == 0)
            return 0;
        var ids = lines.Select(x => x.Id).ToList();
        var shipped = await db.ShipmentLines
            .Where(x => ids.Contains(x.OrderLineId) && x.Shipment.Succeeded)
            .GroupBy(x => x.OrderLineId)
            .Select(x => new { x.Key, Qty = x.Sum(y => y.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Qty);
        return lines.Sum(x => x.Quantity - shipped.GetValueOrDefault(x.Id));
    }

    private async Task<int> ShippedAsync(string orderLineId) =>
        await db.ShipmentLines.Where(x => x.OrderLineId == orderLineId && x.Shipment.Succeeded)
            .SumAsync(x => (int?)x.Quantity) ?? 0;

    private async Task<Dictionary<string, int>> ShippedByLineAsync(List<string>? lineIds = null)
    {
        var query = db.ShipmentLines.Where(x => x.Shipment.Succeeded);
        if (lineIds is not null)
            query = query.Where(x => lineIds.Contains(x.OrderLineId));
        return await query.GroupBy(x => x.OrderLineId)
            .Select(x => new { x.Key, Qty = x.Sum(y => y.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Qty);
    }

    private static OrderSummary Summarize(Order order, Dictionary<string, int> shipped)
    {
        var ordered = order.Lines.Sum(x => x.Quantity);
        var done = order.Lines.Sum(x => shipped.GetValueOrDefault(x.Id));
        var remaining = order.CancelledAt is null ? ordered - done : 0;
        return new OrderSummary(order, ordered, done, remaining, order.CancelledAt is not null);
    }

    private static void ApplyTracking(Shipment shipment, string tracking)
    {
        if (shipment.Channel == Channels.Tcat)
        {
            shipment.TcatWaybillNo = tracking;
            return;
        }

        shipment.CvsCode = tracking;
        shipment.CvsPaymentNo = tracking.Length == 12 ? tracking[..8] : "";
        shipment.CvsValidationNo = tracking.Length == 12 ? tracking[8..] : "";
    }

    private static StockMovement Movement(string productId, int qty, string reason, string? shipmentId, string note) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ProductId = productId,
            Quantity = qty,
            Reason = reason,
            ShipmentId = shipmentId,
            Note = note.Trim(),
            CreatedAt = Clock.UtcNow()
        };
}

public sealed record StockRow(Product Product, int OnHand, int Committed, int Available);

public sealed record OrderSummary(Order Order, int Ordered, int Shipped, int Remaining, bool Cancelled);

public sealed record OrderLineView(OrderLine Line, int Shipped, int Remaining);

public sealed record OrderDetail(Order Order, OrderSummary Summary, List<OrderLineView> Lines, List<Shipment> Shipments);

public sealed class ShipInput
{
    public string OrderId { get; set; } = "";
    public string Channel { get; set; } = Channels.Tcat;
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string StoreName { get; set; } = "";
    public string Tracking { get; set; } = "";
    public List<ShipLineInput> Lines { get; set; } = [];
}

public sealed class ShipLineInput
{
    public string OrderLineId { get; set; } = "";
    public int Qty { get; set; }
}
