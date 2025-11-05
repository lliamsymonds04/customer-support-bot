# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy the full project
COPY . .

# Restore explicitly for linux-musl-x64
RUN dotnet restore -r linux-musl-x64

# Publish (no restore needed now)
RUN dotnet publish -c Release -o /app/publish \
    -r linux-musl-x64 \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=true \
    /p:PublishReadyToRun=true \
    --self-contained true

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["./SupportBotBackend"]

