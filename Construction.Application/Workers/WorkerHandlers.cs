using Construction.Application.Common;
using Construction.Application.Projects;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Workers;

public record ListWorkersQuery(bool? Active) : IRequest<ApiResult<IReadOnlyList<WorkerDto>>>;
public record CreateWorkerCommand(CreateWorkerRequest Request) : IRequest<ApiResult<WorkerDto>>;
public record GetWorkerQuery(Guid Id) : IRequest<ApiResult<WorkerDetailDto>>;
public record UpdateWorkerCommand(Guid Id, UpdateWorkerRequest Request) : IRequest<ApiResult<WorkerDto>>;
public record ListAssignmentsQuery(Guid ProjectId) : IRequest<ApiResult<IReadOnlyList<AssignmentDto>>>;
public record CreateAssignmentCommand(Guid ProjectId, CreateAssignmentRequest Request) : IRequest<ApiResult<AssignmentDto>>;
public record EndAssignmentCommand(Guid Id, EndAssignmentRequest Request) : IRequest<ApiResult<AssignmentDto>>;

public class CreateWorkerValidator : AbstractValidator<CreateWorkerCommand>
{
    public CreateWorkerValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Request.Trade).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.DailyRate).GreaterThan(0);
    }
}

public class UpdateWorkerValidator : AbstractValidator<UpdateWorkerCommand>
{
    public UpdateWorkerValidator()
    {
        RuleFor(x => x.Request.Trade).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Request.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Request.DailyRate).GreaterThan(0);
    }
}

public class CreateAssignmentValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentValidator()
    {
        RuleFor(x => x.Request.WorkerId).NotEmpty();
        RuleFor(x => x.Request.EndDate).GreaterThanOrEqualTo(x => x.Request.StartDate);
    }
}

public static class WorkerMaps
{
    public static WorkerDto ToDto(Worker worker) => new(worker.Id, worker.Name, worker.Trade, worker.Phone, worker.DailyRate, worker.IsActive);
}

public class ListWorkersHandler : IRequestHandler<ListWorkersQuery, ApiResult<IReadOnlyList<WorkerDto>>>
{
    private readonly IAppDbContext _db;

    public ListWorkersHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<WorkerDto>>> Handle(ListWorkersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Workers.AsQueryable();
        if (request.Active.HasValue)
        {
            query = query.Where(worker => worker.IsActive == request.Active.Value);
        }

        var workers = await query.OrderBy(worker => worker.Name).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<WorkerDto>>.Ok(workers.Select(WorkerMaps.ToDto).ToList());
    }
}

public class CreateWorkerHandler : IRequestHandler<CreateWorkerCommand, ApiResult<WorkerDto>>
{
    private readonly IAppDbContext _db;

    public CreateWorkerHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<WorkerDto>> Handle(CreateWorkerCommand command, CancellationToken cancellationToken)
    {
        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            Name = command.Request.Name.Trim(),
            Trade = command.Request.Trade.Trim(),
            Phone = command.Request.Phone.Trim(),
            DailyRate = command.Request.DailyRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Workers.Add(worker);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<WorkerDto>.Ok(WorkerMaps.ToDto(worker));
    }
}

public class GetWorkerHandler : IRequestHandler<GetWorkerQuery, ApiResult<WorkerDetailDto>>
{
    private readonly IAppDbContext _db;

    public GetWorkerHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<WorkerDetailDto>> Handle(GetWorkerQuery request, CancellationToken cancellationToken)
    {
        var worker = await _db.Workers.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (worker is null)
        {
            return ApiResult<WorkerDetailDto>.Fail("Worker was not found.", ApiErrorKind.NotFound);
        }

        var assignments = await _db.ProjectAssignments
            .Include(item => item.Project)
            .Include(item => item.Worker)
            .Where(item => item.WorkerId == worker.Id)
            .OrderByDescending(item => item.StartDate)
            .ToListAsync(cancellationToken);

        return ApiResult<WorkerDetailDto>.Ok(new WorkerDetailDto(
            WorkerMaps.ToDto(worker),
            assignments.Select(ProjectMaps.ToAssignment).ToList()));
    }
}

