# Multi-stage no padrão do Visual Studio (stages base/build/publish/final → habilita F5/Fast Mode).
# Contexto de build = raiz da solução (ver docker-compose.yml).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

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
