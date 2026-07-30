using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// YARP config-driven: rotas /api/** → GearFlow.Api. Adicionar um BC é editar appsettings, não código.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS — vive SÓ no gateway.
const string CorsPolicy = "frontend";
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
{
    var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:3000"];
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

// Rate limiting — vive SÓ no gateway. Janela apertada em auth, generosa no resto; /health e /metrics isentos.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase))
            return RateLimitPartition.GetNoLimiter("exempt");

        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var isAuth = path.Contains("/auth", StringComparison.OrdinalIgnoreCase)
                     || path.Contains("/login", StringComparison.OrdinalIgnoreCase);

        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = isAuth ? 10 : 100,
            Window = isAuth ? TimeSpan.FromMinutes(1) : TimeSpan.FromSeconds(10),
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

app.UseCors(CorsPolicy);
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapReverseProxy();

await app.RunAsync();
