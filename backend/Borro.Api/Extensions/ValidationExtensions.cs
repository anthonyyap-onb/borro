using FluentValidation;

namespace Borro.Api.Extensions;

public static class ValidationExtensions
{
    /// <summary>
    /// Converts a FluentValidation ValidationException into the dictionary format
    /// expected by Results.ValidationProblem (field -> string[]).
    /// </summary>
    public static IDictionary<string, string[]> ToErrorDictionary(this ValidationException ex) =>
        ex.Errors
          .GroupBy(e => e.PropertyName)
          .ToDictionary(
              g => g.Key,
              g => g.Select(e => e.ErrorMessage).ToArray());
}
