# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

RUN apk add --no-cache curl

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY ["shared/Flowist.Shared/Flowist.Shared.csproj", "shared/Flowist.Shared/"]
COPY ["src/Flowist.TaskService/Flowist.TaskService.csproj", "src/Flowist.TaskService/"]

RUN dotnet restore "src/Flowist.TaskService/Flowist.TaskService.csproj"

COPY . .

RUN dotnet publish "src/Flowist.TaskService/Flowist.TaskService.csproj" --configuration Release --output /app/publish /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

USER app

ENTRYPOINT ["dotnet", "Flowist.TaskService.dll"]