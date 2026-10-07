namespace Construction.Domain.Entities;

public class ProductUnit
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<CementProduct> Products { get; set; } = new List<CementProduct>();
}
