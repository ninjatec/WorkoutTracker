FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5225

ENV ASPNETCORE_URLS=http://+:5225

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["WorkoutTrackerWeb/WorkoutTrackerWeb.csproj", "WorkoutTrackerWeb/"]
RUN dotnet restore "WorkoutTrackerWeb/WorkoutTrackerWeb.csproj"
COPY . .
WORKDIR "/src/WorkoutTrackerWeb"
RUN dotnet build "WorkoutTrackerWeb.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "WorkoutTrackerWeb.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkoutTrackerWeb.dll"]
