using Microsoft.EntityFrameworkCore;

namespace SimpleErp.Data;

public sealed class ErpDb(DbContextOptions<ErpDb> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<SellerSetting> Settings => Set<SellerSetting>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<UserAccount>().HasIndex(x => x.UserName).IsUnique();
        model.Entity<Product>().HasIndex(x => x.Sku).IsUnique();
        model.Entity<Order>().HasIndex(x => x.CreatedAt);
        model.Entity<OrderLine>().HasIndex(x => x.OrderId);
        model.Entity<Shipment>().HasIndex(x => x.OrderId);
        model.Entity<Shipment>().HasIndex(x => new { x.OrderId, x.Channel });
        model.Entity<ShipmentLine>().HasIndex(x => x.ShipmentId);
        model.Entity<ShipmentLine>().HasIndex(x => x.OrderLineId);
        model.Entity<StockMovement>().HasIndex(x => x.ProductId);
        model.Entity<StockMovement>().HasIndex(x => x.CreatedAt);
    }
}

public sealed class UserAccount
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public sealed class SellerSetting
{
    public string Id { get; set; } = "default";
    public string SenderName { get; set; } = "";
    public string SenderPhone { get; set; } = "";
    public string SenderAddress { get; set; } = "";
    public string SenderMobile { get; set; } = "";
    public string TcatCustomerId { get; set; } = "";
    public string EcpayMerchantId { get; set; } = "";
}

public sealed class Product
{
    public string Id { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "盒";
    public string Temperature { get; set; } = "ambient";
    public string ShortName { get; set; } = "";
}

public sealed class Customer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string StoreName { get; set; } = "";
}

public sealed class Order
{
    public string Id { get; set; } = "";
    public string CustomerId { get; set; } = "";
    public Customer Customer { get; set; } = null!;
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public sealed class OrderLine
{
    public string Id { get; set; } = "";
    public string OrderId { get; set; } = "";
    public Order Order { get; set; } = null!;
    public string ProductId { get; set; } = "";
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public sealed class Shipment
{
    public string Id { get; set; } = "";
    public string OrderId { get; set; } = "";
    public Order Order { get; set; } = null!;
    public string Channel { get; set; } = "";
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string RecipientAddress { get; set; } = "";
    public string Temperature { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string StoreName { get; set; } = "";
    public bool Succeeded { get; set; }
    public string Status { get; set; } = "created";
    public DateTime CreatedAt { get; set; }
    public string TcatWaybillNo { get; set; } = "";
    public string TcatPrintCode { get; set; } = "";
    public string PdfObjectKey { get; set; } = "";
    public string LogisticsId { get; set; } = "";
    public string CvsPaymentNo { get; set; } = "";
    public string CvsValidationNo { get; set; } = "";
    public string CvsCode { get; set; } = "";
    public List<ShipmentLine> Lines { get; set; } = [];
}

public sealed class ShipmentLine
{
    public string Id { get; set; } = "";
    public string ShipmentId { get; set; } = "";
    public Shipment Shipment { get; set; } = null!;
    public string OrderLineId { get; set; } = "";
    public int Quantity { get; set; }
}

public sealed class StockMovement
{
    public string Id { get; set; } = "";
    public string ProductId { get; set; } = "";
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string? ShipmentId { get; set; }
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
