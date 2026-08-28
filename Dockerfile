# Build and run the site. Railway picks this up automatically.
#
# The image carries no data: the SQLite database, the uploaded photos and the
# data-protection keys live on a mounted volume, because a container filesystem
# is thrown away on every deploy. Attach a Railway volume and the app finds it
# through RAILWAY_VOLUME_MOUNT_PATH.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore on its own layer, so editing code does not re-download packages.
COPY local-liquor/local-liquor/local-liquor.csproj local-liquor/local-liquor/
RUN dotnet restore local-liquor/local-liquor/local-liquor.csproj

COPY local-liquor/ local-liquor/
RUN dotnet publish local-liquor/local-liquor/local-liquor.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

# This runs as root, deliberately. Railway mounts volumes owned by root, so a
# non-root process cannot create the database, the uploads directory or the key
# ring inside one — the app would start and then fail on its first write.
#
# The base image already ships a non-root "app" user (APP_UID). If this ever
# moves somewhere that mounts storage writable by that user, switch to it with:
#     USER $APP_UID
# and chown the mount path to match.

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0

# Railway supplies PORT; Program.cs binds to it when present.
EXPOSE 8080

ENTRYPOINT ["dotnet", "local-liquor.dll"]
