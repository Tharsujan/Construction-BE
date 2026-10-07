using Construction.Application.Common;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Construction.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Site Admin",
            Email = "admin@construction.local",
            Phone = "0770000001",
            PasswordHash = passwords.Hash("Admin@12345"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow
        };
        var client = new User
        {
            Id = Guid.NewGuid(),
            Name = "Sample Client",
            Email = "client@construction.local",
            Phone = "0770000002",
            PasswordHash = passwords.Hash("Client@12345"),
            Role = UserRole.Client,
            CreatedAt = DateTime.UtcNow
        };

        var bag = await db.ProductUnits.FirstOrDefaultAsync(unit => unit.Name == "Bag");
        var ton = await db.ProductUnits.FirstOrDefaultAsync(unit => unit.Name == "Ton");
        if (bag is null)
        {
            bag = new ProductUnit
            {
                Id = Guid.NewGuid(),
                Name = "Bag",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.ProductUnits.Add(bag);
        }

        if (ton is null)
        {
            ton = new ProductUnit
            {
                Id = Guid.NewGuid(),
                Name = "Ton",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.ProductUnits.Add(ton);
        }

        var opc43 = new CementProduct
        {
            Id = Guid.NewGuid(),
            ImageUrl = string.Empty,
            Name = "OPC 43 Grade",
            UnitId = bag.Id,
            Price = 1250,
            StockQuantity = 500,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var opc53 = new CementProduct
        {
            Id = Guid.NewGuid(),
            ImageUrl = string.Empty,
            Name = "OPC 53 Grade",
            UnitId = bag.Id,
            Price = 1400,
            StockQuantity = 300,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var white = new CementProduct
        {
            Id = Guid.NewGuid(),
            ImageUrl = string.Empty,
            Name = "White Cement",
            UnitId = bag.Id,
            Price = 2100,
            StockQuantity = 8,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var upcoming = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Colombo Housing Block A",
            Summary = "Upcoming residential block on Baseline Road.",
            SiteAddress = "Baseline Road, Colombo",
            Description = "Foundation and structural work for a four-storey housing block.",
            Status = ProjectStatus.Planned,
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2027, 4, 30),
            IsPublic = true,
            CreatedAt = DateTime.UtcNow
        };
        var ongoing = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Kandy Road Bridge Repair",
            Summary = "Ongoing bridge repair on Kandy Road.",
            SiteAddress = "Kandy Road, Kadawatha",
            Description = "Deck repair and approach road work while traffic is managed.",
            Status = ProjectStatus.InProgress,
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 15),
            IsPublic = true,
            CreatedAt = DateTime.UtcNow
        };

        var mason = new Worker
        {
            Id = Guid.NewGuid(),
            Name = "Ravi Perera",
            Trade = "Mason",
            Phone = "0771000001",
            DailyRate = 4500,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var helper = new Worker
        {
            Id = Guid.NewGuid(),
            Name = "Kumar Silva",
            Trade = "Helper",
            Phone = "0771000002",
            DailyRate = 2500,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var electrician = new Worker
        {
            Id = Guid.NewGuid(),
            Name = "Nimal Fernando",
            Trade = "Electrician",
            Phone = "0771000003",
            DailyRate = 5000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = ongoing.Id,
            WorkerId = mason.Id,
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 12, 1),
            DailyRate = mason.DailyRate
        };

        var mixer = new Equipment
        {
            Id = Guid.NewGuid(),
            Name = "Concrete Mixer",
            Code = "CM-01",
            DailyRate = 3500,
            Status = EquipmentStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        var vibrator = new Equipment
        {
            Id = Guid.NewGuid(),
            Name = "Concrete Vibrator",
            Code = "VB-02",
            DailyRate = 1800,
            Status = EquipmentStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        var scaffold = new Equipment
        {
            Id = Guid.NewGuid(),
            Name = "Scaffolding Set",
            Code = "SC-03",
            DailyRate = 2200,
            Status = EquipmentStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.AddRange(admin, client);
        db.ProductUnits.AddRange(bag, ton);
        db.CementProducts.AddRange(opc43, opc53, white);
        db.Projects.AddRange(upcoming, ongoing);
        db.Workers.AddRange(mason, helper, electrician);
        db.ProjectAssignments.Add(assignment);
        db.Equipment.AddRange(mixer, vibrator, scaffold);
        await db.SaveChangesAsync();
    }
}
