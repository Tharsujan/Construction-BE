using Construction.Application.Common;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Stats;

public record LowStockProductDto(Guid Id, string Name, decimal StockQuantity);

public record StatsDto(
    int OpenCementOrders,
    int ActiveProjects,
    int WorkersOnSite,
    int EquipmentOutOnRent,
    IReadOnlyList<LowStockProductDto> LowStockProducts);

public record GetStatsQuery : IRequest<ApiResult<StatsDto>>;

public class GetStatsHandler : IRequestHandler<GetStatsQuery, ApiResult<StatsDto>>
{
    private const decimal LowStockThreshold = 10;
    private readonly IAppDbContext _db;

    public GetStatsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<StatsDto>> Handle(GetStatsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var openOrders = await _db.CementOrders.CountAsync(
            order => order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Cancelled,
            cancellationToken);
        var activeProjects = await _db.Projects.CountAsync(project => project.Status == ProjectStatus.InProgress, cancellationToken);
        var workersOnSite = await _db.ProjectAssignments.CountAsync(
            assignment => assignment.StartDate <= today && assignment.EndDate >= today,
            cancellationToken);
        var equipmentOut = await _db.Equipment.CountAsync(item => item.Status == EquipmentStatus.Rented, cancellationToken);
        var lowStock = await _db.CementProducts
            .Where(product => product.IsActive && product.StockQuantity <= LowStockThreshold)
            .OrderBy(product => product.StockQuantity)
            .Select(product => new LowStockProductDto(product.Id, product.Name, product.StockQuantity))
            .ToListAsync(cancellationToken);

        return ApiResult<StatsDto>.Ok(new StatsDto(openOrders, activeProjects, workersOnSite, equipmentOut, lowStock));
    }
}
