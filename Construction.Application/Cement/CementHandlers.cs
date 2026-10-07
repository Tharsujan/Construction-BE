using Construction.Application.Common;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Cement;

public record ListPublicProductsQuery : IRequest<ApiResult<IReadOnlyList<CementProductDto>>>;
public record GetPublicProductQuery(Guid Id) : IRequest<ApiResult<CementProductDto>>;
public record PlaceCementOrderCommand(PlaceCementOrderRequest Request) : IRequest<ApiResult<CementOrderDto>>;
public record ListMyOrdersQuery(string? Status) : IRequest<ApiResult<IReadOnlyList<CementOrderDto>>>;
public record GetMyOrderQuery(Guid Id) : IRequest<ApiResult<CementOrderDto>>;

public record ListAdminProductsQuery : IRequest<ApiResult<IReadOnlyList<CementProductDto>>>;
public record CreateCementProductCommand(CreateCementProductRequest Request) : IRequest<ApiResult<CementProductDto>>;
public record UpdateCementProductCommand(Guid Id, UpdateCementProductRequest Request) : IRequest<ApiResult<CementProductDto>>;
public record AdjustStockCommand(Guid Id, AdjustStockRequest Request) : IRequest<ApiResult<CementProductDto>>;
public record ListAdminOrdersQuery(string? Status) : IRequest<ApiResult<IReadOnlyList<CementOrderDto>>>;
public record GetAdminOrderQuery(Guid Id) : IRequest<ApiResult<CementOrderDto>>;
public record UpdateOrderStatusCommand(Guid Id, UpdateOrderStatusRequest Request) : IRequest<ApiResult<CementOrderDto>>;
public record UpdateOrderPaymentCommand(Guid Id, UpdatePaymentRequest Request) : IRequest<ApiResult<CementOrderDto>>;
public record AttachOrderProjectCommand(Guid Id, AttachProjectRequest Request) : IRequest<ApiResult<CementOrderDto>>;

public class PlaceCementOrderValidator : AbstractValidator<PlaceCementOrderCommand>
{
    public PlaceCementOrderValidator()
    {
        RuleFor(x => x.Request.DeliveryAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId).NotEmpty();
            item.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public class CreateCementProductValidator : AbstractValidator<CreateCementProductCommand>
{
    public CreateCementProductValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Request.ImageUrl).MaximumLength(500);
        RuleFor(x => x.Request.UnitId).NotEmpty();
        RuleFor(x => x.Request.Price).GreaterThan(0);
    }
}

public class UpdateCementProductValidator : AbstractValidator<UpdateCementProductCommand>
{
    public UpdateCementProductValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Request.ImageUrl).MaximumLength(500);
        RuleFor(x => x.Request.UnitId).NotEmpty();
        RuleFor(x => x.Request.Price).GreaterThan(0);
    }
}

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.Request).Must(request => request.StockQuantity.HasValue ^ request.Adjustment.HasValue)
            .WithMessage("Provide either stockQuantity or adjustment.");
    }
}

public static class CementMaps
{
    public static CementProductDto ToDto(CementProduct product) => new(
        product.Id,
        product.ImageUrl,
        product.Name,
        product.UnitId,
        product.Unit.Name,
        product.Price,
        product.StockQuantity,
        product.IsActive);

    public static IQueryable<CementProduct> WithUnit(IQueryable<CementProduct> query) =>
        query.Include(product => product.Unit);

    public static CementOrderDto ToDto(CementOrder order) => new(
        order.Id,
        order.ClientId,
        order.Client.Name,
        order.ProjectId,
        order.Project?.Name,
        order.DeliveryAddress,
        order.Status,
        order.PaymentStatus,
        order.Total,
        order.CreatedAt,
        order.Items.Select(item => new CementOrderItemDto(
            item.ProductId,
            item.Product.Name,
            item.Quantity,
            item.UnitPrice,
            item.Quantity * item.UnitPrice)).ToList());

    public static IQueryable<CementOrder> WithDetails(IQueryable<CementOrder> query) =>
        query.Include(order => order.Client)
            .Include(order => order.Project)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product);
}

public class ListPublicProductsHandler : IRequestHandler<ListPublicProductsQuery, ApiResult<IReadOnlyList<CementProductDto>>>
{
    private readonly IAppDbContext _db;

