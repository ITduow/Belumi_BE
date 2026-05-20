using FluentValidation;
using Belumi.Core.DTOs;

namespace Belumi.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(6).WithMessage("Mật khẩu phải chứa ít nhất 6 ký tự.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(100).WithMessage("Họ và tên không được dài quá 100 ký tự.");

        RuleFor(x => x.Phone)
            .Matches(@"^[0-9\-\+\s]{9,15}$")
            .WithMessage("Số điện thoại không đúng định dạng.")
            .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}
