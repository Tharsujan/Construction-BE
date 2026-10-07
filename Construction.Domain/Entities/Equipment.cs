using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

public class Equipment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public EquipmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<EquipmentRental> Rentals { get; set; } = new List<EquipmentRental>();
}

public class EquipmentRental
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public User Client { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly ExpectedReturn { get; set; }
    public DateOnly? ActualReturn { get; set; }
    public string SiteAddress { get; set; } = string.Empty;
    public decimal Deposit { get; set; }
    public decimal DailyRate { get; set; }
    public decimal Total { get; set; }
    public RentalStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
