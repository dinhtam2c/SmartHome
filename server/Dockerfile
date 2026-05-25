# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore
WORKDIR /src

COPY Directory.Build.props server.sln ./
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Presentation/Presentation.csproj Presentation/
RUN dotnet restore Presentation/Presentation.csproj --disable-parallel

FROM restore AS publish
ARG BUILD_CONFIGURATION=Release
COPY . .
RUN dotnet publish Presentation/Presentation.csproj \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    -m:1 \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

RUN mkdir -p /app/data && chown "$APP_UID:$APP_UID" /app/data

COPY --from=publish /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "Presentation.dll"]
