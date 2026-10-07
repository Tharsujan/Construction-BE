using Construction.Application.Common;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.ProductUnits;

public record UnitDto(Guid Id, string Name, bool IsActive);

public record SaveUnitRequest(string Name, bool IsActive);

public record ListUnitsQuery : IRequest<ApiResult<IReadOnlyList<UnitDto>>>;
public record CreateUnitCommand(SaveUnitRequest Request) : IRequest<ApiResult<UnitDto>>;
public record UpdateUnitCommand(Guid Id, SaveUnitRequest Request) : IRequest<ApiResult<UnitDto>>;
public record DeleteUnitCommand(Guid Id) : IRequest<ApiResult<UnitDto>>;

public class SaveUnitValidator : AbstractValidator<SaveUnitRequest>
{
    public SaveUnitValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(40);
    }
}

public class CreateUnitValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitValidator()
    {
        RuleFor(x => x.Request).SetValidator(new SaveUnitValidator());
    }
}

public class UpdateUnitValidator : AbstractValidator<UpdateUnitCommand>
{
    public UpdateUnitValidator()
    {
        RuleFor(x => x.Request).SetValidator(new SaveUnitValidator());
    }
}

public class ListUnitsHandler : IRequestHandler<ListUnitsQuery, ApiResult<IReadOnlyList<UnitDto>>>
{
    private readonly IAppDbContext _db;

    public ListUnitsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<UnitDto>>> Handle(ListUnitsQuery request, CancellationToken cancellationToken)
    {
        var units = await _db.ProductUnits.OrderBy(unit => unit.Name).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<UnitDto>>.Ok(units.Select(ToDto).ToList());
    }

    internal static UnitDto ToDto(ProductUnit unit) => new(unit.Id, unit.Name, unit.IsActive);
}

public class CreateUnitHandler : IRequestHandler<CreateUnitCommand, ApiResult<UnitDto>>
{
    private readonly IAppDbContext _db;

    public CreateUnitHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<UnitDto>> Handle(CreateUnitCommand command, CancellationToken cancellationToken)
    {
        var name = command.Request.Name.Trim();
        var exists = await _db.ProductUnits.AnyAsync(unit => unit.Name.ToLower() == name.ToLower(), cancellationToken);
        if (exists)
        {
            return ApiResult<UnitDto>.Fail("A unit with this name already exists.");
        }

        var unit = new ProductUnit
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = command.Request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        _db.ProductUnits.Add(unit);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<UnitDto>.Ok(ListUnitsHandler.ToDto(unit));
    }
}

public class UpdateUnitHandler : IRequestHandler<UpdateUnitCommand, ApiResult<UnitDto>>
{
    private readonly IAppDbContext _db;

    public UpdateUnitHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<UnitDto>> Handle(UpdateUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await _db.ProductUnits.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (unit is null)
        {
            return ApiResult<UnitDto>.Fail("Unit was not found.", ApiErrorKind.NotFound);
        }

        var name = command.Request.Name.Trim();
        var exists = await _db.ProductUnits.AnyAsync(
            item => item.Id != unit.Id && item.Name.ToLower() == name.ToLower(),
            cancellationToken);
        if (exists)
        {
            return ApiResult<UnitDto>.Fail("A unit with this name already exists.");
        }

        unit.Name = name;
        unit.IsActive = command.Request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<UnitDto>.Ok(ListUnitsHandler.ToDto(unit));
    }
}

public class DeleteUnitHandler : IRequestHandler<DeleteUnitCommand, ApiResult<UnitDto>>
{
    private readonly IAppDbContext _db;

    public DeleteUnitHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<UnitDto>> Handle(DeleteUnitCommand command, CancellationToken cancellationToken)
    {
        var unit = await _db.ProductUnits.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (unit is null)
        {
            return ApiResult<UnitDto>.Fail("Unit was not found.", ApiErrorKind.NotFound);
        }

        var used = await _db.CementProducts.AnyAsync(product => product.UnitId == unit.Id, cancellationToken);
        if (used)
        {
            return ApiResult<UnitDto>.Fail("This unit is used by a product. Set it inactive instead.");
        }

        _db.ProductUnits.Remove(unit);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<UnitDto>.Ok(ListUnitsHandler.ToDto(unit));
    }
}
