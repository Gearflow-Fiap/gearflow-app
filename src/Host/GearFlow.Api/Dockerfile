# Multi-stage build do host GearFlow.Api (monólito modular)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore por camada: copia os manifests primeiro para cache de restore
COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/Host/GearFlow.Api/GearFlow.Api.csproj
RUN dotnet publish src/Host/GearFlow.Api/GearFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "GearFlow.Api.dll"]
