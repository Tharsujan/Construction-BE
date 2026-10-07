using Construction.Application.Cement;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

[ApiController]
[Route("api/cement/products")]
public class CementProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CementProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPublicProductsQuery(), cancellationToken);
        return ResultHttp.From(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPublicProductQuery(id), cancellationToken);
        return ResultHttp.From(result);
    }
}

[ApiController]
[Route("api/cement/orders")]
[Authorize]
public class CementOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CementOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Place(PlaceCementOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PlaceCementOrderCommand(request), cancellationToken);
        return ResultHttp.From(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListMyOrdersQuery(status), cancellationToken);
        return ResultHttp.From(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyOrderQuery(id), cancellationToken);
        return ResultHttp.From(result);
    }
}
