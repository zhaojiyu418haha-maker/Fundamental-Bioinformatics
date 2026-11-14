# Multi-stage build for the .NET 9 application.
# The Linux runtime uses the managed HttpListener implementation.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "src/BattlegroundsGallery.Web/BattlegroundsGallery.Web.csproj"
RUN dotnet publish "src/BattlegroundsGallery.Web/BattlegroundsGallery.Web.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV BATTLEGROUNDS_DB="Host=db;Port=5432;Database=battlegrounds_gallery;Username=bg_app;Password=bg_dev_password;"
ENV PORT=5080

EXPOSE 5080

ENTRYPOINT ["dotnet", "BattlegroundsGallery.Web.dll", "--host", "0.0.0.0", "--port", "5080"]
