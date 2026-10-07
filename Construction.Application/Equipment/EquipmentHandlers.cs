using Construction.Application.Cement;
using Construction.Application.Common;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Rentals;

public record ListPublicEquipmentQuery : IRequest<ApiResult<IReadOnlyList<EquipmentDto>>>;
public record GetPublicEquipmentQuery(Guid Id) : IRequest<ApiResult<EquipmentDto>>;
public record RequestRentalCommand(RequestRentalRequest Request) : IRequest<ApiResult<RentalDto>>;
public record ListMyRentalsQuery(string? Status) : IRequest<ApiResult<IReadOnlyList<RentalDto>>>;
public record GetMyRentalQuery(Guid Id) : IRequest<ApiResult<RentalDto>>;

public record ListAdminEquipmentQuery : IRequest<ApiResult<IReadOnlyList<EquipmentDto>>>;
public record CreateEquipmentCommand(CreateEquipmentRequest Request) : IRequest<ApiResult<EquipmentDto>>;
public record UpdateEquipmentCommand(Guid Id, UpdateEquipmentRequest Request) : IRequest<ApiResult<EquipmentDto>>;
public record UpdateEquipmentStatusCommand(Guid Id, UpdateEquipmentStatusRequest Request) : IRequest<ApiResult<EquipmentDto>>;
public record ListAdminRentalsQuery(string? Status) : IRequest<ApiResult<IReadOnlyList<RentalDto>>>;
public record GetAdminRentalQuery(Guid Id) : IRequest<ApiResult<RentalDto>>;
public record ApproveRentalCommand(Guid Id) : IRequest<ApiResult<RentalDto>>;
public record ReturnRentalCommand(Guid Id, ReturnRentalRequest Request) : IRequest<ApiResult<RentalDto>>;
public record CancelRentalCommand(Guid Id) : IRequest<ApiResult<RentalDto>>;
public record UpdateRentalPaymentCommand(Guid Id, UpdatePaymentRequest Request) : IRequest<ApiResult<RentalDto>>;
public record AttachRentalProjectCommand(Guid Id, AttachProjectRequest Request) : IRequest<ApiResult<RentalDto>>;

public class RequestRentalValidator : AbstractValidator<RequestRentalCommand>
{
    public RequestRentalValidator()
    {
        RuleFor(x => x.Request.EquipmentId).NotEmpty();
        RuleFor(x => x.Request.SiteAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.ExpectedReturn).GreaterThanOrEqualTo(x => x.Request.StartDate);
        RuleFor(x => x.Request.Deposit).GreaterThanOrEqualTo(0);
    }
}

public class CreateEquipmentValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Request.DailyRate).GreaterThan(0);
    }
}

public class UpdateEquipmentValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Request.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Request.DailyRate).GreaterThan(0);
    }
}

public static class EquipmentMaps
{
    public static EquipmentDto ToDto(Domain.Entities.Equipment equipment) =>
        new(equipment.Id, equipment.Name, equipment.Code, equipment.DailyRate, equipment.Status);

    public static RentalDto ToDto(EquipmentRental rental) => new(
        rental.Id,
        rental.ClientId,
        rental.Client.Name,
        rental.EquipmentId,
        rental.Equipment.Name,
        rental.ProjectId,
        rental.Project?.Name,
        rental.StartDate,
        rental.ExpectedReturn,
        rental.ActualReturn,
        rental.SiteAddress,
        rental.Deposit,
        rental.DailyRate,
        rental.Total,
        rental.Status,
        rental.PaymentStatus,
        rental.CreatedAt);

    public static IQueryable<EquipmentRental> WithDetails(IQueryable<EquipmentRental> query) =>
        query.Include(rental => rental.Client)
            .Include(rental => rental.Equipment)
            .Include(rental => rental.Project);
}

public class ListPublicEquipmentHandler : IRequestHandler<ListPublicEquipmentQuery, ApiResult<IReadOnlyList<EquipmentDto>>>
{
    private readonly IAppDbContext _db;

    public ListPublicEquipmentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<EquipmentDto>>> Handle(ListPublicEquipmentQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.Equipment
            .Where(item => item.Status != EquipmentStatus.Maintenance)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<EquipmentDto>>.Ok(items.Select(EquipmentMaps.ToDto).ToList());
    }
}

public class GetPublicEquipmentHandler : IRequestHandler<GetPublicEquipmentQuery, ApiResult<EquipmentDto>>
{
    private readonly IAppDbContext _db;