    public ListPublicProductsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<CementProductDto>>> Handle(ListPublicProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await CementMaps.WithUnit(_db.CementProducts)
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<CementProductDto>>.Ok(products.Select(CementMaps.ToDto).ToList());
    }
}

public class GetPublicProductHandler : IRequestHandler<GetPublicProductQuery, ApiResult<CementProductDto>>
{
    private readonly IAppDbContext _db;

    public GetPublicProductHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementProductDto>> Handle(GetPublicProductQuery request, CancellationToken cancellationToken)
    {
        var product = await CementMaps.WithUnit(_db.CementProducts)
            .FirstOrDefaultAsync(item => item.Id == request.Id && item.IsActive, cancellationToken);
        return product is null
            ? ApiResult<CementProductDto>.Fail("Product was not found.", ApiErrorKind.NotFound)
            : ApiResult<CementProductDto>.Ok(CementMaps.ToDto(product));
    }
}

public class PlaceCementOrderHandler : IRequestHandler<PlaceCementOrderCommand, ApiResult<CementOrderDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public PlaceCementOrderHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResult<CementOrderDto>> Handle(PlaceCementOrderCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ApiResult<CementOrderDto>.Fail("Sign in is required.", ApiErrorKind.Unauthorized);
        }

        var productIds = command.Request.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _db.CementProducts.Where(product => productIds.Contains(product.Id)).ToListAsync(cancellationToken);
        if (products.Count != productIds.Count || products.Any(product => !product.IsActive))
        {
            return ApiResult<CementOrderDto>.Fail("One or more products are not available.");
        }

        var quantities = command.Request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        foreach (var product in products)
        {
            if (product.StockQuantity < quantities[product.Id])
            {
                return ApiResult<CementOrderDto>.Fail($"{product.Name} does not have enough stock.");
            }
        }

        var order = new CementOrder
        {
            Id = Guid.NewGuid(),
            ClientId = _currentUser.UserId.Value,
            DeliveryAddress = command.Request.DeliveryAddress.Trim(),
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (productId, quantity) in quantities)
        {
            var product = products.First(item => item.Id == productId);
            order.Items.Add(new CementOrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }

        order.Total = order.Items.Sum(item => item.Quantity * item.UnitPrice);
        _db.CementOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        var saved = await CementMaps.WithDetails(_db.CementOrders).FirstAsync(item => item.Id == order.Id, cancellationToken);
        return ApiResult<CementOrderDto>.Ok(CementMaps.ToDto(saved), "Order placed.");
    }
}

public class ListMyOrdersHandler : IRequestHandler<ListMyOrdersQuery, ApiResult<IReadOnlyList<CementOrderDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMyOrdersHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResult<IReadOnlyList<CementOrderDto>>> Handle(ListMyOrdersQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ApiResult<IReadOnlyList<CementOrderDto>>.Fail("Sign in is required.", ApiErrorKind.Unauthorized);
        }

        var parsed = StatusParser.TryParse<OrderStatus>(request.Status, out var status, out var error);
        if (!parsed)
        {
            return ApiResult<IReadOnlyList<CementOrderDto>>.Fail(error!);
        }

        var query = CementMaps.WithDetails(_db.CementOrders).Where(order => order.ClientId == _currentUser.UserId);
        if (status.HasValue)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        var orders = await query.OrderByDescending(order => order.CreatedAt).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<CementOrderDto>>.Ok(orders.Select(CementMaps.ToDto).ToList());
    }
}

public class GetMyOrderHandler : IRequestHandler<GetMyOrderQuery, ApiResult<CementOrderDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyOrderHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResult<CementOrderDto>> Handle(GetMyOrderQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ApiResult<CementOrderDto>.Fail("Sign in is required.", ApiErrorKind.Unauthorized);
        }

        var order = await CementMaps.WithDetails(_db.CementOrders)
            .FirstOrDefaultAsync(item => item.Id == request.Id && item.ClientId == _currentUser.UserId, cancellationToken);
        return order is null
            ? ApiResult<CementOrderDto>.Fail("Order was not found.", ApiErrorKind.NotFound)
            : ApiResult<CementOrderDto>.Ok(CementMaps.ToDto(order));
    }
}

public class ListAdminProductsHandler : IRequestHandler<ListAdminProductsQuery, ApiResult<IReadOnlyList<CementProductDto>>>
{
    private readonly IAppDbContext _db;

