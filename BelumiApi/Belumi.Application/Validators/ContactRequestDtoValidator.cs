using FluentValidation;
using Belumi.Core.DTOs;

namespace Belumi.Application.Validators;

public sealed class ContactRequestDtoValidator : AbstractValidator<ContactRequestDto>
{
    public ContactRequestDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên không được để trống.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Số điện thoại không được để trống.")
            .Matches(@"^[0-9\-\+\s]{9,15}$").WithMessage("Số điện thoại không hợp lệ.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Nội dung tin nhắn không được để trống.");
    }
}
