using FluentValidation;

namespace T3.Application.Features.Notifications.SendNotification;

public sealed record SendNotificationRequest(string Message);

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Bildirim metni zorunludur.")
            .MaximumLength(2000).WithMessage("Bildirim metni en fazla 2000 karakter olabilir.");
    }
}
