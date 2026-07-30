# Multi-stage build do API Gateway (YARP)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/ src/
RUN dotnet restore src/Gateway/GearFlow.Gateway.csproj
RUN dotnet publish src/Gateway/GearFlow.Gateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE 5000
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "GearFlow.Gateway.dll"]
