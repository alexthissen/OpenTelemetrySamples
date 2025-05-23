#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0.0-preview.4-trixie-slim-arm64v8 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

#10.0.0-preview.5-trixie-slim
	
FROM mcr.microsoft.com/dotnet/sdk:10.0.100-preview.4-alpine3.21 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/LeaderboardWebAPI/LeaderboardWebAPI.csproj", "src/LeaderboardWebAPI/"]
RUN dotnet restore "src/LeaderboardWebAPI/LeaderboardWebAPI.csproj"
COPY . .
WORKDIR "/src/src/LeaderboardWebAPI"
RUN dotnet build "LeaderboardWebAPI.csproj" -c Release -o /app/build
RUN dotnet tool install --tool-path /tools dotnet-trace \
 && dotnet tool install --tool-path /tools dotnet-counters

FROM build AS publish
RUN dotnet publish "LeaderboardWebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /tools
COPY --from=build /tools .
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeaderboardWebAPI.dll"]