namespace Construction.Domain.Entities;

public class Worker
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Trade { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<ProjectAssignment> Assignments { get; set; } = new List<ProjectAssignment>();
}

public class ProjectAssignment
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal DailyRate { get; set; }
}
