using FluentValidation;
using IzinTalepUygulamasi.Models.ViewModels;

namespace IzinTalepUygulamasi.Validators
{
    public class LeaveRequestCreateViewModelValidator : AbstractValidator<LeaveRequestCreateViewModel>
    {
        public LeaveRequestCreateViewModelValidator()
        {
            RuleFor(x => x.LeaveType)
                .IsInEnum().WithMessage("Geçerli bir izin türü seçiniz.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Başlangıç tarihi zorunludur.")
                .GreaterThanOrEqualTo(DateTime.Now.Date.AddDays(-7)).WithMessage("Başlangıç tarihi bugünden en fazla 7 gün öncesi olabilir.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("Bitiş tarihi zorunludur.")
                .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("Bitiş tarihi, başlangıç tarihinden önce olamaz.");

            RuleFor(x => x.Reason)
                .MaximumLength(500).WithMessage("Açıklama en fazla 500 karakter olabilir.");
        }
    }
}
