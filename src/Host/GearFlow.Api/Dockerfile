# Multi-stage no padrão do Visual Studio (stages base/build/publish/final → habilita F5/Fast Mode).
# Contexto de build = raiz da solução (ver docker-compose.yml).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# New Relic .NET agent — [trusted=yes] contorna rotação de chaves GPG do repo apt.newrelic.com
RUN echo 'deb [trusted=yes] https://apt.newrelic.com/debian/ newrelic non-free' \
       | tee /etc/apt/sources.list.d/newrelic.list \
    && apt-get update \
    && apt-get install -y newrelic-dotnet-agent \
    && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
    CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so \
    NEW_RELIC_APPLICATION_LOGGING_FORWARDING_ENABLED=true

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY src/ src/
RUN dotnet restore "src/Host/GearFlow.Api/GearFlow.Api.csproj"
WORKDIR "/src/src/Host/GearFlow.Api"
RUN dotnet build "GearFlow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GearFlow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GearFlow.Api.dll"]
