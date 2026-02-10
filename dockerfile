# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet --info
RUN ls -la
RUN ls -la Host || true

# Печатаем доступность nuget (часто именно тут проблема)
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl && rm -rf /var/lib/apt/lists/*
RUN curl -I https://api.nuget.org/v3/index.json || true

# Restore с подробным логом и для конкретного проекта
RUN dotnet restore Host/Host.csproj -v diag

RUN dotnet publish Host/Host.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Host.dll"]