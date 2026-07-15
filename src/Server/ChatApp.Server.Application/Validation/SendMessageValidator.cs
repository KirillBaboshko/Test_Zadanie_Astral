using ChatApp.Contracts.Requests;
using FluentValidation;

namespace ChatApp.Server.Application.Validation;

public sealed class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.SenderName)
            .NotEmpty()
            .WithMessage("Имя отправителя не может быть пустым")
            .MaximumLength(100)
            .WithMessage("Имя отправителя не должно превышать 100 символов")
            .Matches(@"^[a-zA-Zа-яА-ЯёЁ0-9\s_-]+$")
            .WithMessage("Имя отправителя может содержать только буквы, цифры, пробелы, дефисы и подчёркивания");

        RuleFor(x => x.Content)
            .NotEmpty()
            .WithMessage("Содержимое сообщения не может быть пустым")
            .MaximumLength(1000)
            .WithMessage("Сообщение не должно превышать 1000 символов")
            .MinimumLength(1)
            .WithMessage("Сообщение должно содержать хотя бы 1 символ");
    }
}
