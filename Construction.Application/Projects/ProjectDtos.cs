using Construction.Domain.Enums;

namespace Construction.Application.Projects;

public record PublicProjectDto(
    Guid Id,
    string Name,
    string Summary,
    string Description,
    string Location,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly EndDate);

public record ProjectDto(
    Guid Id,
    string Name,
    string Summary,
    string SiteAddress,
    string Description,
    ProjectStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsPublic,
    DateTime CreatedAt);

public record CreateProjectRequest(
    string Name,
    string Summary,
    string SiteAddress,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsPublic);

public record UpdateProjectRequest(
    string Name,
    string Summary,
    string SiteAddress,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsPublic);

public record UpdateProjectStatusRequest(ProjectStatus Status);

public record LinkedOrderDto(Guid Id, string ClientName, string Status, decimal Total, DateTime CreatedAt);

public record LinkedRentalDto(Guid Id, string ClientName, string EquipmentName, string Status, DateOnly StartDate, DateOnly ExpectedReturn);

public record AdminProjectDetailDto(
    ProjectDto Project,
    IReadOnlyList<Workers.AssignmentDto> Assignments,
    IReadOnlyList<LinkedOrderDto> CementOrders,
    IReadOnlyList<LinkedRentalDto> Rentals);
