using Construction.Application.Common;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<CementProduct> CementProducts => Set<CementProduct>();
    public DbSet<CementOrder> CementOrders => Set<CementOrder>();
    public DbSet<CementOrderItem> CementOrderItems => Set<CementOrderItem>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Worker> Workers => Set<Worker>();
    public DbSet<ProjectAssignment> ProjectAssignments => Set<ProjectAssignment>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentRental> EquipmentRentals => Set<EquipmentRental>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Name).HasMaxLength(120);
            entity.Property(user => user.Email).HasMaxLength(200);
            entity.Property(user => user.Phone).HasMaxLength(30);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<ProductUnit>(entity =>
        {
            entity.ToTable("Units");
            entity.HasIndex(unit => unit.Name).IsUnique();
            entity.Property(unit => unit.Name).HasMaxLength(40);
        });

        modelBuilder.Entity<CementProduct>(entity =>
        {
            entity.Property(product => product.Name).HasMaxLength(160);
            entity.Property(product => product.ImageUrl).HasMaxLength(500);
            entity.Property(product => product.Price).HasPrecision(18, 2);
            entity.Property(product => product.StockQuantity).HasPrecision(18, 3);
            entity.HasOne(product => product.Unit)
                .WithMany(unit => unit.Products)
                .HasForeignKey(product => product.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CementOrder>(entity =>
        {
            entity.Property(order => order.DeliveryAddress).HasMaxLength(300);
            entity.Property(order => order.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(order => order.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            entity.Property(order => order.Total).HasPrecision(18, 2);
            entity.HasOne(order => order.Client).WithMany().HasForeignKey(order => order.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(order => order.Project).WithMany(project => project.CementOrders).HasForeignKey(order => order.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CementOrderItem>(entity =>
        {
            entity.Property(item => item.Quantity).HasPrecision(18, 3);
            entity.Property(item => item.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(item => item.Order).WithMany(order => order.Items).HasForeignKey(item => item.OrderId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.Product).WithMany(product => product.OrderItems).HasForeignKey(item => item.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(project => project.Name).HasMaxLength(160);
            entity.Property(project => project.Summary).HasMaxLength(300);
            entity.Property(project => project.SiteAddress).HasMaxLength(300);
            entity.Property(project => project.Description).HasMaxLength(2000);
            entity.Property(project => project.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Worker>(entity =>
        {
            entity.Property(worker => worker.Name).HasMaxLength(120);
            entity.Property(worker => worker.Trade).HasMaxLength(80);
            entity.Property(worker => worker.Phone).HasMaxLength(30);
            entity.Property(worker => worker.DailyRate).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProjectAssignment>(entity =>
        {
            entity.Property(assignment => assignment.DailyRate).HasPrecision(18, 2);
            entity.HasOne(assignment => assignment.Project).WithMany(project => project.Assignments).HasForeignKey(assignment => assignment.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(assignment => assignment.Worker).WithMany(worker => worker.Assignments).HasForeignKey(assignment => assignment.WorkerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasIndex(item => item.Code).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(160);
            entity.Property(item => item.Code).HasMaxLength(40);
            entity.Property(item => item.DailyRate).HasPrecision(18, 2);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<EquipmentRental>(entity =>
        {
            entity.Property(rental => rental.SiteAddress).HasMaxLength(300);
            entity.Property(rental => rental.Deposit).HasPrecision(18, 2);
            entity.Property(rental => rental.DailyRate).HasPrecision(18, 2);
            entity.Property(rental => rental.Total).HasPrecision(18, 2);
            entity.Property(rental => rental.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(rental => rental.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(rental => rental.Client).WithMany().HasForeignKey(rental => rental.ClientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rental => rental.Equipment).WithMany(item => item.Rentals).HasForeignKey(rental => rental.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(rental => rental.Project).WithMany(project => project.Rentals).HasForeignKey(rental => rental.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
