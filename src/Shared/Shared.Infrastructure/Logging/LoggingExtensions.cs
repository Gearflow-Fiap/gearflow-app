using Microsoft.AspNetCore.Builder;
using Serilog;

namespace Shared.Infrastructure.Logging;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder, string apiName)
    {
        var isDevelopment = builder.Environment.EnvironmentName == "Development";

        Log.Logger = SerilogConfiguration.ConfigureLogging(apiName, isDevelopment);

        builder.Host.UseSerilog();

        return builder;
    }
}
