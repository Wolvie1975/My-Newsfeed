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
# No --no-restore: the early restore above only saw the .csproj, not the .razor files, and skips the package that
# supplies _framework/blazor.web.js. Restoring again with the full source picks it up.
RUN dotnet publish src/MyNewsFeed.Web/MyNewsFeed.Web.csproj -c Release -o /app/publish

# Guard: fail the build if the published site cannot serve the Blazor script. Without it the admin pages are not
# interactive and every form post is rejected with a 400, and nothing else would notice until someone used the site.
RUN grep -q "_framework/blazor.web.js" /app/publish/MyNewsFeed.Web.staticwebassets.endpoints.json || { \
      echo "ERROR: _framework/blazor.web.js is missing from the published static assets." >&2; \
      echo "The admin pages would not be interactive. Check that restore ran with the full source (see the publish step)." >&2; \
      exit 1; }

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
