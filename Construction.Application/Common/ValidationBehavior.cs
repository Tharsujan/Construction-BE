using FluentValidation;
using MediatR;

namespace Construction.Application.Common;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        var message = string.Join(" ", failures.Select(failure => failure.ErrorMessage).Distinct());
        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(ApiResult<>))
        {
            var dataType = typeof(TResponse).GetGenericArguments()[0];
            var closed = typeof(ApiResult<>).MakeGenericType(dataType);
            var instance = Activator.CreateInstance(closed);
            closed.GetProperty(nameof(ApiResult<object>.Success))!.SetValue(instance, false);
            closed.GetProperty(nameof(ApiResult<object>.Message))!.SetValue(instance, message);
            closed.GetProperty(nameof(ApiResult<object>.ErrorKind))!.SetValue(instance, ApiErrorKind.BadRequest);
            return (TResponse)instance!;
        }

        throw new ValidationException(failures);
    }
}
