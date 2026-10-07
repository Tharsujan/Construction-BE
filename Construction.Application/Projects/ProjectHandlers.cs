using Construction.Application.Common;
using Construction.Application.Workers;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Projects;

public record ListPublicProjectsQuery(string? Status) : IRequest<ApiResult<IReadOnlyList<PublicProjectDto>>>;
public record GetPublicProjectQuery(Guid Id) : IRequest<ApiResult<PublicProjectDto>>;
public record ListAdminProjectsQuery(string? Status) : IRequest<ApiResult<IReadOnlyList<ProjectDto>>>;
public record CreateProjectCommand(CreateProjectRequest Request) : IRequest<ApiResult<ProjectDto>>;
public record GetAdminProjectQuery(Guid Id) : IRequest<ApiResult<AdminProjectDetailDto>>;
public record UpdateProjectCommand(Guid Id, UpdateProjectRequest Request) : IRequest<ApiResult<ProjectDto>>;
public record UpdateProjectStatusCommand(Guid Id, UpdateProjectStatusRequest Request) : IRequest<ApiResult<ProjectDto>>;

public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Request.Summary).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.SiteAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.EndDate).GreaterThanOrEqualTo(x => x.Request.StartDate);
    }
}

public class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Request.Summary).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.SiteAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.EndDate).GreaterThanOrEqualTo(x => x.Request.StartDate);
    }
}

public static class ProjectMaps
{
    public static PublicProjectDto ToPublic(Project project) => new(
        project.Id,
        project.Name,
        project.Summary,
        project.Description,
        project.SiteAddress,
        project.Status,
        project.StartDate,
        project.EndDate);

    public static ProjectDto ToDto(Project project) => new(
        project.Id,
        project.Name,
        project.Summary,
        project.SiteAddress,
        project.Description,
        project.Status,
        project.StartDate,
        project.EndDate,
        project.IsPublic,
        project.CreatedAt);

    public static AssignmentDto ToAssignment(ProjectAssignment assignment) => new(
        assignment.Id,
        assignment.ProjectId,
        assignment.Project.Name,
        assignment.WorkerId,
        assignment.Worker.Name,
        assignment.Worker.Trade,
        assignment.StartDate,
        assignment.EndDate,
        assignment.DailyRate);
}

public class ListPublicProjectsHandler : IRequestHandler<ListPublicProjectsQuery, ApiResult<IReadOnlyList<PublicProjectDto>>>
{
    private readonly IAppDbContext _db;

    public ListPublicProjectsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<PublicProjectDto>>> Handle(ListPublicProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Projects.Where(project => project.IsPublic);
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(project => project.Status == ProjectStatus.Planned || project.Status == ProjectStatus.InProgress);
        }
        else if (request.Status.Equals("upcoming", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(project => project.Status == ProjectStatus.Planned);
        }
        else if (request.Status.Equals("ongoing", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(project => project.Status == ProjectStatus.InProgress);
        }
        else
        {
            return ApiResult<IReadOnlyList<PublicProjectDto>>.Fail("Status must be upcoming or ongoing.");
        }

        var projects = await query.OrderBy(project => project.StartDate).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<PublicProjectDto>>.Ok(projects.Select(ProjectMaps.ToPublic).ToList());
    }
}

public class GetPublicProjectHandler : IRequestHandler<GetPublicProjectQuery, ApiResult<PublicProjectDto>>
{
    private readonly IAppDbContext _db;

    public GetPublicProjectHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<PublicProjectDto>> Handle(GetPublicProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(item => item.Id == request.Id && item.IsPublic, cancellationToken);
        return project is null
            ? ApiResult<PublicProjectDto>.Fail("Project was not found.", ApiErrorKind.NotFound)
            : ApiResult<PublicProjectDto>.Ok(ProjectMaps.ToPublic(project));
    }
}

public class ListAdminProjectsHandler : IRequestHandler<ListAdminProjectsQuery, ApiResult<IReadOnlyList<ProjectDto>>>
{
    private readonly IAppDbContext _db;

    public ListAdminProjectsHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<IReadOnlyList<ProjectDto>>> Handle(ListAdminProjectsQuery request, CancellationToken cancellationToken)
    {
        var parsed = StatusParser.TryParse<ProjectStatus>(request.Status, out var status, out var error);
        if (!parsed)
        {
            return ApiResult<IReadOnlyList<ProjectDto>>.Fail(error!);
        }

        var query = _db.Projects.AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(project => project.Status == status.Value);
        }

