using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SiteAddress { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();
    public ICollection<CementOrder> CementOrders { get; set; } = new List<CementOrder>();
    public ICollection<EquipmentRental> Rentals { get; set; } = new List<EquipmentRental>();
}