    public GetPublicEquipmentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<EquipmentDto>> Handle(GetPublicEquipmentQuery request, CancellationToken cancellationToken)
    {
        var item = await _db.Equipment.FirstOrDefaultAsync(
            equipment => equipment.Id == request.Id && equipment.Status != EquipmentStatus.Maintenance,
            cancellationToken);
        return item is null
            ? ApiResult<EquipmentDto>.Fail("Equipment was not found.", ApiErrorKind.NotFound)
            : ApiResult<EquipmentDto>.Ok(EquipmentMaps.ToDto(item));
    }
}

public class RequestRentalHandler : IRequestHandler<RequestRentalCommand, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RequestRentalHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResult<RentalDto>> Handle(RequestRentalCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ApiResult<RentalDto>.Fail("Sign in is required.", ApiErrorKind.Unauthorized);
        }

        var equipment = await _db.Equipment.FirstOrDefaultAsync(item => item.Id == command.Request.EquipmentId, cancellationToken);
        if (equipment is null || equipment.Status == EquipmentStatus.Maintenance)
        {
            return ApiResult<RentalDto>.Fail("Equipment was not found.", ApiErrorKind.NotFound);
        }

        if (equipment.Status != EquipmentStatus.Available)
        {
            return ApiResult<RentalDto>.Fail("This equipment is not available to rent.");
        }

        var rental = new EquipmentRental
        {
            Id = Guid.NewGuid(),
            ClientId = _currentUser.UserId.Value,
            EquipmentId = equipment.Id,
            StartDate = command.Request.StartDate,
            ExpectedReturn = command.Request.ExpectedReturn,
            SiteAddress = command.Request.SiteAddress.Trim(),
            Deposit = command.Request.Deposit,
            DailyRate = equipment.DailyRate,
            Total = 0,
            Status = RentalStatus.Requested,
            PaymentStatus = PaymentStatus.Unpaid,
            CreatedAt = DateTime.UtcNow
        };
        _db.EquipmentRentals.Add(rental);
        await _db.SaveChangesAsync(cancellationToken);

        var saved = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstAsync(item => item.Id == rental.Id, cancellationToken);
        return ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(saved), "Rental requested.");
    }
}

public class ListMyRentalsHandler : IRequestHandler<ListMyRentalsQuery, ApiResult<IReadOnlyList<RentalDto>>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMyRentalsHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResult<IReadOnlyList<RentalDto>>> Handle(ListMyRentalsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ApiResult<IReadOnlyList<RentalDto>>.Fail("Sign in is required.", ApiErrorKind.Unauthorized);
        }

        var parsed = StatusParser.TryParse<RentalStatus>(request.Status, out var status, out var error);
        if (!parsed)
        {
            return ApiResult<IReadOnlyList<RentalDto>>.Fail(error!);
        }

        var query = EquipmentMaps.WithDetails(_db.EquipmentRentals).Where(rental => rental.ClientId == _currentUser.UserId);
        if (status.HasValue)
        {
            query = query.Where(rental => rental.Status == status.Value);
        }

        var rentals = await query.OrderByDescending(rental => rental.CreatedAt).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<RentalDto>>.Ok(rentals.Select(EquipmentMaps.ToDto).ToList());
    }
}

public class GetMyRentalHandler : IRequestHandler<GetMyRentalQuery, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyRentalHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ApiResult<RentalDto>> Handle(GetMyRentalQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
        {
            return ApiResult<RentalDto>.Fail("Sign in is required.", ApiErrorKind.Unauthorized);
        }

        var rental = await EquipmentMaps.WithDetails(_db.EquipmentRentals)
            .FirstOrDefaultAsync(item => item.Id == request.Id && item.ClientId == _currentUser.UserId, cancellationToken);
        return rental is null
            ? ApiResult<RentalDto>.Fail("Rental was not found.", ApiErrorKind.NotFound)
            : ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(rental));
    }
}

public class ListAdminEquipmentHandler : IRequestHandler<ListAdminEquipmentQuery, ApiResult<IReadOnlyList<EquipmentDto>>>
{
    private readonly IAppDbContext _db;

    public ListAdminEquipmentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<EquipmentDto>>> Handle(ListAdminEquipmentQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.Equipment.OrderBy(item => item.Name).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<EquipmentDto>>.Ok(items.Select(EquipmentMaps.ToDto).ToList());
    }
}

