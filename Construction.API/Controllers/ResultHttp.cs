using Construction.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public static class ResultHttp
{
    public static IActionResult From<T>(ApiResult<T> result) => result.ErrorKind switch
    {
        ApiErrorKind.NotFound => new NotFoundObjectResult(result),
        ApiErrorKind.Unauthorized => new UnauthorizedObjectResult(result),
        _ when result.Success => new OkObjectResult(result),
        _ => new BadRequestObjectResult(result)
    };
}
