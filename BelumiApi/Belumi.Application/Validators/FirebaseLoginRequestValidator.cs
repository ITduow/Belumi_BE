using Belumi.Core.DTOs;
using FluentValidation;

namespace Belumi.Application.Validators;

public sealed class FirebaseLoginRequestValidator : AbstractValidator<FirebaseLoginRequest>
{
    public FirebaseLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Firebase ID token is required.");
    }
}
