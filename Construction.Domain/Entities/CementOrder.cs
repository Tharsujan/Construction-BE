using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class CementOrder
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public User Client { get; set; } = null!;
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal Total { get; set; }
    public bool StockDeducted { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CementOrderItem> Items { get; set; } = new List<CementOrderItem>();
}

public class CementOrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public CementOrder Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public CementProduct Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
