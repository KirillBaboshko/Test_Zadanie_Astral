using FluentValidation;

namespace ChatApp.Server.Application.Validation;

public sealed class GetMessagesQueryValidator : AbstractValidator<GetMessagesQuery>
{
    public GetMessagesQueryValidator()
    {
        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .WithMessage("Лимит должен быть больше 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Лимит не может превышать 1000 сообщений");

        RuleFor(x => x.Since)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.Since.HasValue)
            .WithMessage("Дата 'since' не может быть в будущем");
    }
}

public sealed class GetMessagesQuery
{
    public DateTime? Since { get; init; }
    public Int32 Limit { get; init; } = 100;
}
