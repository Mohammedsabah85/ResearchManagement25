using FluentValidation;
using ResearchManagement.Application.DTOs;

namespace ResearchManagement.Application.Validators
{
    public class CreateTrackManagerDtoValidator : AbstractValidator<CreateTrackManagerDto>
    {
        public CreateTrackManagerDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("معرف المستخدم مطلوب");

            RuleFor(x => x.Track)
                .IsInEnum().WithMessage("التخصص غير صالح");

            RuleFor(x => x.TrackDescription)
                .MaximumLength(200).WithMessage("وصف التخصص يجب ألا يتجاوز 200 حرف");
        }
    }
}