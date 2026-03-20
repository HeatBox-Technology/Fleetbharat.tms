public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        await _next(context);

        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path == "/")
        {
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "script-src 'self' 'unsafe-inline';";
        }
        else
        {
            headers["Content-Security-Policy"] =
                "default-src 'self'; style-src 'self'; script-src 'self';";
        }
    }
}