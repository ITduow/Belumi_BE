using FluentValidation;
using Belumi.Core.DTOs;

namespace Belumi.Application.Validators;

public sealed class SkinAnalysisRequestValidator : AbstractValidator<SkinAnalysisRequest>
{
    public SkinAnalysisRequestValidator()
    {
        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("Ảnh chụp phân tích da không được để trống.");

        RuleFor(x => x.SkinType)
            .NotEmpty().WithMessage("Loại da không được để trống.");
    }
}
