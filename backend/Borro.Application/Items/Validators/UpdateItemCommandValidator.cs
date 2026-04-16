using Borro.Application.Items.Commands;
using FluentValidation;

namespace Borro.Application.Items.Validators;

public sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty().WithMessage("ItemId is required.");
        RuleFor(x => x.RequestingUserId).NotEmpty().WithMessage("RequestingUserId is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("Title is required and must be at most 200 characters.");
        RuleFor(x => x.Description).MaximumLength(2000).WithMessage("Description must be at most 2000 characters.");
        RuleFor(x => x.DailyPrice).GreaterThan(0).WithMessage("DailyPrice must be greater than zero.");
        RuleFor(x => x.Location).NotEmpty().MaximumLength(300).WithMessage("Location is required and must be at most 300 characters.");
        RuleFor(x => x.Category).IsInEnum().WithMessage("Category is not valid.");
        RuleFor(x => x.HandoverOptions).NotNull().WithMessage("HandoverOptions must not be null.");
        RuleForEach(x => x.HandoverOptions).IsInEnum().WithMessage("Each HandoverOption must be a valid value.");
    }
}