public class UpdateWorkerHandler : IRequestHandler<UpdateWorkerCommand, ApiResult<WorkerDto>>
{
    private readonly IAppDbContext _db;

    public UpdateWorkerHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<WorkerDto>> Handle(UpdateWorkerCommand command, CancellationToken cancellationToken)
    {
        var worker = await _db.Workers.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (worker is null)
        {
            return ApiResult<WorkerDto>.Fail("Worker was not found.", ApiErrorKind.NotFound);
        }

        worker.Trade = command.Request.Trade.Trim();
        worker.Phone = command.Request.Phone.Trim();
        worker.DailyRate = command.Request.DailyRate;
        worker.IsActive = command.Request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<WorkerDto>.Ok(WorkerMaps.ToDto(worker));
    }
}

public class ListAssignmentsHandler : IRequestHandler<ListAssignmentsQuery, ApiResult<IReadOnlyList<AssignmentDto>>>
{
    private readonly IAppDbContext _db;

    public ListAssignmentsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<AssignmentDto>>> Handle(ListAssignmentsQuery request, CancellationToken cancellationToken)
    {
        var projectExists = await _db.Projects.AnyAsync(project => project.Id == request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return ApiResult<IReadOnlyList<AssignmentDto>>.Fail("Project was not found.", ApiErrorKind.NotFound);
        }

        var assignments = await _db.ProjectAssignments
            .Include(item => item.Project)
            .Include(item => item.Worker)
            .Where(item => item.ProjectId == request.ProjectId)
            .OrderBy(item => item.StartDate)
            .ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<AssignmentDto>>.Ok(assignments.Select(ProjectMaps.ToAssignment).ToList());
    }
}

public class CreateAssignmentHandler : IRequestHandler<CreateAssignmentCommand, ApiResult<AssignmentDto>>
{
    private readonly IAppDbContext _db;

    public CreateAssignmentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<AssignmentDto>> Handle(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(item => item.Id == command.ProjectId, cancellationToken);
        if (project is null)
        {
            return ApiResult<AssignmentDto>.Fail("Project was not found.", ApiErrorKind.NotFound);
        }

        var worker = await _db.Workers.FirstOrDefaultAsync(item => item.Id == command.Request.WorkerId, cancellationToken);
        if (worker is null)
        {
            return ApiResult<AssignmentDto>.Fail("Worker was not found.", ApiErrorKind.NotFound);
        }

        if (!worker.IsActive)
        {
            return ApiResult<AssignmentDto>.Fail("This worker is not active.");
        }

        var overlaps = await _db.ProjectAssignments.AnyAsync(item =>
            item.WorkerId == worker.Id &&
            item.StartDate <= command.Request.EndDate &&
            item.EndDate >= command.Request.StartDate, cancellationToken);
        if (overlaps)
        {
            return ApiResult<AssignmentDto>.Fail("This worker is already assigned during those dates.");
        }

        var assignment = new ProjectAssignment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Project = project,
            WorkerId = worker.Id,
            Worker = worker,
            StartDate = command.Request.StartDate,
            EndDate = command.Request.EndDate,
            DailyRate = worker.DailyRate
        };
        _db.ProjectAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<AssignmentDto>.Ok(ProjectMaps.ToAssignment(assignment));
    }
}

public class EndAssignmentHandler : IRequestHandler<EndAssignmentCommand, ApiResult<AssignmentDto>>
{
    private readonly IAppDbContext _db;

    public EndAssignmentHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<AssignmentDto>> Handle(EndAssignmentCommand command, CancellationToken cancellationToken)
    {
        var assignment = await _db.ProjectAssignments
            .Include(item => item.Project)
            .Include(item => item.Worker)
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (assignment is null)
        {
            return ApiResult<AssignmentDto>.Fail("Assignment was not found.", ApiErrorKind.NotFound);
        }

        var endDate = command.Request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (endDate < assignment.StartDate)
        {
            return ApiResult<AssignmentDto>.Fail("End date cannot be before the assignment start date.");
        }

        assignment.EndDate = endDate;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<AssignmentDto>.Ok(ProjectMaps.ToAssignment(assignment));
    }
}