public class CreateEquipmentHandler : IRequestHandler<CreateEquipmentCommand, ApiResult<EquipmentDto>>
{
    private readonly IAppDbContext _db;

    public CreateEquipmentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<EquipmentDto>> Handle(CreateEquipmentCommand command, CancellationToken cancellationToken)
    {
        var code = command.Request.Code.Trim().ToUpperInvariant();
        var exists = await _db.Equipment.AnyAsync(item => item.Code == code, cancellationToken);
        if (exists)
        {
            return ApiResult<EquipmentDto>.Fail("Equipment code is already in use.");
        }

        var equipment = new Domain.Entities.Equipment
        {
            Id = Guid.NewGuid(),
            Name = command.Request.Name.Trim(),
            Code = code,
            DailyRate = command.Request.DailyRate,
            Status = EquipmentStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
        _db.Equipment.Add(equipment);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<EquipmentDto>.Ok(EquipmentMaps.ToDto(equipment));
    }
}

public class UpdateEquipmentHandler : IRequestHandler<UpdateEquipmentCommand, ApiResult<EquipmentDto>>
{
    private readonly IAppDbContext _db;

    public UpdateEquipmentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<EquipmentDto>> Handle(UpdateEquipmentCommand command, CancellationToken cancellationToken)
    {
        var equipment = await _db.Equipment.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (equipment is null)
        {
            return ApiResult<EquipmentDto>.Fail("Equipment was not found.", ApiErrorKind.NotFound);
        }

        var code = command.Request.Code.Trim().ToUpperInvariant();
        var exists = await _db.Equipment.AnyAsync(item => item.Code == code && item.Id != equipment.Id, cancellationToken);
        if (exists)
        {
            return ApiResult<EquipmentDto>.Fail("Equipment code is already in use.");
        }

        equipment.Name = command.Request.Name.Trim();
        equipment.Code = code;
        equipment.DailyRate = command.Request.DailyRate;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<EquipmentDto>.Ok(EquipmentMaps.ToDto(equipment));
    }
}

public class UpdateEquipmentStatusHandler : IRequestHandler<UpdateEquipmentStatusCommand, ApiResult<EquipmentDto>>
{
    private readonly IAppDbContext _db;

    public UpdateEquipmentStatusHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<EquipmentDto>> Handle(UpdateEquipmentStatusCommand command, CancellationToken cancellationToken)
    {
        var equipment = await _db.Equipment.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (equipment is null)
        {
            return ApiResult<EquipmentDto>.Fail("Equipment was not found.", ApiErrorKind.NotFound);
        }

        if (command.Request.Status is not (EquipmentStatus.Available or EquipmentStatus.Maintenance))
        {
            return ApiResult<EquipmentDto>.Fail("Status can be set to Available or Maintenance. Rented is set by an active rental.");
        }

        if (equipment.Status == EquipmentStatus.Rented)
        {
            return ApiResult<EquipmentDto>.Fail("Return the active rental before changing this equipment.");
        }

        equipment.Status = command.Request.Status;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<EquipmentDto>.Ok(EquipmentMaps.ToDto(equipment));
    }
}

public class ListAdminRentalsHandler : IRequestHandler<ListAdminRentalsQuery, ApiResult<IReadOnlyList<RentalDto>>>
{
    private readonly IAppDbContext _db;

    public ListAdminRentalsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<RentalDto>>> Handle(ListAdminRentalsQuery request, CancellationToken cancellationToken)
    {
        var parsed = StatusParser.TryParse<RentalStatus>(request.Status, out var status, out var error);
        if (!parsed)
        {
            return ApiResult<IReadOnlyList<RentalDto>>.Fail(error!);
        }

        var query = EquipmentMaps.WithDetails(_db.EquipmentRentals);
        if (status.HasValue)
        {
            query = query.Where(rental => rental.Status == status.Value);
        }

        var rentals = await query.OrderByDescending(rental => rental.CreatedAt).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<RentalDto>>.Ok(rentals.Select(EquipmentMaps.ToDto).ToList());
    }
}

public class GetAdminRentalHandler : IRequestHandler<GetAdminRentalQuery, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;

    public GetAdminRentalHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<RentalDto>> Handle(GetAdminRentalQuery request, CancellationToken cancellationToken)
    {
        var rental = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        return rental is null
            ? ApiResult<RentalDto>.Fail("Rental was not found.", ApiErrorKind.NotFound)
            : ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(rental));
    }
}

