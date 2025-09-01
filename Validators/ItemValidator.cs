
using FluentValidation;
using Starter.Api.DTOs.Items;

namespace Starter.Api.Validators;

public class ItemValidator : AbstractValidator<ItemDto>
{
    public ItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1024).When(x => x.Description != null);
    }
}
