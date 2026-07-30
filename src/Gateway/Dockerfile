# API Gateway (YARP) — padrão multi-stage do Visual Studio (base/build/publish/final).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY src/ src/
RUN dotnet restore "src/Gateway/GearFlow.Gateway.csproj"
WORKDIR "/src/src/Gateway"
RUN dotnet build "GearFlow.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GearFlow.Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=5000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GearFlow.Gateway.dll"]
