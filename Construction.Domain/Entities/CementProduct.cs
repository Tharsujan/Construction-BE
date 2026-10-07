namespace Construction.Domain.Entities;

public class CementProduct
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid UnitId { get; set; }
    public ProductUnit Unit { get; set; } = null!;
    public decimal Price { get; set; }
    public decimal StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CementOrderItem> OrderItems { get; set; } = new List<CementOrderItem>();
}
