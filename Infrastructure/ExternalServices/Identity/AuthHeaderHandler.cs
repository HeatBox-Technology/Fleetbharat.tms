using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _context;

    public AuthHeaderHandler(IHttpContextAccessor context)
    {
        _context = context;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authorizationHeader = _context.HttpContext?
            .Request.Headers["Authorization"]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authorizationHeader) &&
            AuthenticationHeaderValue.TryParse(authorizationHeader, out var parsedHeader) &&
            string.Equals(parsedHeader.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(parsedHeader.Parameter))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", parsedHeader.Parameter);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