    public ListAdminProductsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<CementProductDto>>> Handle(ListAdminProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await CementMaps.WithUnit(_db.CementProducts).OrderBy(product => product.Name).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<CementProductDto>>.Ok(products.Select(CementMaps.ToDto).ToList());
    }
}

public class CreateCementProductHandler : IRequestHandler<CreateCementProductCommand, ApiResult<CementProductDto>>
{
    private readonly IAppDbContext _db;

    public CreateCementProductHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementProductDto>> Handle(CreateCementProductCommand command, CancellationToken cancellationToken)
    {
        var unit = await _db.ProductUnits.FirstOrDefaultAsync(item => item.Id == command.Request.UnitId, cancellationToken);
        if (unit is null)
        {
            return ApiResult<CementProductDto>.Fail("Unit was not found.", ApiErrorKind.NotFound);
        }

        if (!unit.IsActive)
        {
            return ApiResult<CementProductDto>.Fail("Choose an active unit.");
        }

        var product = new CementProduct
        {
            Id = Guid.NewGuid(),
            ImageUrl = command.Request.ImageUrl?.Trim() ?? string.Empty,
            Name = command.Request.Name.Trim(),
            UnitId = unit.Id,
            Unit = unit,
            Price = command.Request.Price,
            StockQuantity = 0,
            IsActive = command.Request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _db.CementProducts.Add(product);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<CementProductDto>.Ok(CementMaps.ToDto(product));
    }
}

public class UpdateCementProductHandler : IRequestHandler<UpdateCementProductCommand, ApiResult<CementProductDto>>
{
    private readonly IAppDbContext _db;

    public UpdateCementProductHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementProductDto>> Handle(UpdateCementProductCommand command, CancellationToken cancellationToken)
    {
        var product = await CementMaps.WithUnit(_db.CementProducts).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return ApiResult<CementProductDto>.Fail("Product was not found.", ApiErrorKind.NotFound);
        }

        var unit = await _db.ProductUnits.FirstOrDefaultAsync(item => item.Id == command.Request.UnitId, cancellationToken);
        if (unit is null)
        {
            return ApiResult<CementProductDto>.Fail("Unit was not found.", ApiErrorKind.NotFound);
        }

        if (!unit.IsActive && product.UnitId != unit.Id)
        {
            return ApiResult<CementProductDto>.Fail("Choose an active unit.");
        }

        product.ImageUrl = command.Request.ImageUrl?.Trim() ?? string.Empty;
        product.Name = command.Request.Name.Trim();
        product.UnitId = unit.Id;
        product.Unit = unit;
        product.Price = command.Request.Price;
        product.IsActive = command.Request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<CementProductDto>.Ok(CementMaps.ToDto(product));
    }
}

public class AdjustStockHandler : IRequestHandler<AdjustStockCommand, ApiResult<CementProductDto>>
{
    private readonly IAppDbContext _db;

    public AdjustStockHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementProductDto>> Handle(AdjustStockCommand command, CancellationToken cancellationToken)
    {
        var product = await CementMaps.WithUnit(_db.CementProducts).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (product is null)
        {
            return ApiResult<CementProductDto>.Fail("Product was not found.", ApiErrorKind.NotFound);
        }

        var next = command.Request.StockQuantity ?? product.StockQuantity + command.Request.Adjustment!.Value;
        if (next < 0)
        {
            return ApiResult<CementProductDto>.Fail("Stock cannot be negative.");
        }

        product.StockQuantity = next;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<CementProductDto>.Ok(CementMaps.ToDto(product));
    }
}

public class ListAdminOrdersHandler : IRequestHandler<ListAdminOrdersQuery, ApiResult<IReadOnlyList<CementOrderDto>>>
{
    private readonly IAppDbContext _db;

    public ListAdminOrdersHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<CementOrderDto>>> Handle(ListAdminOrdersQuery request, CancellationToken cancellationToken)
    {
        var parsed = StatusParser.TryParse<OrderStatus>(request.Status, out var status, out var error);
        if (!parsed)
        {
            return ApiResult<IReadOnlyList<CementOrderDto>>.Fail(error!);
        }

        var query = CementMaps.WithDetails(_db.CementOrders);
        if (status.HasValue)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        var orders = await query.OrderByDescending(order => order.CreatedAt).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<CementOrderDto>>.Ok(orders.Select(CementMaps.ToDto).ToList());
    }
}

