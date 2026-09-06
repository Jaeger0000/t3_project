using Microsoft.Extensions.Options;
using T3.Application.Common.Interfaces;

namespace T3.Infrastructure.Notifications;

public sealed class AppLinkBuilder(IOptions<EmailOptions> options) : IAppLinkBuilder
{
    private readonly EmailOptions _options = options.Value;

    public string BuildAppUrl(string relativePath) =>
        $"{_options.AppBaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
}
