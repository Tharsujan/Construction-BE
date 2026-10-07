using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Common;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<ProductUnit> ProductUnits { get; }
    DbSet<CementProduct> CementProducts { get; }
    DbSet<CementOrder> CementOrders { get; }
    DbSet<CementOrderItem> CementOrderItems { get; }
    DbSet<Project> Projects { get; }
    DbSet<Worker> Workers { get; }
    DbSet<ProjectAssignment> ProjectAssignments { get; }
    DbSet<Equipment> Equipment { get; }
    DbSet<EquipmentRental> EquipmentRentals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
