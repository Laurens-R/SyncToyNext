# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Stn.Cli/Stn.Cli.csproj -c Release -o /app/publish --self-contained true -r linux-x64 /p:PublishSingleFile=true /p:PublishTrimmed=true

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# Expose a volume for config and data
VOLUME ["/data"]
ENTRYPOINT ["/app/Stn.Cli"]
