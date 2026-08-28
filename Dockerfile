# Build and run the site. Railway will pick this up automatically.
#
# The image carries no data: the SQLite database and uploaded photos live on a
# mounted volume, because a container filesystem is thrown away on every deploy.
# Attach a Railway volume and the app finds it through RAILWAY_VOLUME_MOUNT_PATH.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, on its own layer, so code edits do not re-download packages.
COPY local-liquor/local-liquor/local-liquor.csproj local-liquor/local-liquor/
RUN dotnet restore local-liquor/local-liquor/local-liquor.csproj

COPY local-liquor/ local-liquor/
RUN dotnet publish local-liquor/local-liquor/local-liquor.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Run as a non-root user. The volume mount point is created and handed over
# first, or the app cannot write its database.
RUN useradd --uid 5678 --create-home app \
    && mkdir -p /data \
    && chown -R app:app /data
USER app

COPY --from=build --chown=app:app /app .

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0

# Railway supplies PORT; Program.cs binds to it when present.
EXPOSE 8080

ENTRYPOINT ["dotnet", "local-liquor.dll"]
