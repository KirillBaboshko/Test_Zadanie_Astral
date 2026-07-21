using ChatApp.Contracts.Requests;
using FluentValidation;

namespace ChatApp.Server.Application.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Имя пользователя обязательно")
            .Length(3, 100).WithMessage("Имя пользователя должно быть от 3 до 100 символов")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Имя пользователя может содержать только буквы, цифры, _ и -");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Пароль должен быть минимум 6 символов")
            .MaximumLength(100).WithMessage("Пароль не может быть длиннее 100 символов");
    }
}
