# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore \
    -r linux-x64 \
    /p:PublishTrimmed=true /p:PublishSingleFile=true /p:ReadyToRun=true \
    --self-contained true


# Stage 2: Runtime (lightweight)
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["./SupportBotBackend"]
