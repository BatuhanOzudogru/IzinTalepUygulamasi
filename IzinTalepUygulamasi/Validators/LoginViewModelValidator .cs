using FluentValidation;
using IzinTalepUygulamasi.Models.ViewModels;

namespace IzinTalepUygulamasi.Validators
{
    public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
    {
        public LoginViewModelValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Kullanıcı adı alanı zorunludur.")
                .Length(3, 50).WithMessage("Kullanıcı adı en az 3, en fazla 50 karakter olmalıdır.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı zorunludur.")
                .MinimumLength(3).WithMessage("Şifre en az 3 karakter olmalıdır.");
        }
    }
}
