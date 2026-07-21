using ChatApp.Contracts.Requests;
using FluentValidation;

namespace ChatApp.Server.Application.Validation;

public sealed class SendMessageAuthRequestValidator : AbstractValidator<SendMessageAuthRequest>
{
    public SendMessageAuthRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Содержимое сообщения не может быть пустым")
            .Length(1, 1000).WithMessage("Содержимое сообщения должно быть от 1 до 1000 символов");
    }
}
