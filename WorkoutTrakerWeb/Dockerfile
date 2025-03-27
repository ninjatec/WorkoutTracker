FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5238

ENV ASPNETCORE_URLS=http://+:5238

USER app
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG configuration=Release
WORKDIR /src
COPY ["WorkoutTrakerWeb/WorkoutTrakerWeb.csproj", "WorkoutTrakerWeb/"]
RUN dotnet restore "WorkoutTrakerWeb/WorkoutTrakerWeb.csproj"
COPY . .
WORKDIR "/src/WorkoutTrakerWeb"
RUN dotnet build "WorkoutTrakerWeb.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "WorkoutTrakerWeb.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WorkoutTrakerWeb.dll"]
