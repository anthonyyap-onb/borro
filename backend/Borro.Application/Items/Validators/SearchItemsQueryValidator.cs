using Borro.Application.Items.Queries;
using FluentValidation;

namespace Borro.Application.Items.Validators;

public sealed class SearchItemsQueryValidator : AbstractValidator<SearchItemsQuery>
{
    public SearchItemsQueryValidator()
    {
        RuleFor(x => x.MinPrice)
            .GreaterThan(0).When(x => x.MinPrice.HasValue)
            .WithMessage("MinPrice must be greater than zero.");

        RuleFor(x => x.MaxPrice)
            .GreaterThan(0).When(x => x.MaxPrice.HasValue)
            .WithMessage("MaxPrice must be greater than zero.");

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithName("PriceRange")
            .WithMessage("MinPrice must be less than or equal to MaxPrice.");

        RuleFor(x => x)
            .Must(x =>
                (!x.AvailableFrom.HasValue && !x.AvailableTo.HasValue) ||
                (x.AvailableFrom.HasValue && x.AvailableTo.HasValue && x.AvailableFrom <= x.AvailableTo))
            .WithName("DateRange")
            .WithMessage("AvailableFrom and AvailableTo must both be provided and AvailableFrom must not be after AvailableTo.");

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Category)
            .IsInEnum().When(x => x.Category.HasValue)
            .WithMessage("Category is not valid.");
    }
}
