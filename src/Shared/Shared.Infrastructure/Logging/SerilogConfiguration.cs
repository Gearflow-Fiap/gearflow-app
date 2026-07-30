using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Shared.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static Serilog.ILogger ConfigureLogging(string apiName, bool isDevelopment)
    {
        var logTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
        // Seq é opt-in: só envia quando explicitamente configurado (senão o sink bufferiza/descarta
        // contra um Seq que não está de pé — ele vive no profile `tools`).
        var seqUrl = Environment.GetEnvironmentVariable("Seq__ServerUrl");

        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", apiName)
            .Enrich.WithProperty("Environment", isDevelopment ? "Development" : "Production")
            .Enrich.WithProperty("MachineName", Environment.MachineName);

        if (isDevelopment)
        {
            logConfig
                .WriteTo.Console(outputTemplate: logTemplate)
                .WriteTo.File(
                    path: "logs/app-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: logTemplate,
                    retainedFileCountLimit: 7);
        }
        else
        {
            // Logs estruturados em JSON (Fase 3): correlação por TraceId/SpanId via LogContext.
            logConfig
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(
                    formatter: new JsonFormatter(),
                    path: "logs/app-.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30);
        }

        if (!string.IsNullOrWhiteSpace(seqUrl))
            logConfig.WriteTo.Seq(seqUrl);

        return logConfig.CreateLogger();
    }
}
