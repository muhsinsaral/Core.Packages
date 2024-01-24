﻿using Core.CrossCuttingConcerns.Exceptions.Types;
using FluentValidation;
using MediatR;
using ValidationException = Core.CrossCuttingConcerns.Exceptions.Types.ValidationException;

namespace Core.Application.PipeLines.Validation;

public class RequestValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public RequestValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ValidationContext<object> context = new(request);

        IEnumerable<ValidationExceptionModel> errors = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(error => error != null)
            .GroupBy(
            keySelector: error => error.PropertyName,
            resultSelector: (propertyName, errors) =>
            new ValidationExceptionModel { Property = propertyName, Errors = errors.Select(error => error.ErrorMessage) })
            .ToList();
        if (errors.Any())
            throw new ValidationException(errors);

        TResponse response = await next();
        return response;
    }
}