public class ApproveRentalHandler : IRequestHandler<ApproveRentalCommand, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;

    public ApproveRentalHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<RentalDto>> Handle(ApproveRentalCommand command, CancellationToken cancellationToken)
    {
        var rental = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (rental is null)
        {
            return ApiResult<RentalDto>.Fail("Rental was not found.", ApiErrorKind.NotFound);
        }

        if (rental.Status != RentalStatus.Requested)
        {
            return ApiResult<RentalDto>.Fail("Only a requested rental can be approved.");
        }

        if (rental.Equipment.Status != EquipmentStatus.Available)
        {
            return ApiResult<RentalDto>.Fail("This equipment is not available.");
        }

        rental.Status = RentalStatus.Active;
        rental.Equipment.Status = EquipmentStatus.Rented;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(rental));
    }
}

public class ReturnRentalHandler : IRequestHandler<ReturnRentalCommand, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;

    public ReturnRentalHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<RentalDto>> Handle(ReturnRentalCommand command, CancellationToken cancellationToken)
    {
        var rental = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (rental is null)
        {
            return ApiResult<RentalDto>.Fail("Rental was not found.", ApiErrorKind.NotFound);
        }

        if (rental.Status != RentalStatus.Active)
        {
            return ApiResult<RentalDto>.Fail("Only an active rental can be returned.");
        }

        var actualReturn = command.Request.ActualReturn ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (actualReturn < rental.StartDate)
        {
            return ApiResult<RentalDto>.Fail("Return date cannot be before the rental start date.");
        }

        var days = actualReturn.DayNumber - rental.StartDate.DayNumber;
        if (days < 1)
        {
            days = 1;
        }

        rental.ActualReturn = actualReturn;
        rental.Total = rental.DailyRate * days;
        rental.Status = RentalStatus.Returned;
        rental.Equipment.Status = EquipmentStatus.Available;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(rental));
    }
}

public class CancelRentalHandler : IRequestHandler<CancelRentalCommand, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;

    public CancelRentalHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<RentalDto>> Handle(CancelRentalCommand command, CancellationToken cancellationToken)
    {
        var rental = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (rental is null)
        {
            return ApiResult<RentalDto>.Fail("Rental was not found.", ApiErrorKind.NotFound);
        }

        if (rental.Status is RentalStatus.Returned or RentalStatus.Cancelled)
        {
            return ApiResult<RentalDto>.Fail("This rental cannot be cancelled.");
        }

        if (rental.Status == RentalStatus.Active)
        {
            rental.Equipment.Status = EquipmentStatus.Available;
        }

        rental.Status = RentalStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(rental));
    }
}

public class UpdateRentalPaymentHandler : IRequestHandler<UpdateRentalPaymentCommand, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;

    public UpdateRentalPaymentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<RentalDto>> Handle(UpdateRentalPaymentCommand command, CancellationToken cancellationToken)
    {
        var rental = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (rental is null)
        {
            return ApiResult<RentalDto>.Fail("Rental was not found.", ApiErrorKind.NotFound);
        }

        if (!Enum.IsDefined(command.Request.PaymentStatus))
        {
            return ApiResult<RentalDto>.Fail("Payment status is not valid.");
        }

        rental.PaymentStatus = command.Request.PaymentStatus;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(rental));
    }
}

public class AttachRentalProjectHandler : IRequestHandler<AttachRentalProjectCommand, ApiResult<RentalDto>>
{
    private readonly IAppDbContext _db;

    public AttachRentalProjectHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<RentalDto>> Handle(AttachRentalProjectCommand command, CancellationToken cancellationToken)
    {
        var rental = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (rental is null)
        {
            return ApiResult<RentalDto>.Fail("Rental was not found.", ApiErrorKind.NotFound);
        }

        if (command.Request.ProjectId is Guid projectId)
        {
            var exists = await _db.Projects.AnyAsync(project => project.Id == projectId, cancellationToken);
            if (!exists)
            {
                return ApiResult<RentalDto>.Fail("Project was not found.", ApiErrorKind.NotFound);
            }

            rental.ProjectId = projectId;
        }
        else
        {
            rental.ProjectId = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var saved = await EquipmentMaps.WithDetails(_db.EquipmentRentals).FirstAsync(item => item.Id == rental.Id, cancellationToken);
        return ApiResult<RentalDto>.Ok(EquipmentMaps.ToDto(saved));
    }
}
