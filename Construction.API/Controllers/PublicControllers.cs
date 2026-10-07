using Construction.Application.Rentals;
using Construction.Application.Projects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPublicProjectsQuery(status), cancellationToken);
        return ResultHttp.From(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPublicProjectQuery(id), cancellationToken);
        return ResultHttp.From(result);
    }
}

[ApiController]
[Route("api/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly IMediator _mediator;

    public EquipmentController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPublicEquipmentQuery(), cancellationToken);
        return ResultHttp.From(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPublicEquipmentQuery(id), cancellationToken);
        return ResultHttp.From(result);
    }
}

[ApiController]
[Route("api/rentals")]
[Authorize]
public class RentalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RentalsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> RequestRental(RequestRentalRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RequestRentalCommand(request), cancellationToken);
        return ResultHttp.From(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListMyRentalsQuery(status), cancellationToken);
        return ResultHttp.From(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyRentalQuery(id), cancellationToken);
        return ResultHttp.From(result);
    }
}
