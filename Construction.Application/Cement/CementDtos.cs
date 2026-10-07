using Construction.Domain.Enums;

namespace Construction.Application.Cement;

public record CementProductDto(
    Guid Id,
    string ImageUrl,
    string Name,
    Guid UnitId,
    string UnitName,
    decimal Price,
    decimal StockQuantity,
    bool IsActive);

public record OrderItemRequest(Guid ProductId, decimal Quantity);

public record PlaceCementOrderRequest(string DeliveryAddress, IReadOnlyList<OrderItemRequest> Items);

public record CementOrderItemDto(Guid ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public record CementOrderDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid? ProjectId,
    string? ProjectName,
    string DeliveryAddress,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal Total,
    DateTime CreatedAt,
    IReadOnlyList<CementOrderItemDto> Items);

public record CreateCementProductRequest(
    string? ImageUrl,
    string Name,
    Guid UnitId,
    decimal Price,
    bool IsActive);

public record UpdateCementProductRequest(
    string? ImageUrl,
    string Name,
    Guid UnitId,
    decimal Price,
    bool IsActive);

public record AdjustStockRequest(decimal? StockQuantity, decimal? Adjustment);

public record UpdateOrderStatusRequest(OrderStatus Status);

public record UpdatePaymentRequest(PaymentStatus PaymentStatus);

public record AttachProjectRequest(Guid? ProjectId);
