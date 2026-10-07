using Construction.Domain.Enums;

namespace Construction.Application.Workers;

public record WorkerDto(Guid Id, string Name, string Trade, string Phone, decimal DailyRate, bool IsActive);

public record AssignmentDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    Guid WorkerId,
    string WorkerName,
    string Trade,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal DailyRate);

public record CreateWorkerRequest(string Name, string Trade, string Phone, decimal DailyRate);

public record UpdateWorkerRequest(string Trade, string Phone, decimal DailyRate, bool IsActive);

public record CreateAssignmentRequest(Guid WorkerId, DateOnly StartDate, DateOnly EndDate);

public record EndAssignmentRequest(DateOnly? EndDate);

public record WorkerDetailDto(WorkerDto Worker, IReadOnlyList<AssignmentDto> Assignments);
