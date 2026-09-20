# syntax=docker/dockerfile:1

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached until a .csproj changes.
COPY MyNewsFeed.sln ./
COPY src/MyNewsFeed.Web/MyNewsFeed.Web.csproj src/MyNewsFeed.Web/
COPY tests/MyNewsFeed.Tests/MyNewsFeed.Tests.csproj tests/MyNewsFeed.Tests/
RUN dotnet restore src/MyNewsFeed.Web/MyNewsFeed.Web.csproj

COPY src/ src/
RUN dotnet publish src/MyNewsFeed.Web/MyNewsFeed.Web.csproj -c Release --no-restore -o /app/publish

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Persistent home for the cookie-signing keys (mounted as a volume); owned by the non-root app user.
RUN mkdir /keys && chown $APP_UID /keys

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataProtection__KeysPath=/keys

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "MyNewsFeed.Web.dll"]
