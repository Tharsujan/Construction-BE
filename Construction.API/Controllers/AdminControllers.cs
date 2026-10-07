using Construction.Application.Cement;
using Construction.Application.ProductUnits;
using Construction.Application.Rentals;
using Construction.Application.Projects;
using Construction.Application.Stats;
using Construction.Application.Workers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/units")]
public class AdminUnitsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminUnitsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListUnitsQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(SaveUnitRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new CreateUnitCommand(request), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveUnitRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateUnitCommand(id, request), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new DeleteUnitCommand(id), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/cement/products")]
public class AdminCementProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCementProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListAdminProductsQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCementProductRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new CreateCementProductCommand(request), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCementProductRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateCementProductCommand(id, request), cancellationToken));

    [HttpPatch("{id:guid}/stock")]
    public async Task<IActionResult> Stock(Guid id, AdjustStockRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new AdjustStockCommand(id, request), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/cement/orders")]
public class AdminCementOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCementOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListAdminOrdersQuery(status), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new GetAdminOrderQuery(id), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, UpdateOrderStatusRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateOrderStatusCommand(id, request), cancellationToken));

    [HttpPatch("{id:guid}/payment")]
    public async Task<IActionResult> Payment(Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateOrderPaymentCommand(id, request), cancellationToken));

    [HttpPatch("{id:guid}/project")]
    public async Task<IActionResult> Project(Guid id, AttachProjectRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new AttachOrderProjectCommand(id, request), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/equipment")]
public class AdminEquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminEquipmentController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListAdminEquipmentQuery(), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateEquipmentRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new CreateEquipmentCommand(request), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEquipmentRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateEquipmentCommand(id, request), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, UpdateEquipmentStatusRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateEquipmentStatusCommand(id, request), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/rentals")]
public class AdminRentalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminRentalsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListAdminRentalsQuery(status), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new GetAdminRentalQuery(id), cancellationToken));

    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ApproveRentalCommand(id), cancellationToken));

    [HttpPatch("{id:guid}/return")]
    public async Task<IActionResult> Return(Guid id, ReturnRentalRequest? request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ReturnRentalCommand(id, request ?? new ReturnRentalRequest(null)), cancellationToken));

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new CancelRentalCommand(id), cancellationToken));

    [HttpPatch("{id:guid}/payment")]
    public async Task<IActionResult> Payment(Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateRentalPaymentCommand(id, request), cancellationToken));

    [HttpPatch("{id:guid}/project")]
    public async Task<IActionResult> Project(Guid id, AttachProjectRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new AttachRentalProjectCommand(id, request), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/projects")]
public class AdminProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminProjectsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListAdminProjectsQuery(status), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new CreateProjectCommand(request), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new GetAdminProjectQuery(id), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateProjectCommand(id, request), cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> Status(Guid id, UpdateProjectStatusRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateProjectStatusCommand(id, request), cancellationToken));

    [HttpGet("{id:guid}/assignments")]
    public async Task<IActionResult> Assignments(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListAssignmentsQuery(id), cancellationToken));

    [HttpPost("{id:guid}/assignments")]
    public async Task<IActionResult> Assign(Guid id, CreateAssignmentRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new CreateAssignmentCommand(id, request), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/workers")]
public class AdminWorkersController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminWorkersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? active, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new ListWorkersQuery(active), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkerRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new CreateWorkerCommand(request), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new GetWorkerQuery(id), cancellationToken));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateWorkerRequest request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new UpdateWorkerCommand(id, request), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/assignments")]
public class AdminAssignmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminAssignmentsController(IMediator mediator) => _mediator = mediator;

    [HttpPatch("{id:guid}/end")]
    public async Task<IActionResult> End(Guid id, EndAssignmentRequest? request, CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new EndAssignmentCommand(id, request ?? new EndAssignmentRequest(null)), cancellationToken));
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/stats")]
public class AdminStatsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminStatsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        ResultHttp.From(await _mediator.Send(new GetStatsQuery(), cancellationToken));
}
