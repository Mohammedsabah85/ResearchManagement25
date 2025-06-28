using FluentValidation;
using ResearchManagement.Application.DTOs;

namespace ResearchManagement.Application.Validators
{
    public class CreateTrackReviewerDtoValidator : AbstractValidator<CreateTrackReviewerDto>
    {
        public CreateTrackReviewerDtoValidator()
        {
            RuleFor(x => x.ReviewerId)
                .NotEmpty().WithMessage("معرف المراجع مطلوب");

            RuleFor(x => x.TrackManagerId)
                .GreaterThan(0).WithMessage("معرف مدير التراك غير صالح");

            RuleFor(x => x.Track)
                .IsInEnum().WithMessage("التخصص غير صالح");
        }
    }
}