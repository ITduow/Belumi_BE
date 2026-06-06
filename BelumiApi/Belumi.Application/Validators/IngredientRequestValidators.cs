using Belumi.Core.DTOs;
using FluentValidation;
using System.Linq.Expressions;

namespace Belumi.Application.Validators;

public sealed class IngredientCreateRequestValidator : AbstractValidator<IngredientCreateRequest>
{
    public IngredientCreateRequestValidator()
    {
        Include(new IngredientRequestRules<IngredientCreateRequest>(
            x => x.NameInc,
            x => x.Name,
            x => x.Category,
            x => x.Description,
            x => x.Links));
    }
}

public sealed class IngredientUpdateRequestValidator : AbstractValidator<IngredientUpdateRequest>
{
    public IngredientUpdateRequestValidator()
    {
        Include(new IngredientRequestRules<IngredientUpdateRequest>(
            x => x.NameInc,
            x => x.Name,
            x => x.Category,
            x => x.Description,
            x => x.Links));
    }
}

internal sealed class IngredientRequestRules<T> : AbstractValidator<T>
{
    public IngredientRequestRules(
        Expression<Func<T, string>> nameInc,
        Expression<Func<T, string>> name,
        Expression<Func<T, string>> category,
        Expression<Func<T, string>> description,
        Expression<Func<T, string>> links)
    {
        RuleFor(nameInc)
            .NotEmpty().WithName("name_inc").WithMessage("INCI name is required.")
            .MaximumLength(200).WithName("name_inc").WithMessage("INCI name must be 200 characters or fewer.");

        RuleFor(name)
            .NotEmpty().WithName("name").WithMessage("Display name is required.")
            .MaximumLength(200).WithName("name").WithMessage("Display name must be 200 characters or fewer.");

        RuleFor(category)
            .NotEmpty().WithName("category").WithMessage("Category is required.")
            .MaximumLength(300).WithName("category").WithMessage("Category must be 300 characters or fewer.");

        RuleFor(description)
            .NotEmpty().WithName("description").WithMessage("Description is required.")
            .MaximumLength(5000).WithName("description").WithMessage("Description must be 5000 characters or fewer.");

        RuleFor(links)
            .NotEmpty().WithName("links").WithMessage("Links are required.")
            .MaximumLength(3000).WithName("links").WithMessage("Links must be 3000 characters or fewer.")
            .Must(value => value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .All(link => Uri.TryCreate(link, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithName("links")
            .WithMessage("Links must be absolute HTTP/HTTPS URLs separated by '|'.");
    }
}