        var projects = await query.OrderByDescending(project => project.CreatedAt).ToListAsync(cancellationToken);
        return ApiResult<IReadOnlyList<ProjectDto>>.Ok(projects.Select(ProjectMaps.ToDto).ToList());
    }
}

public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, ApiResult<ProjectDto>>
{
    private readonly IAppDbContext _db;

    public CreateProjectHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<ProjectDto>> Handle(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = command.Request.Name.Trim(),
            Summary = command.Request.Summary.Trim(),
            SiteAddress = command.Request.SiteAddress.Trim(),
            Description = command.Request.Description.Trim(),
            Status = ProjectStatus.Planned,
            StartDate = command.Request.StartDate,
            EndDate = command.Request.EndDate,
            IsPublic = command.Request.IsPublic,
            CreatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<ProjectDto>.Ok(ProjectMaps.ToDto(project));
    }
}

public class GetAdminProjectHandler : IRequestHandler<GetAdminProjectQuery, ApiResult<AdminProjectDetailDto>>
{
    private readonly IAppDbContext _db;

    public GetAdminProjectHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<AdminProjectDetailDto>> Handle(GetAdminProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (project is null)
        {
            return ApiResult<AdminProjectDetailDto>.Fail("Project was not found.", ApiErrorKind.NotFound);
        }

        var assignments = await _db.ProjectAssignments
            .Include(item => item.Project)
            .Include(item => item.Worker)
            .Where(item => item.ProjectId == project.Id)
            .OrderBy(item => item.StartDate)
            .ToListAsync(cancellationToken);

        var orders = await _db.CementOrders
            .Include(item => item.Client)
            .Where(item => item.ProjectId == project.Id)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new LinkedOrderDto(item.Id, item.Client.Name, item.Status.ToString(), item.Total, item.CreatedAt))
            .ToListAsync(cancellationToken);

        var rentals = await _db.EquipmentRentals
            .Include(item => item.Client)
            .Include(item => item.Equipment)
            .Where(item => item.ProjectId == project.Id)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new LinkedRentalDto(item.Id, item.Client.Name, item.Equipment.Name, item.Status.ToString(), item.StartDate, item.ExpectedReturn))
            .ToListAsync(cancellationToken);

        return ApiResult<AdminProjectDetailDto>.Ok(new AdminProjectDetailDto(
            ProjectMaps.ToDto(project),
            assignments.Select(ProjectMaps.ToAssignment).ToList(),
            orders,
            rentals));
    }
}

public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, ApiResult<ProjectDto>>
{
    private readonly IAppDbContext _db;

    public UpdateProjectHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<ProjectDto>> Handle(UpdateProjectCommand command, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (project is null)
        {
            return ApiResult<ProjectDto>.Fail("Project was not found.", ApiErrorKind.NotFound);
        }

        project.Name = command.Request.Name.Trim();
        project.Summary = command.Request.Summary.Trim();
        project.SiteAddress = command.Request.SiteAddress.Trim();
        project.Description = command.Request.Description.Trim();
        project.StartDate = command.Request.StartDate;
        project.EndDate = command.Request.EndDate;
        project.IsPublic = command.Request.IsPublic;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<ProjectDto>.Ok(ProjectMaps.ToDto(project));
    }
}

public class UpdateProjectStatusHandler : IRequestHandler<UpdateProjectStatusCommand, ApiResult<ProjectDto>>
{
    private readonly IAppDbContext _db;

    public UpdateProjectStatusHandler(IAppDbContext db) => _db = db;

    public async Task<ApiResult<ProjectDto>> Handle(UpdateProjectStatusCommand command, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);
        if (project is null)
        {
            return ApiResult<ProjectDto>.Fail("Project was not found.", ApiErrorKind.NotFound);
        }

        if (!Enum.IsDefined(command.Request.Status))
        {
            return ApiResult<ProjectDto>.Fail("Project status is not valid.");
        }

        project.Status = command.Request.Status;
        await _db.SaveChangesAsync(cancellationToken);
        return ApiResult<ProjectDto>.Ok(ProjectMaps.ToDto(project));
    }
}