public class GetAdminOrderHandler : IRequestHandler<GetAdminOrderQuery, ApiResult<CementOrderDto>>
{
    private readonly IAppDbContext _db;

    public GetAdminOrderHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementOrderDto>> Handle(GetAdminOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await CementMaps.WithDetails(_db.CementOrders).FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        return order is null
            ? ApiResult<CementOrderDto>.Fail("Order was not found.", ApiErrorKind.NotFound)
            : ApiResult<CementOrderDto>.Ok(CementMaps.ToDto(order));
    }
}

public class UpdateOrderStatusHandler : IRequestHandler<UpdateOrderStatusCommand, ApiResult<CementOrderDto>>
{
    private readonly IAppDbContext _db;

    public UpdateOrderStatusHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementOrderDto>> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await CementMaps.WithDetails(_db.CementOrders).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (order is null)
        {
            return ApiResult<CementOrderDto>.Fail("Order was not found.", ApiErrorKind.NotFound);
        }

        if (!CanTransition(order.Status, command.Request.Status))
        {
            return ApiResult<CementOrderDto>.Fail($"Cannot move an order from {order.Status} to {command.Request.Status}.");
        }

        if (command.Request.Status == OrderStatus.Confirmed)
        {
            foreach (var item in order.Items)
            {
                if (item.Product.StockQuantity < item.Quantity)
                {
                    return ApiResult<CementOrderDto>.Fail($"{item.Product.Name} does not have enough stock to confirm this order.");
                }
            }

            foreach (var item in order.Items)
            {
                item.Product.StockQuantity -= item.Quantity;
            }

            order.StockDeducted = true;
        }

        if (command.Request.Status == OrderStatus.Cancelled && order.StockDeducted)
        {
            foreach (var item in order.Items)
            {
                item.Product.StockQuantity += item.Quantity;
            }

            order.StockDeducted = false;
        }

        order.Status = command.Request.Status;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<CementOrderDto>.Ok(CementMaps.ToDto(order));
    }

    private static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => true,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.OutForDelivery) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        (OrderStatus.OutForDelivery, OrderStatus.Delivered) => true,
        (OrderStatus.OutForDelivery, OrderStatus.Cancelled) => true,
        _ => false
    };
}

public class UpdateOrderPaymentHandler : IRequestHandler<UpdateOrderPaymentCommand, ApiResult<CementOrderDto>>
{
    private readonly IAppDbContext _db;

    public UpdateOrderPaymentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementOrderDto>> Handle(UpdateOrderPaymentCommand command, CancellationToken cancellationToken)
    {
        var order = await CementMaps.WithDetails(_db.CementOrders).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (order is null)
        {
            return ApiResult<CementOrderDto>.Fail("Order was not found.", ApiErrorKind.NotFound);
        }

        if (!Enum.IsDefined(command.Request.PaymentStatus))
        {
            return ApiResult<CementOrderDto>.Fail("Payment status is not valid.");
        }

        order.PaymentStatus = command.Request.PaymentStatus;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<CementOrderDto>.Ok(CementMaps.ToDto(order));
    }
}

public class AttachOrderProjectHandler : IRequestHandler<AttachOrderProjectCommand, ApiResult<CementOrderDto>>
{
    private readonly IAppDbContext _db;

    public AttachOrderProjectHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<CementOrderDto>> Handle(AttachOrderProjectCommand command, CancellationToken cancellationToken)
    {
        var order = await CementMaps.WithDetails(_db.CementOrders).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (order is null)
        {
            return ApiResult<CementOrderDto>.Fail("Order was not found.", ApiErrorKind.NotFound);
        }

        if (command.Request.ProjectId is Guid projectId)
        {
            var projectExists = await _db.Projects.AnyAsync(project => project.Id == projectId, cancellationToken);
            if (!projectExists)
            {
                return ApiResult<CementOrderDto>.Fail("Project was not found.", ApiErrorKind.NotFound);
            }

            order.ProjectId = projectId;
        }
        else
        {
            order.ProjectId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var saved = await CementMaps.WithDetails(_db.CementOrders).FirstAsync(item => item.Id == order.Id, cancellationToken);
        return ApiResult<CementOrderDto>.Ok(CementMaps.ToDto(saved));
    }
}
