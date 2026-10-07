using Construction.Domain.Enums;

namespace Construction.Application.Rentals;

public record EquipmentDto(Guid Id, string Name, string Code, decimal DailyRate, EquipmentStatus Status);

public record CreateEquipmentRequest(string Name, string Code, decimal DailyRate);

public record UpdateEquipmentRequest(string Name, string Code, decimal DailyRate);

public record UpdateEquipmentStatusRequest(EquipmentStatus Status);

public record RequestRentalRequest(Guid EquipmentId, DateOnly StartDate, DateOnly ExpectedReturn, string SiteAddress, decimal Deposit);

public record RentalDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    Guid EquipmentId,
    string EquipmentName,
    Guid? ProjectId,
    string? ProjectName,
    DateOnly StartDate,
    DateOnly ExpectedReturn,
    DateOnly? ActualReturn,
    string SiteAddress,
    decimal Deposit,
    decimal DailyRate,
    decimal Total,
    RentalStatus Status,
    PaymentStatus PaymentStatus,
    DateTime CreatedAt);

public record ReturnRentalRequest(DateOnly? ActualReturn);
