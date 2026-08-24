using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Notifications;

public sealed class ResetLinkBuilder(IOptions<EmailOptions> options) : IResetLinkBuilder
{
    private readonly EmailOptions _options = options.Value;

    public string Build(string rawToken) =>
        $"{_options.AppBaseUrl.TrimEnd('/')}/sifre-sifirla/{Uri.EscapeDataString(rawToken)}";
}
